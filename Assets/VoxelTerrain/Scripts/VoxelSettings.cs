using UnityEngine;
using System.Collections;

public static class VoxelSettings {
    // world settings.
    public static int seed = 0;
    public const int radius = 30;
    public const bool randomSeed = false;
    public const bool CircleGen = false;
    public const bool enableCaves = false;
    public const float caveDensity = 5;
    public const float amplitude = 250;
    public const float groundOffset = 80;
    public const float grassOffset = 4;
    public const int maxSuperChunksX = 1;
    public const int maxSuperChunksY = 1;
    public const int maxSuperChunksZ = 1;
    public const int ViewDistanceX = 5;
    public const int ViewDistanceY = 5;
    public const int ViewDistanceZ = 5;
    public const int ViewRadius = 2;

    // chunk settings.
    public const double voxelsPerMeter = 1f;
    public const int MeterSizeX = 20;
    public const int MeterSizeY = 20;
    public const int MeterSizeZ = 20;
    public const int ChunkSizeX = (int)(MeterSizeX * voxelsPerMeter);
    public const int ChunkSizeY = (int)(MeterSizeY * voxelsPerMeter);
    public const int ChunkSizeZ = (int)(MeterSizeZ * voxelsPerMeter);
    public const float half = (float)((1 / voxelsPerMeter) / 2);

    // Super chunks settings.
    public const int maxChunksX = 10;
    public const int maxChunksY = 16;
    public const int maxChunksZ = 10;
    public const int SuperSizeX = ChunkSizeX * maxChunksX;
    public const int SuperSizeY = ChunkSizeY * maxChunksY;
    public const int SuperSizeZ = ChunkSizeZ * maxChunksZ;

    //flora
    public static int treesPerChunk = 4;
}

