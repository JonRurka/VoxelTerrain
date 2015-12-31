using System;
using UnityEngine;
using System.Collections;

public interface IVoxelBuilder : IDisposable {
    void SetBlockTypes(BlockType[] blockTypeList, Rect[] AtlasUvs);
    MeshData Render(bool renderOnly);
    void Generate(int seed, bool enableCaves, float amp, float caveDensity, float grassOffset);
    void SetBlock(int x, int y, int z, Block block);
    Block GetBlock(int x, int y, int z);
    void SetSurroundingChunks();
}
