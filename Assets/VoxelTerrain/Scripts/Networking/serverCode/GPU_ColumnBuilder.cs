using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;


public class GPU_ColumnBuilder : IColumnBuilder
{
    

    struct Debug_Data_Res
    {
        public Vector3 p1;
        public Vector3 p2;
    };

    public GPU_TerrainSampler Sampler { get; private set; }

    public ColumnResult Result { get; private set; }

    public Vector3Int Location { get; private set; }

    public bool SurfaceGenerated { get; private set; }

    private float VoxelsPerMeter;
    private int ChunkMeterSizeX;
    private int ChunkMeterSizeY;
    private int ChunkMeterSizeZ;
    private int ChunkSizeX;
    private int ChunkSizeY;
    private int ChunkSizeZ;
    private int skipDist;
    private float half;
    private float xSideLength;
    private float ySideLength;
    private float zSideLength;
    private bool deactivated;


    ComputeShader shader;

    public ComputeBuffer Data { get; set; }
    public ComputeBuffer Data_DEBUG { get; set; }

    int CS_Generate;

    public GPU_ColumnBuilder()
    {
        Result = new ColumnResult();
    }

    public GPU_ColumnBuilder(ColumnResult result)
    {
        Result = result;
    }

    public GPU_ColumnBuilder(ISampler sampler)
    {
        Result = new ColumnResult();
        Sampler = (GPU_TerrainSampler)sampler;
    }

    public GPU_ColumnBuilder(ColumnResult result, ISampler sampler)
    {
        Result = result;
        Sampler = (GPU_TerrainSampler)sampler;
    }

    public void Init(Vector3Int location, float voxelsPerMeter, int chunkMeterSizeX, int chunkMeterSizeY, int chunkMeterSizeZ)
    {
        Location = location;

        VoxelsPerMeter = voxelsPerMeter;
        ChunkMeterSizeX = chunkMeterSizeX;
        ChunkMeterSizeY = chunkMeterSizeY;
        ChunkMeterSizeZ = chunkMeterSizeZ;

        ChunkSizeX = (int)(ChunkMeterSizeX * VoxelsPerMeter);
        ChunkSizeY = (int)(ChunkMeterSizeY * VoxelsPerMeter);
        ChunkSizeZ = (int)(ChunkMeterSizeZ * VoxelsPerMeter);
        Debug.LogFormat("GPU Init: {0} x {1} x {2}, {3}", ChunkMeterSizeX, ChunkMeterSizeY, ChunkMeterSizeZ, VoxelsPerMeter);

        half = ((1.0f / (float)VoxelsPerMeter) / 2.0f);
        xSideLength = ChunkMeterSizeX / (float)ChunkSizeX;
        ySideLength = ChunkMeterSizeY / (float)ChunkSizeY;
        zSideLength = ChunkMeterSizeZ / (float)ChunkSizeZ;
        skipDist = Mathf.RoundToInt(1 / (float)VoxelsPerMeter);

        Sampler.Extract = false;

        shader = (ComputeShader)Resources.Load("shaders/ChunkCompute");

        shader.SetFloat("VoxelsPerMeter", VoxelsPerMeter);
        shader.SetInt("ChunkMeterSizeX", ChunkMeterSizeX);
        shader.SetInt("ChunkMeterSizeY", ChunkMeterSizeY);
        shader.SetInt("ChunkMeterSizeZ", ChunkMeterSizeZ);
        shader.SetInt("ChunkSizeX", ChunkSizeX);
        shader.SetInt("ChunkSizeY", ChunkSizeY);
        shader.SetInt("ChunkSizeZ", ChunkSizeZ);
        shader.SetInt("skipDist", skipDist);
        shader.SetFloat("half_", half);
        shader.SetFloat("xSideLength", xSideLength);
        shader.SetFloat("ySideLength", ySideLength);
        shader.SetFloat("zSideLength", zSideLength);

        CS_Generate = shader.FindKernel("CS_Generate");
    }

    public float[] GenerateHeightMap()
    {
        if (!SurfaceGenerated)
        {
            Vector2Int bottomLeft = new Vector2(Location.x * ChunkSizeX, Location.z * ChunkSizeZ);
            Vector2Int topRight = new Vector2(Location.x * ChunkSizeX + ChunkSizeX, Location.z * ChunkSizeZ + ChunkSizeZ);
            Result.SurfaceData = Sampler.SetSurfaceData(bottomLeft, topRight);
            SurfaceGenerated = true;

            Result.Min = (int)Sampler.GetMin();
            Result.Max = (int)Sampler.GetMax();

            return Result.SurfaceData;
        }
        return Result.SurfaceData;
    }

    public ColumnResult Generate(int Y_Min, int Y_Max, int xStart, int zStart)
    {
        Stopwatch watch_overall = new Stopwatch();
        watch_overall.Start();

        Stopwatch watch = new Stopwatch();
        watch.Start();
        Sampler.ComputeNoiseGrid(Y_Min, Y_Max, xStart, zStart);
        watch.Stop();

        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): ComputeNoiseGrid: {0}", watch.Elapsed);

        int y_height = (Y_Max - Y_Min) + 2;

        watch.Restart();

        int[] args = new int[] { 0, 1, 0, 0 };
        ComputeBuffer argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        argBuffer.SetData(args);

        Data = new ComputeBuffer(Sampler.iso_type_buffer.count, sizeof(int), ComputeBufferType.Append);
        Data_DEBUG = new ComputeBuffer(Sampler.iso_type_buffer.count, sizeof(float) * 2, ComputeBufferType.Append);

        Data.SetCounterValue(0);
        Data_DEBUG.SetCounterValue(0);

        shader.SetBuffer(CS_Generate, "HeightMap", Sampler.height_buffer);
        shader.SetBuffer(CS_Generate, "ISO_Type_Map", Sampler.iso_type_buffer);

        shader.SetBuffer(CS_Generate, "Data", Data);
        shader.SetBuffer(CS_Generate, "Data_DEBUG", Data_DEBUG);

        watch.Stop();

        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): Init buffers: {0}", watch.Elapsed);

        watch.Restart();

        shader.SetInt("Y_Min", Y_Min);
        shader.SetInt("Y_Max", Y_Max);
        shader.SetInt("xStart", xStart);
        shader.SetInt("zStart", zStart);
        shader.SetInt("y_height", y_height);

        watch.Stop();

        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): Init shader globals: {0}", watch.Elapsed);

        watch.Restart();

        //Debug.LogFormat("GPU Generate: {0} x {1} x {2}", ChunkSizeX, y_height, ChunkSizeZ);
        shader.Dispatch(CS_Generate, ChunkSizeX / 6, y_height - 1, ChunkSizeZ / 6);

        watch.Stop();

        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): Dispatch: {0}", watch.Elapsed);

        watch.Restart();

        ComputeBuffer.CopyCount(Data, argBuffer, 0);
        argBuffer.GetData(args);

        Result.surfaceBlocksCount = args[0];
        Result.surfaceBlocks = new uint[Result.surfaceBlocksCount];
        Data.GetData(Result.surfaceBlocks);

        watch.Stop();

        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): Get surfaceBlocks: {0}", watch.Elapsed);

        watch.Restart();

        Debug_Data_Res[] debug_data = new Debug_Data_Res[0];

        if (true)
        {
            debug_data = new Debug_Data_Res[Result.surfaceBlocksCount];
            Data_DEBUG.GetData(debug_data);
        }


        watch.Stop();
        watch_overall.Stop();

        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): Get debug_data: {0}", watch.Elapsed);
        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): Overall: {0}", watch_overall.Elapsed);


        Data.Dispose();
        Data_DEBUG.Dispose();

        Sampler.Dispose_ISO_Types();

        for (int i = 0; i < debug_data.Length; i++)
        {
            Vector3Int p1 = debug_data[i].p1;
            Vector3Int p2 = debug_data[i].p2;
            Vector3 orig = new Vector3(p2.x, p2.y, p2.z);
            Vector3 other = new Vector3(p1.x, p1.y, p1.z);

            UnityEngine.Debug.DrawLine(orig, other, UnityEngine.Color.red, 50000);
        }

        return Result;
    }

    public void Dispose()
    {
        deactivated = true;
        if (Sampler != null)
            Sampler.Dispose();
        Result.Dispose();
    }
}
