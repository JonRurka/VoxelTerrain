using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IColumnBuilder : IDisposable
{
    float[] GenerateHeightMap();

    ColumnResult Generate(int Y_Min, int Y_Max, int xStart, int zStart);
}

public class ColumnResult : IDisposable
{
    public byte[] blocks_type { get; set; }

    public float[] SurfaceData { get; set; }

    public float[] blocks_iso { get; set; }

    public bool[] blocks_set { get; set; }

    public bool[] blocks_surface { get; set; }

    public uint[] surfaceBlocks { get; set; }

    public int Min { get; set; }
    public int Max { get; set; }

    public int surfaceBlocksCount { get; set; }

    private bool allocated;

    public void Allocate(int ChunkSizeX, int ChunkSizeY, int ChunkSizeZ)
    {
        if (allocated)
            return;
        allocated = true;

        int size = ChunkSizeX * ChunkSizeY * ChunkSizeZ;
        blocks_iso = new float[size];
        blocks_type = new byte[size];
        blocks_set = new bool[size];
        blocks_surface = new bool[size];
        surfaceBlocks = new uint[size];
    }

    public void Dispose()
    {
        blocks_type = null;
        SurfaceData = null;
        blocks_iso = null;
        blocks_set = null;
        blocks_surface = null;
        surfaceBlocks = null;
        surfaceBlocksCount = -1;
    }
}
