using System.Collections;
using System.Collections.Generic;
using LibNoise;
using UnityEngine;

public class SphereSampler : ISampler
{
    public IModule NoiseModule;
    public IModule caveModule;

    Vector3 Center;
    float Radius;

    public double[] SurfaceData;

    public double VoxelsPerMeter;
    public int ChunkSizeZ;

    public SphereSampler(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;

        Perlin _caves = new Perlin();
        _caves.Seed = 0;
        _caves.Frequency = 0.5;
        caveModule = _caves;

        Random.InitState(new System.DateTime().Millisecond);
    }

    public void SetChunkSettings(double voxelsPerMeter, Vector3Int chunkSizes, Vector3Int chunkMeterSize, int skipDist, float half, Vector3 sideLength)
    {
        VoxelsPerMeter = voxelsPerMeter;
        ChunkSizeZ = chunkSizes.z;
    }

    public void SetChunkSettings(double voxelsPerMeter, Vector3Int chunkSizes)
    {
        VoxelsPerMeter = voxelsPerMeter;
        ChunkSizeZ = chunkSizes.z;
    }

    public double GetHeight(int x, int y)
    {
        if (NoiseModule != null)
            return NoiseModule.GetValue((x * (.003 / VoxelsPerMeter)), 0, (y * (.003 / VoxelsPerMeter))) * VoxelsPerMeter;
        return 0;
    }

    public double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, out uint type)
    {
        double result = -1;
        type = 1;
        try
        {



            float distance = Vector3.Distance(LocalPosition, Center);
            float iso = Radius - distance;

            /*float iso = -1;
            if (LocalPosition.x > 1 && LocalPosition.x < ChunkSizeZ - 1 &&
                LocalPosition.y > 1 && LocalPosition.y < ChunkSizeZ - 1 &&
                LocalPosition.z > 1 && LocalPosition.z < ChunkSizeZ - 1)
            {
                iso = 1;
            }*/

            
            //type = (uint)Mathf.RoundToInt(Random.Range(0.6f, 4.4f));

            //if (iso > 0)
            //{
                if (LocalPosition.y - 10 > 3)
                    type = 1;
                else if (LocalPosition.y - 10 >= 0)
                    type = 2;
                else
                    type = 3;
            //}
            //else
            //    type = 0;

            result = iso;
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
        return SurfaceData[(LocalX + 1) * (ChunkSizeZ + 2) + (LocalZ + 1)];
    }

    public double Noise(IModule module, int x, int y, int z, double scale, double height, double power)
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
        NoiseModule = null;
        caveModule = null;
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
