using UnityEngine;
using System.Collections;

public static class VoxelSettings {
    // world settings.
    public static int seed = 0;
    public static int radius = 10;
    public static bool randomSeed = false;
    public static bool CircleGen = false;
    public static bool enableCaves = false;
    public static float caveDensity = 5;
    public static float amplitude = 250;
    public static float groundOffset = 10;
    public static float grassOffset = 4;
    public static int maxSuperChunksX = 1;
    public static int maxSuperChunksY = 1;
    public static int maxSuperChunksZ = 1;
    public static int ViewDistanceX = 5;
    public static int ViewDistanceY = 5;
    public static int ViewDistanceZ = 5;
    public static int ViewRadius = 2;

    // chunk settings.
    public static float voxelsPerMeter = 1f;
    public static int MeterSizeX = 20;
    public static int MeterSizeY = 20;
    public static int MeterSizeZ = 20;
    public static int ChunkSizeX = (int)(MeterSizeX * voxelsPerMeter);
    public static int ChunkSizeY = (int)(MeterSizeY * voxelsPerMeter);
    public static int ChunkSizeZ = (int)(MeterSizeZ * voxelsPerMeter);
    public static float half = 0;// ((1f / (float)voxelsPerMeter) / 2f);

    // Super chunks settings.
    public static int maxChunksX = 10;
    public static int maxChunksY = 10;
    public static int maxChunksZ = 10;
    public static int SuperSizeX = ChunkSizeX * maxChunksX;
    public static int SuperSizeY = ChunkSizeY * maxChunksY;
    public static int SuperSizeZ = ChunkSizeZ * maxChunksZ;

    //flora
    public static int treesPerChunk = 4;
}

