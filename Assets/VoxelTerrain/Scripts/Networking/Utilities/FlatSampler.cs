using System.Collections;
using System.Collections.Generic;
using LibNoise;
using UnityEngine;

public class FlatSampler : ISampler
{
    public int Y;
    public uint Type;

    public float[] SurfaceData;

    public double VoxelsPerMeter;

    public int ChunkSizeZ;

    public FlatSampler(int y, uint type)
    {
        Y = y;
        Type = type;
    }

    public double GetHeight(int x, int y)
    {
        return Y;
    }

    public double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, out uint type)
    {
        double result = -1;
        type = Type;

        double surfaceHeight = GetSurfaceHeight(LocalPosition.x, LocalPosition.z);
        result = surfaceHeight - (globalLocation.y * VoxelsPerMeter);

        


        return result;
    }

    public double GetSurfaceHeight(int LocalX, int LocalZ)
    {
        return SurfaceData[(LocalX + 1) * (ChunkSizeZ + 2) + (LocalZ + 1)];
    }

    public double Noise(IModule module, float x, float y, float z, double scale, double height, double power)
    {
        return Y;
    }

    public void SetChunkSettings(double voxelsPerMeter, Vector3Int chunkSizes, Vector3Int chunkMeterSize, int skipDist, float half, Vector3 sideLength)
    {
        VoxelsPerMeter = voxelsPerMeter;
        ChunkSizeZ = chunkSizes.z;

        SurfaceData = new float[(chunkSizes.x + 2) * (chunkSizes.z + 2)];
    }

    public float[] SetSurfaceData(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float val = data[i];
            SurfaceData[i] = val;
        }
        return SurfaceData;
    }

    public float[] SetSurfaceData(Vector2Int bottomLeft, Vector2Int topRight)
    {
        for (int noiseX = bottomLeft.x - 1, x = 0; noiseX < topRight.x + 1; noiseX++, x++)
        {
            for (int noiseZ = bottomLeft.y - 1, z = 0; noiseZ < topRight.y + 1; noiseZ++, z++)
            {
                SurfaceData[x * (ChunkSizeZ + 2) + z] = (float)GetHeight(noiseX, noiseZ);
            }
        }
        return SurfaceData;
    }

    public float[] GetSurfaceData()
    {
        return SurfaceData;
    }

    public void Dispose()
    {
        SurfaceData = null;
    }

    public double GetMin()
    {
        return Y;
    }

    public double GetMax()
    {
        return Y;
    }
}
