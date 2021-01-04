using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public class StandardColumnBuilder : IColumnBuilder
{
    public ISampler Sampler { get; private set; }

    public ColumnResult Result { get; private set; }

    public Vector3Int Location { get; private set; }

    public bool SurfaceGenerated { get; private set; }

    public bool GPU_Accelorated_Noise {
        get{
            return (Sampler is GPU_TerrainSampler);
        }
    }

    private float VoxelsPerMeter;
    private int ChunkMeterSizeX;
    private int ChunkMeterSizeY;
    private int ChunkMeterSizeZ;
    private int ChunkSizeX;
    private int ChunkSizeY;
    private int ChunkSizeZ;
    private int skipDist;
    private float half;
    private float xSideLength;
    private float ySideLength;
    private float zSideLength;
    private bool deactivated;

    private static Vector3Int[] directionOffsets = new Vector3Int[8]
    {
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 0, 0),
            new Vector3Int(0, 1, 1),
            new Vector3Int(1, 1, 1),
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 1, 0),
    };

    private Vector3[] locOffset;
    private Vector3Int[] globalOffsets;

    #region edgeTable
    private static int[] edgeTable = new int[256]
        {0x0 , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
        0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
        0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
        0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
        0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
        0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
        0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
        0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
        0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
        0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
        0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
        0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
        0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
        0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
        0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
        0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
        0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0};
    #endregion

    public StandardColumnBuilder()
    {
        Result = new ColumnResult();
    }

    public StandardColumnBuilder(ColumnResult result)
    {
        Result = result;
    }

    public StandardColumnBuilder(ISampler sampler)
    {
        Result = new ColumnResult();
        Sampler = sampler;
    }

    public StandardColumnBuilder(ColumnResult result, ISampler sampler)
    {
        Result = result;
        Sampler = sampler;
    }

    public void Init(Vector3Int location, float voxelsPerMeter, int chunkMeterSizeX, int chunkMeterSizeY, int chunkMeterSizeZ)
    {
        Location = location;

        VoxelsPerMeter = voxelsPerMeter;
        ChunkMeterSizeX = chunkMeterSizeX;
        ChunkMeterSizeY = chunkMeterSizeY;
        ChunkMeterSizeZ = chunkMeterSizeZ;

        ChunkSizeX = (int)(ChunkMeterSizeX * VoxelsPerMeter);
        ChunkSizeY = (int)(ChunkMeterSizeY * VoxelsPerMeter);
        ChunkSizeZ = (int)(ChunkMeterSizeZ * VoxelsPerMeter);

        half = ((1.0f / (float)VoxelsPerMeter) / 2.0f);
        xSideLength = ChunkMeterSizeX / (float)ChunkSizeX;
        ySideLength = ChunkMeterSizeY / (float)ChunkSizeY;
        zSideLength = ChunkMeterSizeZ / (float)ChunkSizeZ;
        skipDist = Mathf.RoundToInt(1 / (float)VoxelsPerMeter);

        locOffset = new Vector3[8]
        {
            new Vector3(0,0,zSideLength),
            new Vector3(xSideLength,0,zSideLength),
            new Vector3(xSideLength,0,0),
            new Vector3(0,0,0),
            new Vector3(0,ySideLength,zSideLength),
            new Vector3(xSideLength,ySideLength,zSideLength),
            new Vector3(xSideLength,ySideLength,0),
            new Vector3(0,ySideLength,0),
        };
        globalOffsets = new Vector3Int[8]
        {
            new Vector3Int(0, 0, 1) * skipDist,
            new Vector3Int(1, 0, 1) * skipDist,
            new Vector3Int(1, 0, 0) * skipDist,
            new Vector3Int(0, 0, 0) * skipDist,
            new Vector3Int(0, 1, 1) * skipDist,
            new Vector3Int(1, 1, 1) * skipDist,
            new Vector3Int(1, 1, 0) * skipDist,
            new Vector3Int(0, 1, 0) * skipDist,
        };

        UnityGameServer.Logger.Log("yes: " + ChunkMeterSizeX);
        //SurfaceGenerated = Result.SurfaceData != null;
    }

    public float[] GenerateHeightMap()
    {
        if (!SurfaceGenerated)
        {
            Vector2Int bottomLeft = new Vector2(Location.x * ChunkSizeX, Location.z * ChunkSizeZ);
            Vector2Int topRight = new Vector2(Location.x * ChunkSizeX + ChunkSizeX, Location.z * ChunkSizeZ + ChunkSizeZ);
            Result.SurfaceData = Sampler.SetSurfaceData(bottomLeft, topRight);
            SurfaceGenerated = true;

            Result.Min = (int)Sampler.GetMin();
            Result.Max = (int)Sampler.GetMax();

            return Result.SurfaceData;
        }
        return Result.SurfaceData;
    }

    public ColumnResult Generate(int Y_Min, int Y_Max, int xStart, int zStart)
    {
        Result.Min = int.MaxValue;
        Result.Max = int.MinValue;

        Stopwatch watch_overall = new Stopwatch();
        watch_overall.Start();

        if (GPU_Accelorated_Noise)
        {
            UnityGameServer.Logger.Log("Using GPU sampler.");
            ((GPU_TerrainSampler)Sampler).ComputeNoiseGrid(Y_Min, Y_Max, xStart, zStart);
        }

        int globalLocY = 0;
        int globalLocZ = 0;
        int globalLocX = 0;
        int x, y, z = 0;

        for (globalLocY = Y_Min, y = Y_Min; y < Y_Max; globalLocY++, y++)
        {
            Result.Max = Mathf.Max(y, Result.Max);
            Result.Min = Mathf.Min(y, Result.Min);

            for (globalLocZ = zStart, z = 0; z < ChunkSizeZ; globalLocZ++, z++)
            {
                for (globalLocX = xStart, x = 0; x < ChunkSizeX; globalLocX++, x++)
                {
                    if (deactivated)
                    {
                        UnityGameServer.Logger.Log("DEACTIVATED!!!");
                        break;
                    }

                    

                    Vector3 worldPos = new Vector3(x * xSideLength, y * ySideLength, z * zSideLength);
                    Vector3Int globalPos = new Vector3Int(globalLocX * skipDist, globalLocY * skipDist, globalLocZ * skipDist);
                    Vector3Int localPos = new Vector3Int(x, y, z);
                    GridPoint[] grid = new GridPoint[8];
                    for (int i = 0; i < grid.Length; i++)
                        grid[i] = GetGridPoint(worldPos + locOffset[i], localPos + directionOffsets[i], globalPos + globalOffsets[i]);
                    ProcessBlock(grid, 0);
                    //GeneratedBlocks++;
                }
            }
        }

        watch_overall.Stop();

        UnityGameServer.Logger.Log("GPU_ColumnBuilder Generate(): Overall: {0}", watch_overall.Elapsed);

        if (GPU_Accelorated_Noise)
            ((GPU_TerrainSampler)Sampler).Dispose_ISO_Types();

        UnityGameServer.Logger.Log("Min: {0}, Max: {1}", Sampler.GetMin(), Sampler.GetMax());
        UnityGameServer.Logger.Log("Res.Min: {0}, Res.Max: {1}", Result.Min, Result.Max);
        UnityGameServer.Logger.Log("Y_Min: {0}, Y_Max: {1}", Y_Min, Y_Max);

        return Result;
    }

    public void ProcessBlock(GridPoint[] grid, float isoLevel)
    {
        int cubeIndex = 0;
        if (grid[0].iso > isoLevel) cubeIndex |= 1;
        if (grid[1].iso > isoLevel) cubeIndex |= 2;
        if (grid[2].iso > isoLevel) cubeIndex |= 4;
        if (grid[3].iso > isoLevel) cubeIndex |= 8;
        if (grid[4].iso > isoLevel) cubeIndex |= 16;
        if (grid[5].iso > isoLevel) cubeIndex |= 32;
        if (grid[6].iso > isoLevel) cubeIndex |= 64;
        if (grid[7].iso > isoLevel) cubeIndex |= 128;

        if (edgeTable[cubeIndex] == 0)
            return;


        if ((edgeTable[cubeIndex] & 1) != 0)
            MarkSurfaceBlocks(grid[0], grid[1]);
        if ((edgeTable[cubeIndex] & 2) != 0)
            MarkSurfaceBlocks(grid[1], grid[2]);
        if ((edgeTable[cubeIndex] & 4) != 0)
            MarkSurfaceBlocks(grid[2], grid[3]);
        if ((edgeTable[cubeIndex] & 8) != 0)
            MarkSurfaceBlocks(grid[3], grid[0]);

        if ((edgeTable[cubeIndex] & 16) != 0)
            MarkSurfaceBlocks(grid[4], grid[5]);
        if ((edgeTable[cubeIndex] & 32) != 0)
            MarkSurfaceBlocks(grid[5], grid[6]);
        if ((edgeTable[cubeIndex] & 64) != 0)
            MarkSurfaceBlocks(grid[6], grid[7]);
        if ((edgeTable[cubeIndex] & 128) != 0)
            MarkSurfaceBlocks(grid[7], grid[4]);

        if ((edgeTable[cubeIndex] & 256) != 0)
            MarkSurfaceBlocks(grid[0], grid[4]);
        if ((edgeTable[cubeIndex] & 512) != 0)
            MarkSurfaceBlocks(grid[1], grid[5]);
        if ((edgeTable[cubeIndex] & 1024) != 0)
            MarkSurfaceBlocks(grid[2], grid[6]);
        if ((edgeTable[cubeIndex] & 2048) != 0)
            MarkSurfaceBlocks(grid[3], grid[7]);
    }

    public void MarkSurfaceBlocks(GridPoint p1, GridPoint p2)
    {
        byte[] loc_bytes;

        if (p1.type == byte.MaxValue || p2.type == byte.MaxValue)
            return;

        ushort loc_p1 = (ushort)Get_Flat_Index(p1.OriginLocal.x, p1.OriginLocal.y, p1.OriginLocal.z);
        if (IsInBounds(p1.OriginLocal.x, p1.OriginLocal.y, p1.OriginLocal.z) && !Result.blocks_surface[loc_p1])
        {
            //Vector3 org = 
            Vector3 other = new Vector3(p2.x, p2.y, p2.z);
            //UnityEngine.Debug.DrawLine(new Vector3(p1.x, p1.y, p1.z), other, UnityEngine.Color.red, 50000);
            byte type_P1 = (byte)p1.type;
            byte iso_p1 = (byte)VoxelConversions.Scale(Mathf.Clamp(p1.iso, -2, 2), -2, 2, byte.MinValue, byte.MaxValue);
            loc_bytes = BitConverter.GetBytes(loc_p1);
            Result.surfaceBlocks[Result.surfaceBlocksCount] = (BitConverter.ToUInt32(new byte[] { loc_bytes[0], loc_bytes[1], type_P1, iso_p1 }, 0));
            Result.blocks_surface[loc_p1] = true;
            Result.surfaceBlocksCount++;
        }

        

        ushort loc_p2 = (ushort)Get_Flat_Index(p2.OriginLocal.x, p2.OriginLocal.y, p2.OriginLocal.z);
        if (IsInBounds(p2.OriginLocal.x, p2.OriginLocal.y, p2.OriginLocal.z) && !Result.blocks_surface[loc_p2])
        {
            Vector3 other = new Vector3(p1.x, p1.y, p1.z);
            //UnityEngine.Debug.DrawLine(new Vector3(p2.x, p2.y, p2.z), other, UnityEngine.Color.red, 50000);
            byte type_P2 = (byte)p2.type;
            byte iso_p2 = (byte)VoxelConversions.Scale(Mathf.Clamp(p2.iso, -2, 2), -2, 2, byte.MinValue, byte.MaxValue);
            loc_bytes = BitConverter.GetBytes(loc_p2);
            Result.surfaceBlocks[Result.surfaceBlocksCount] = (BitConverter.ToUInt32(new byte[] { loc_bytes[0], loc_bytes[1], type_P2, iso_p2 }, 0));
            Result.blocks_surface[loc_p2] = true;
            Result.surfaceBlocksCount++;
        }
    }

    public GridPoint GetGridPoint(Vector3 world, Vector3Int local, Vector3Int global)
    {
        GridPoint result = default(GridPoint);
        uint type;

        if (IsInBounds(local.x, local.y, local.z))
        {
            result = new GridPoint(world.x, world.y, world.z, (float)GetIsoValue(local, global, true, out type), type);
            
        }
        else
        {
            type = byte.MaxValue;
            result = new GridPoint(world.x, world.y, world.z, -1f, type);
            //result = new GridPoint(world.x, world.y, world.z, (float)Sampler.GetIsoValue(local, global, out type), type);
        }

        result.OriginLocal = local;
        result.OriginGlobal = global;
        //UnityGameServer.Logger.Log(result.iso);
        return result;
    }

    public double GetIsoValue(Vector3Int LocalPosition, Vector3Int globalLocation, bool generate, out uint type)
    {
        if (generate && !IsBlockSet(LocalPosition.x, LocalPosition.y, LocalPosition.z))
        {
            float generatedValue = (float)GetIsoValue(LocalPosition.x, LocalPosition.y, LocalPosition.z, globalLocation.x, globalLocation.y, globalLocation.z, out type);
            SetBlock(LocalPosition.x, LocalPosition.y, LocalPosition.z, new Block((byte)type, generatedValue));
            MarkAsSet(LocalPosition.x, LocalPosition.y, LocalPosition.z);
        }
        Block res = GetBlock(LocalPosition.x, LocalPosition.y, LocalPosition.z);
        type = res.type;
        return res.iso;
    }

    public double GetIsoValue(int LocalPositionX, int LocalPositionY, int LocalPositionZ, int globalX, int globalY, int globalZ, out uint type)
    {
        return Sampler.GetIsoValue(new Vector3Int(LocalPositionX, LocalPositionY, LocalPositionZ), new Vector3Int(globalX, globalY, globalZ), out type);
    }

    public void MarkAsSet(int x, int y, int z)
    {
        if (!deactivated)
            Result.blocks_set[Get_Flat_Index(x, y, z)] = true;
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        if (!deactivated)
        {
            int index = Get_Flat_Index(x, y, z);
            Result.blocks_type[index] = (byte)block.type;
            Result.blocks_iso[index] = block.iso;
        }
    }

    public Block GetBlock(int x, int y, int z)
    {
        if (!deactivated)
        {
            //if (IsInBounds(x, y, z)) {
            x = Mathf.Clamp(x, 0, ChunkSizeX);
            y = Mathf.Clamp(y, 0, ChunkSizeY);
            z = Mathf.Clamp(z, 0, ChunkSizeZ);

            int index = Get_Flat_Index(x, y, z);

            Block res = new Block(Result.blocks_type[index], Result.blocks_iso[index]);
            res.set = Result.blocks_set[index];

            return res;
        }
        return default(Block);
    }

    public int Get_Flat_Index(int x, int y, int z)
    {
        return x + ChunkSizeX * (y + ChunkSizeY * z);
    }

    public void SetBlockValue(int x, int y, int z, float value)
    {
        if (!deactivated)
        {
            Result.blocks_iso[Get_Flat_Index(x, y, z)] = value;
        }
    }

    public float GetBlockValue(int x, int y, int z)
    {
        if (!deactivated)
        {
            return Result.blocks_iso[Get_Flat_Index(x, y, z)];
        }
        return 0;
    }

    public bool IsBlockSet(int x, int y, int z)
    {
        if (!deactivated)
            return Result.blocks_set[Get_Flat_Index(x, y, z)];
        return false;
    }

    public bool IsInBounds(int x, int y, int z)
    {
        return ((x < ChunkSizeX) && x >= 0) && ((y < ChunkSizeY) && y >= 0) && ((z < ChunkSizeZ) && z >= 0);
    }


    public void Dispose()
    {
        deactivated = true;
        if (Sampler != null)
            Sampler.Dispose();
        Result.Dispose();
    }
}
