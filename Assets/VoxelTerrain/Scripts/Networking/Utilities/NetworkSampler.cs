using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibNoise;
using UnityEngine;

public class NetworkSampler : ISampler
{
    public float[] SurfaceData;
    public int[] plantMap;
    public bool SurfaceSet = false;

    public double VoxelsPerMeter;
    public int ChunkSizeX;
    public int ChunkSizeY;
    public int ChunkSizeZ;

    public float min = float.MaxValue;
    public float max = float.MinValue;

    public NetworkSampler()
    {
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
        return 0;
    }

    public double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, out uint type)
    {
        double result = -1;
        type = 1;

        double surfaceHeight = GetSurfaceHeight(LocalPosition.x, LocalPosition.z);
        result = surfaceHeight - (globalLocation.y * VoxelsPerMeter);

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

        return result;
    }

    Vector3 GetPointNormal(int x, int z)
    {
        float val = (float)GetSurfaceHeight(x, z);

        float nx = (val - (float)GetSurfaceHeight(x + 1, z)) - (val - (float)GetSurfaceHeight(x - 1, z));
        float ny = (val - (float)GetSurfaceHeight(x, z) + 1) - (val - (float)GetSurfaceHeight(x, z) - 1);
        float nz = (val - (float)GetSurfaceHeight(x, z + 1)) - (val - (float)GetSurfaceHeight(x, z - 1));

        return new Vector3(nx, ny, nz).normalized;
    }

    public double GetMax()
    {
        return max;
    }

    public double GetMin()
    {
        return min;
    }

    public float[] GetSurfaceData()
    {
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
        return 0;
    }

    public float[] SetSurfaceData(float[] heightmap)
    {
        SurfaceData = heightmap;
        SurfaceSet = true;
        return SurfaceData;
    }

    public float[] SetSurfaceData(Vector2Int bottomLeft, Vector2Int topRight)
    {
        return SurfaceData;
    }

    public int[] GetPlantMap()
    {
        return plantMap;
    }

    public void Dispose()
    {
        SurfaceData = null;
    }
}
