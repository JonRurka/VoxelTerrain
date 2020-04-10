using System.Collections;
using System.Collections.Generic;
using LibNoise;
using UnityEngine;

public class FlatSampler : ISampler
{
    public int Y;
    public uint Type;

    public double[] SurfaceData;

    public int ChunkSizeZ;

    public FlatSampler(int y, uint type)
    {
        Y = y;
        Type = type;
    }

    public double GetHeight(int x, int y)
    {
        return y;
    }

    public double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, out uint type)
    {
        double result = 1;
        type = Type;

        if (globalLocation.z < Y)
        {
            result = -1;
        }


        return result;
    }

    public double GetSurfaceHeight(int LocalX, int LocalZ)
    {
        return SurfaceData[(LocalX + 1) * (ChunkSizeZ + 2) + (LocalZ + 1)];
    }

    public double Noise(IModule module, int x, int y, int z, double scale, double height, double power)
    {
        return y;
    }

    public void SetChunkSettings(double voxelsPerMeter, Vector3Int chunkSizes, Vector3Int chunkMeterSize, int skipDist, float half, Vector3 sideLength)
    {
        ChunkSizeZ = chunkSizes.z;
    }

    public double[] SetSurfaceData(Vector2Int bottomLeft, Vector2Int topRight)
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

    public void Dispose()
    {
        SurfaceData = null;
    }

    public double GetMin()
    {
        throw new System.NotImplementedException();
    }

    public double GetMax()
    {
        throw new System.NotImplementedException();
    }
}
