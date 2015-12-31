using UnityEngine;
using System.Collections;

public static class VoxelConversions {
    public static Vector3Int[] GetCoords(Vector3 location) {
        Vector3Int[] result = new Vector3Int[3];
        result[0] = WorldPosToChunkCoord(location);
        //result[1] = ChunkCoordToSuperCoord(result[0]);
        result[1] = GlobalToLocalChunkCoords(result[0]);
        return result;
    }

    public static Vector3 ChunkCoordToWorld(Vector3Int location) {
        //return new Vector3(location.x * VoxelSettings.ChunkSizeX / VoxelSettings.voxelsPerMeter - VoxelSettings.half, location.y * VoxelSettings.ChunkSizeY / VoxelSettings.voxelsPerMeter - VoxelSettings.half, location.z * VoxelSettings.ChunkSizeZ / VoxelSettings.voxelsPerMeter - VoxelSettings.half);
        return new Vector3(location.x * VoxelSettings.MeterSizeX - VoxelSettings.half, location.y * VoxelSettings.MeterSizeY - VoxelSettings.half, location.z * VoxelSettings.MeterSizeZ - VoxelSettings.half);
    }

    public static Vector3Int WorldPosToChunkCoord(Vector3 location) {
        return new Vector3Int(Mathf.RoundToInt(location.x / VoxelSettings.MeterSizeX + VoxelSettings.half), Mathf.RoundToInt(location.y / VoxelSettings.MeterSizeY + VoxelSettings.half), Mathf.RoundToInt(location.y / VoxelSettings.MeterSizeZ + VoxelSettings.half));
    }

    public static Vector3Int GlobalToLocalChunkCoords(Vector3Int location) {
        throw new System.NotImplementedException();
        //Vector3Int super = ChunkCoordToSuperCoord(location);
        //return GlobalToLocalChunkCoords(super, location);
    }

    public static Vector3Int GlobalToLocalChunkCoords(Vector3Int super, Vector3Int location) {
        int x = (location.x - (super.x * VoxelSettings.maxChunksX));
        int y = (location.y - (super.y * VoxelSettings.maxChunksY));
        int z = (location.z - (super.z * VoxelSettings.maxChunksZ));
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int LocalToGlobalChunkCoords(Vector3Int super, Vector3Int chunk) {
        int x = chunk.x + (super.x * VoxelSettings.maxChunksX);
        int y = chunk.y + (super.y * VoxelSettings.maxChunksY);
        int z = chunk.z + (super.z * VoxelSettings.maxChunksZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int GlobalVoxToSuperVoxCoord(Vector3Int SuperLocation ,Vector3Int location) {
        int x = location.x - Mathf.Abs(SuperLocation.x * VoxelSettings.SuperSizeX);
        int y = location.y - Mathf.Abs(SuperLocation.y * VoxelSettings.SuperSizeY);
        int z = location.z - Mathf.Abs(SuperLocation.z * VoxelSettings.SuperSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int SuperVoxToGlobalVoxCoord(Vector3Int SuperChunk, Vector3Int location) {
        int x = location.x + (SuperChunk.x / VoxelSettings.SuperSizeX);
        int y = location.y + (SuperChunk.y / VoxelSettings.SuperSizeY);
        int z = location.z + (SuperChunk.z / VoxelSettings.SuperSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int GlobalVoxToLocalChunkVoxCoord(Vector3Int location) {
        Vector3Int ChunkCoord = VoxelToChunk(location);
        return GlobalVoxToLocalChunkVoxCoord(ChunkCoord, location);
    }

    public static Vector3Int GlobalVoxToLocalChunkVoxCoord(Vector3Int ChunkCoord, Vector3Int location) {
        int x = location.x - (ChunkCoord.x * VoxelSettings.ChunkSizeX);
        int y = location.y - (ChunkCoord.y * VoxelSettings.ChunkSizeY);
        int z = location.z - (ChunkCoord.z * VoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int LocalChunkVoxToGlobalVoxCoord(Vector3Int Chunk, Vector3Int location) {
        int x = location.x + (Chunk.x / VoxelSettings.ChunkSizeX);
        int y = location.y + (Chunk.y / VoxelSettings.ChunkSizeY);
        int z = location.z + (Chunk.z / VoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int VoxelToChunk(Vector3Int location) {
        int x = Mathf.FloorToInt(location.x / (float)VoxelSettings.ChunkSizeX);
        int y = Mathf.FloorToInt(location.y / (float)VoxelSettings.ChunkSizeY);
        int z = Mathf.FloorToInt(location.z / (float)VoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3Int ChunkToVoxel(Vector3Int location)
    {
        int x = ((location.x) * VoxelSettings.ChunkSizeX);
        int y = ((location.y) * VoxelSettings.ChunkSizeY);
        int z = ((location.z) * VoxelSettings.ChunkSizeZ);
        return new Vector3Int(x, y, z);
    }

    public static Vector3 VoxelToWorld(Vector3Int location)
    {
        return VoxelToWorld(location.x, location.y, location.z);
    }

    public static Vector3 VoxelToWorld(int x, int y, int z) {
        float newX = (((x / (float)VoxelSettings.voxelsPerMeter) - VoxelSettings.half));
        float newY = (((y / (float)VoxelSettings.voxelsPerMeter) - VoxelSettings.half));
        float newZ = (((z / (float)VoxelSettings.voxelsPerMeter) - VoxelSettings.half));
        return new Vector3(newX, newY, newZ);
    }

    public static Vector3Int WorldToVoxel(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(((worldPos.x) + VoxelSettings.half) * (float)VoxelSettings.voxelsPerMeter);
        int y = Mathf.FloorToInt(((worldPos.y) + VoxelSettings.half) * (float)VoxelSettings.voxelsPerMeter);
        int z = Mathf.FloorToInt(((worldPos.z) + VoxelSettings.half) * (float)VoxelSettings.voxelsPerMeter);
        return new Vector3Int(x, y, z);
    }
}

