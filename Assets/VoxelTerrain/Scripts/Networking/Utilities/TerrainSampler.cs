using System.Collections;
using System.Collections.Generic;
using LibNoise;
using UnityEngine;

public class TerrainSampler : ISampler
{

    public IModule NoiseModule;
    public IModule caveModule;

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

    public TerrainSampler(IModule module, int _seed, bool _enableCaves, float _amp, float _caveDensity, float _grassOffset)
    {
        NoiseModule = module;
        seed = _seed;
        enableCaves = _enableCaves;
        amp = _amp;
        caveDensity = _caveDensity;
        grassOffset = _grassOffset;

        RidgedMultifractal _caves = new RidgedMultifractal();
        _caves.Seed = _seed;
        _caves.Frequency = 0.3;
        caveModule = _caves;
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
    }

    public double GetHeight(int x, int y)
    {
        if (NoiseModule != null)
            return NoiseModule.GetValue((x * (.003 / VoxelsPerMeter)), 0, (y * (.003 / VoxelsPerMeter))) * amp * VoxelsPerMeter - 60;
        //else
        //    SafeDebug.LogError("NoiseModule null!");
        return 0;
    }

    Vector3 GetPointNormal(int x, int z)
    {
        float val = (float)GetSurfaceHeight(x, z);

        float nx = (val - (float)GetSurfaceHeight(x + 1, z)) - (val - (float)GetSurfaceHeight(x - 1, z));
        float ny = (val - (float)GetSurfaceHeight(x, z) + 1) - (val - (float)GetSurfaceHeight(x, z) - 1);
        float nz = (val - (float)GetSurfaceHeight(x, z + 1)) - (val - (float)GetSurfaceHeight(x, z - 1));

        return new Vector3(nx, ny, nz).normalized;
    }

    public double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, out uint type)
    {
        double result = -1;
        type = 1;
        try
        {

            double surfaceHeight = GetSurfaceHeight(LocalPosition.x, LocalPosition.z);
            result = surfaceHeight - (globalLocation.y * VoxelsPerMeter);
            bool surface = (result > 0);

            if (globalLocation.y < surfaceHeight - 6)
            {
                type = 3;
            }
            else if (globalLocation.y < surfaceHeight - 2)
            {
                type = 2;
            }

            if (type == 1)
            {
                Vector3 norm = GetPointNormal(LocalPosition.x, LocalPosition.z);
                //if (Vector3.Distance(globalLocation, new Vector3(globalLocation.x, (float)surfaceHeight, globalLocation.z)) < 1)
                //    Debug.DrawRay(globalLocation, norm, Color.red, 100000);

                if (Vector3.Angle(Vector3.up, norm) > 40f)
                {
                    type = 2; // dirt
                }

                if (Vector3.Angle(Vector3.up, norm) > 50f)
                {
                    type = 3; // rock
                }
                
            }

            if (enableCaves)
            {
                float noiseVal = (float)Noise(caveModule, globalLocation.x, globalLocation.y, globalLocation.z, 16.0,
                    17.0, 1.0);
                if (noiseVal > caveDensity)
                {
                    result = Mathf.Clamp01((float)result) - noiseVal;
                    surface = false;
                }
            }

            //if (globalLocation.y < 5)
            //    result = 1;

            //if (surface && !surfacePoints.ContainsKey(new Vector2Int(globalLocation.x, globalLocation.z)))
            //    surfacePoints.Add(new Vector2Int(globalLocation.x, globalLocation.z), globalLocation);
        }
        catch (System.Exception e)
        {
            SafeDebug.LogError(string.Format("Message: {0}\nglobalX={1}, globalZ={2}\nlocalX={3}/{4}, localZ={5}/{6}",
                e.Message, globalLocation.x, globalLocation.z, LocalPosition.x, SurfaceData.GetLength(0), LocalPosition.z, SurfaceData.GetLength(1)), e);
            type = 0;
        }
        return result;
    }

    public double GetSurfaceHeight(int LocalX, int LocalZ)
    {
        LocalX = Mathf.Clamp(LocalX, -1, ChunkSizeX);
        LocalZ = Mathf.Clamp(LocalZ, -1, ChunkSizeZ);

        return SurfaceData[(LocalX + 1) * (ChunkSizeZ + 2) + (LocalZ + 1)];
    }

    public double Noise(IModule module, float x, float y, float z, double scale, double height, double power)
    {
        double rValue = 0;
        if (module != null)
        {
            rValue = module.GetValue(((double)x) / scale, ((double)y) / scale, ((double)z) / scale);
            rValue *= height;

            if (power != 0)
            {
                rValue = Mathf.Pow((float)rValue, (float)power);
            }
        }

        return rValue;
    }

    public float[] SetSurfaceData(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float val = data[i];
            min = Mathf.Min(min, val);
            max = Mathf.Max(max, val);
            SurfaceData[i] = val;
        }
        SurfaceSet = true;
        return SurfaceData;
    }

    public float[] SetSurfaceData(Vector2Int bottomLeft, Vector2Int topRight)
    {
        if (SurfaceSet)
            return SurfaceData;


        for (int noiseX = bottomLeft.x - 1, x = 0; noiseX < topRight.x + 1; noiseX++, x++)
        {
            for (int noiseZ = bottomLeft.y - 1, z = 0; noiseZ < topRight.y + 1; noiseZ++, z++)
            {
                float val = (float)GetHeight(noiseX, noiseZ);
                min = Mathf.Min(min, val);
                max = Mathf.Max(max, val);
                
                if (x > 0 && z > 0 && x <= ChunkSizeX && z <= ChunkSizeZ)
                {
                    int type = 1;
                    if (enableCaves)
                    {
                        float noiseVal = (float)Noise(caveModule, noiseX, val, noiseZ, 16.0,
                            17.0, 1.0);
                        if (noiseVal > caveDensity - 2)
                        {
                            type = 0;
                            //val = val - noiseVal;
                        }
                    }

                    plantMap[(x - 1) * (ChunkSizeZ) + (z - 1)] = type;
                }
                SurfaceData[x * (ChunkSizeZ + 2) + z] = val;
            }
        }
        SurfaceSet = true;
        return SurfaceData;
    }

    public float[] GetSurfaceData()
    {
        return SurfaceData;
    }

    public int[] GetPlantMap()
    {
        return plantMap;
    }

    public double GetMin()
    {
        return min;
    }

    public double GetMax()
    {
        return max;
    }

    public void Dispose()
    {
        NoiseModule = null;
        caveModule = null;
        SurfaceData = null;
    }
}
