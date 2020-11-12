using System.Collections;
using System.Collections.Generic;
using LibNoise;
using LibNoise.Models;
using LibNoise.Modifiers;
using UnityEngine;

public interface ISampler : System.IDisposable
{
    void SetChunkSettings(double voxelsPerMeter, Vector3Int chunkSizes, Vector3Int chunkMeterSize, int skipDist, float half, Vector3 sideLength);
    double Noise(IModule module, float x, float y, float z, double scale, double height, double power);
    double GetHeight(int x, int y);
    float[] SetSurfaceData(Vector2Int bottomLeft, Vector2Int topRight);
    float[] SetSurfaceData(float[] data);
    float[] GetSurfaceData();
    double GetSurfaceHeight(int LocalX, int LocalZ);
    double GetMin();
    double GetMax();
    double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, out uint type);
}
