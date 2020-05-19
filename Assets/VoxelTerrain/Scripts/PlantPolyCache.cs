using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlantPolyCache
{
    public static int[] indices_4x4 { get; private set; }
    public static int[] indices_2x2 { get; private set; }
    public static int[] indices_1x1 { get; private set; }

    public static int[] GetQuadTris(int offset, int num)
    {
        int tri_offset = num * 8;

        int[] result = new int[12];
        result[0] = offset + tri_offset + 0;
        result[1] = offset + tri_offset + 1;
        result[2] = offset + tri_offset + 2;
        result[3] = offset + tri_offset + 2;
        result[4] = offset + tri_offset + 1;
        result[5] = offset + tri_offset + 3;

        result[6] = offset + tri_offset + 2;
        result[7] = offset + tri_offset + 1;
        result[8] = offset + tri_offset + 0;
        result[9] = offset + tri_offset + 3;
        result[10] = offset + tri_offset + 1;
        result[11] = offset + tri_offset + 2;

        return result;
    }

    public static int[] GenIndices(int size_x, int size_z)
    {
        List<int> tris = new List<int>();

        int verts = 0;
        for (int x = 0; x < size_x; x++)
        {
            for (int z = 0; z < size_z; z++)
            {
                int offset = verts;

                tris.AddRange(GetQuadTris(offset, 0));
                verts += 8;

                tris.AddRange(GetQuadTris(offset, 1));
                verts += 8;

                tris.AddRange(GetQuadTris(offset, 2));
                verts += 8;
            }
        }

        return tris.ToArray();
    }

    public static void Init()
    {
        indices_4x4 = GenIndices((SmoothVoxelSettings.ChunkSizeX / 2) * 4, (SmoothVoxelSettings.ChunkSizeZ / 2) * 4);
        indices_2x2 = GenIndices((SmoothVoxelSettings.ChunkSizeX / 2) * 2, (SmoothVoxelSettings.ChunkSizeZ / 2) * 2);
        indices_1x1 = GenIndices((SmoothVoxelSettings.ChunkSizeX / 2) * 1, (SmoothVoxelSettings.ChunkSizeZ / 2) * 1);
    }
}
