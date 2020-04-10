using UnityEngine;
using System.Collections;

public static class VoxelConversions {


    public static Vector3 ChunkCoordToWorld(Vector3Int location) {
        //return new Vector3(location.x * VoxelSettings.ChunkSizeX / VoxelSettings.voxelsPerMeter - VoxelSettings.half, location.y * VoxelSettings.ChunkSizeY / VoxelSettings.voxelsPerMeter - VoxelSettings.half, location.z * VoxelSettings.ChunkSizeZ / VoxelSettings.voxelsPerMeter - VoxelSettings.half);
        return new Vector3(location.x * SmoothVoxelSettings.MeterSizeX - SmoothVoxelSettings.half, location.y * SmoothVoxelSettings.MeterSizeY - SmoothVoxelSettings.half, location.z * SmoothVoxelSettings.MeterSizeZ - SmoothVoxelSettings.half);
    }

    public static Vector3Int WorldPosToChunkCoord(Vector3 location) {
        return new Vector3Int(Mathf.RoundToInt(location.x / SmoothVoxelSettings.MeterSizeX + SmoothVoxelSettings.half), Mathf.RoundToInt(location.y / SmoothVoxelSettings.MeterSizeY + SmoothVoxelSettings.half), Mathf.RoundToInt(location.y / SmoothVoxelSettings.MeterSizeZ + SmoothVoxelSettings.half));
    }

    public static Vector3Int GlobalToLocalChunkCoord(Vector3Int location) {
        Vector3Int ChunkCoord = VoxelToChunk(location);
        return GlobalToLocalChunkCoord(ChunkCoord, location);
    }

    public static Vector3Int GlobalToLocalChunkCoord(Vector3Int ChunkCoord, Vector3Int location) {
        int x = location.x - (ChunkCoord.x * SmoothVoxelSettings.ChunkSizeX);
        int y = location.y - (ChunkCoord.y * SmoothVoxelSettings.ChunkSizeY);
        int z = location.z - (ChunkCoord.z * SmoothVoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int LocalToGlobalCoord(Vector3Int Chunk, Vector3Int location) {
        int x = location.x + (Chunk.x * SmoothVoxelSettings.ChunkSizeX);
        int y = location.y + (Chunk.y * SmoothVoxelSettings.ChunkSizeY);
        int z = location.z + (Chunk.z * SmoothVoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int VoxelToChunk(Vector3Int location) {
        int x = Mathf.FloorToInt(location.x / (float)SmoothVoxelSettings.ChunkSizeX);
        int y = Mathf.FloorToInt(location.y / (float)SmoothVoxelSettings.ChunkSizeY);
        int z = Mathf.FloorToInt(location.z / (float)SmoothVoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int ChunkToVoxel(Vector3Int location)
    {
        int x = ((location.x) * SmoothVoxelSettings.ChunkSizeX);
        int y = ((location.y) * SmoothVoxelSettings.ChunkSizeY);
        int z = ((location.z) * SmoothVoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3 VoxelToWorld(Vector3Int location)
    {
        return VoxelToWorld(location.x, location.y, location.z);
    }

    public static Vector3 VoxelToWorld(int x, int y, int z) {
        float newX = (((x / (float)SmoothVoxelSettings.voxelsPerMeter) - SmoothVoxelSettings.half));
        float newY = (((y / (float)SmoothVoxelSettings.voxelsPerMeter) - SmoothVoxelSettings.half));
        float newZ = (((z / (float)SmoothVoxelSettings.voxelsPerMeter) - SmoothVoxelSettings.half));
        return new Vector3(newX, newY, newZ);
    }

    public static Vector3Int WorldToVoxel(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(((worldPos.x) + SmoothVoxelSettings.half) * (float)SmoothVoxelSettings.voxelsPerMeter);
        int y = Mathf.FloorToInt(((worldPos.y) + SmoothVoxelSettings.half) * (float)SmoothVoxelSettings.voxelsPerMeter);
        int z = Mathf.FloorToInt(((worldPos.z) + SmoothVoxelSettings.half) * (float)SmoothVoxelSettings.voxelsPerMeter);
        return new Vector3Int(x, y, z);
    }

    public static float Scale(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return newMin + (value - oldMin) * (newMax - newMin) / (oldMax - oldMin);
    }
}

