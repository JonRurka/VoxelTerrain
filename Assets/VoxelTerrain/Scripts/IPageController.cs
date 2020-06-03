using UnityEngine;
using System.Collections;

public interface IPageController {
    int NumSourceTextures { get; }
    Texture2DArray TextureArray { get; }
    GameObject ChunkPrefab { get; }
    BlockType[] BlockTypes { get; }
    ComputeBuffer TextureComputeBuffer { get; }

    GameObject getGameObject();
    bool BuilderExists(int x, int y, int z);
    bool BuilderGenerated(int x, int y, int z);
    IVoxelBuilder GetBuilder(int x, int y, int z);
    void UpdateChunk(int x, int y, int z);
    void AddBlockType(BaseType _baseType, string _name, Color col, int[] _textures, GameObject _prefab);
    Block GetBlock(int x, int y, int z);
    void AddChunk(Vector3Int pos, IChunk chunk);
}
