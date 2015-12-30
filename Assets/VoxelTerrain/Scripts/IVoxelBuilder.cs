using System;
using UnityEngine;
using System.Collections;

public interface IVoxelBuilder : IDisposable {
    void SetBlockTypes(BlockType[] _blockTypeList, Rect[] _AtlasUvs);
    MeshData Render(bool renderOnly);
    float[] Generate(int _seed, bool _enableCaves, float _amp, float _caveDensity, float _groundOffset, float _grassOffset);
    void SetBlock(int _x, int _y, int _z, Block block);
    Block GetBlock(int _x, int _y, int _z);
    void SetSurroundingChunks();
}
