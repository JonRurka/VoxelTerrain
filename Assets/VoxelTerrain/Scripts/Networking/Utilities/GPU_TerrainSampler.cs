using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibNoise;
using UnityEngine;

public class GPU_TerrainSampler : ISampler
{
    public struct Result
    {
        public float iso;
        public uint type;

        public static int Size()
        {
            return sizeof(float) + sizeof(uint);
        }
    }

    public int seed;
    public bool enableCaves;
    public float amp;
    public float caveDensity;
    public float grassOffset;

    public float[] SurfaceData;
    public int[] plantMap;
    public bool SurfaceSet = false;

    public double VoxelsPerMeter;
    public int ChunkSizeX;
    public int ChunkSizeY;
    public int ChunkSizeZ;

    public float min = float.MaxValue;
    public float max = float.MinValue;

    public int ymin;
    public int ymax;
    int y_height;

    ComputeShader shader;

    public ComputeBuffer height_buffer { get; set; }
    public ComputeBuffer iso_buffer { get; set; }
    public ComputeBuffer type_buffer { get; set; }
    public ComputeBuffer iso_type_buffer { get; set; }

    public bool SurfaceExtracted;
    public bool ISO_Extracted;
    public bool Extract { get; set; }

    private Result[] iso_types;

    int CS_Heightmap;
    int CS_ISO_Type_Map;

    public GPU_TerrainSampler(int _seed, bool _enableCaves, float _amp, float _caveDensity, float _grassOffset)
    {
        Extract = true;

        seed = _seed;
        enableCaves = _enableCaves;
        amp = _amp;
        caveDensity = _caveDensity;
        grassOffset = _grassOffset;

        shader = (ComputeShader)Resources.Load("shaders/TerrainModule");



        iso_type_buffer = new ComputeBuffer(1, Result.Size());
        height_buffer = new ComputeBuffer(1, sizeof(float));
        iso_buffer = new ComputeBuffer(1, sizeof(float) + sizeof(uint));

        CS_Heightmap = shader.FindKernel("CS_Heightmap");
        CS_ISO_Type_Map = shader.FindKernel("CS_ISO_Type_Map");

        //RidgedMultifractal _caves = new RidgedMultifractal();
        //_caves.Seed = _seed;
        //_caves.Frequency = 0.3;
        //caveModule = _caves;
    }

    public void SetChunkSettings(double voxelsPerMeter, Vector3Int chunkSizes, Vector3Int chunkMeterSize, int skipDist, float half, Vector3 sideLength)
    {
        if (SurfaceSet)
            return;

        VoxelsPerMeter = voxelsPerMeter;
        ChunkSizeX = chunkSizes.x;
        ChunkSizeY = chunkSizes.y;
        ChunkSizeZ = chunkSizes.z;

        SurfaceData = new float[(ChunkSizeX + 2) * (ChunkSizeZ + 2)];
        plantMap = new int[ChunkSizeX * ChunkSizeZ];

        shader.SetInt("ChunkSizeX", ChunkSizeX);
        shader.SetInt("ChunkSizeY", ChunkSizeY);
        shader.SetInt("ChunkSizeZ", ChunkSizeZ);

        shader.SetFloat("VoxelsPerMeter", (float)VoxelsPerMeter);
        shader.SetInt("quality", 2);
        shader.SetInt("seed", seed);
        shader.SetBool("enableCaves", enableCaves);
        shader.SetFloat("amp", amp);
        shader.SetFloat("caveDensity", caveDensity);
        shader.SetFloat("grassOffset", grassOffset);

        

        //height_buffer = new ComputeBuffer(SurfaceData.Length, sizeof(float));
        //iso_buffer = new ComputeBuffer(ChunkSizeX * ChunkSizeY * ChunkSizeZ, sizeof(float));
    }

    public double GetHeight(int x, int y)
    {
        throw new NotImplementedException();
    }

    public double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, out uint type)
    {
        Result res = default(Result);
        try
        {
            res = iso_types[Get_Flat_Index(LocalPosition.x, LocalPosition.y - ymin, LocalPosition.z, y_height)];
        }
        catch(Exception ex)
        {
            int ind = Get_Flat_Index(LocalPosition.x, LocalPosition.y - ymin, LocalPosition.z, y_height);
            UnityGameServer.Logger.Log("GetIsoValue: {0}, {1} => {2}", ind, LocalPosition, new Vector3Int(LocalPosition.x, LocalPosition.y - ymin, LocalPosition.z));
            throw ex;
        }
        type = res.type;

        return res.iso;
    }

    public double GetMax()
    {
        if (!Extract && !SurfaceExtracted)
        {
            GetSurfaceData();
        }

        return Mathf.Ceil(max);
    }

    public double GetMin()
    {
        if (!Extract && !SurfaceExtracted)
        {
            GetSurfaceData();
        }

        return min;
    }

    public float[] GetSurfaceData()
    {
        SurfaceData = new float[height_buffer.count];
        height_buffer.GetData(SurfaceData);

        min = Mathf.Min(SurfaceData);
        max = Mathf.Max(SurfaceData);

        UnityGameServer.Logger.Log("SurfaceData[0]: {0}", SurfaceData[0]);

        return SurfaceData;
    }

    public double GetSurfaceHeight(int LocalX, int LocalZ)
    {
        LocalX = Mathf.Clamp(LocalX, -1, ChunkSizeX);
        LocalZ = Mathf.Clamp(LocalZ, -1, ChunkSizeZ);

        return SurfaceData[(LocalX + 1) * (ChunkSizeZ + 2) + (LocalZ + 1)];
    }

    public double Noise(IModule module, float x, float y, float z, double scale, double height, double power)
    {
        throw new NotImplementedException();
    }

    public float[] SetSurfaceData(Vector2Int bottomLeft, Vector2Int topRight)
    {
        int noiseX = bottomLeft.x - 1;
        int noiseZ = bottomLeft.y - 1;

        height_buffer.Dispose();
        height_buffer = new ComputeBuffer((ChunkSizeX + 2) * (ChunkSizeZ + 2), sizeof(float));
        shader.SetBuffer(CS_Heightmap, "HeightMap", height_buffer);

        shader.SetFloat("noiseX", noiseX);
        shader.SetFloat("noiseZ", noiseZ);

        shader.Dispatch(CS_Heightmap, (ChunkSizeX + 2) / 6, 1, (ChunkSizeZ + 2) / 6);

        if (Extract)
        {
            GetSurfaceData();

            min = Mathf.Min(SurfaceData);
            max = Mathf.Max(SurfaceData);
        }

        return SurfaceData;
    }

    public void ComputeNoiseGrid(int Y_Min, int Y_Max, int xStart, int zStart)
    {
        ymin = Y_Min;
        ymax = Y_Max;

        y_height = (Y_Max - Y_Min) + 2;

        iso_type_buffer.Dispose();

        UnityGameServer.Logger.Log("ComputeNoiseGrid: {0}, {1} | {2}, {3}, {4}", Y_Min, Y_Max, ChunkSizeX, y_height, ChunkSizeZ);

        iso_type_buffer = new ComputeBuffer(ChunkSizeX * y_height * ChunkSizeZ, Result.Size());
        //iso_buffer = new ComputeBuffer(ChunkSizeX * y_height * ChunkSizeZ, sizeof(float));
        //type_buffer = new ComputeBuffer(ChunkSizeX * y_height * ChunkSizeZ, sizeof(uint));

        shader.SetBuffer(CS_ISO_Type_Map, "HeightMap", height_buffer);
        shader.SetBuffer(CS_ISO_Type_Map, "ISO_Type_Map", iso_type_buffer);
        //shader.SetBuffer(CS_ISO_Type_Map, "ISO_Map", iso_buffer);
        //shader.SetBuffer(CS_ISO_Type_Map, "Type_Map", type_buffer);

        shader.SetInt("Y_Min", Y_Min);
        shader.SetInt("Y_Max", Y_Max);
        shader.SetInt("xStart", xStart);
        shader.SetInt("zStart", zStart);
        shader.SetInt("y_height", y_height);

        shader.Dispatch(CS_ISO_Type_Map, ChunkSizeX / 8, y_height / 1, ChunkSizeZ / 8);

        if (Extract)
        {
            iso_types = new Result[ChunkSizeX * y_height * ChunkSizeZ];
            iso_type_buffer.GetData(iso_types);
        }
    }

    public float[] SetSurfaceData(float[] data)
    {
        SurfaceData = data;
        return SurfaceData;
    }

    public void Dispose_ISO_Types()
    {
        iso_types = new Result[0];
    }

    public void Dispose()
    {
        height_buffer.Dispose();
        iso_buffer.Dispose();
        type_buffer.Dispose();
        iso_type_buffer.Dispose();

        SurfaceData = null;
        plantMap = null;
        iso_types = null;
    }

    int Get_Flat_Index(int x, int y, int z, int h)
    {
        return x + ChunkSizeX * (y + h * z);
    }

    int Get_Flat_Index_2D(int x, int y)
    {
        return x * (ChunkSizeZ + 2) + y;
    }
}

