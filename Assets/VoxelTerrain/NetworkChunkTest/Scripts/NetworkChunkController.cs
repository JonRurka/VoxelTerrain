using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkChunkController : MonoBehaviour, IPageController
{
    public int NumSourceTextures => throw new System.NotImplementedException();

    public Texture2DArray TextureArray => throw new System.NotImplementedException();

    public GameObject ChunkPrefab => throw new System.NotImplementedException();

    public BlockType[] BlockTypes => throw new System.NotImplementedException();

    public ComputeBuffer TextureComputeBuffer => throw new System.NotImplementedException();

    private GameClient GameClient;

    // Start is called before the first frame update
    void Start()
    {
        System.Byte dir = 7;
        System.Byte tst = 2;

        byte val = (byte)(tst | (dir << 5));
        

        Debug.Log("flag: " + BufferUtils.PrintFlag(val));

        int res_dir = val >> 5;
        Debug.Log("Dir result: " + res_dir);

        int res_type = val & 31;
        Debug.Log("Dir result: " + res_type);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameClient != null)
            GameClient.Update();
    }

    public void Init()
    {
        Debug.Log("Starting game client...");
        GameClient = new GameClient();
        GameClient.Connect();
    }

    public void AddBlockType(BaseType _baseType, string _name, Color col, int[] _textures, GameObject _prefab)
    {
        throw new System.NotImplementedException();
    }

    public void AddChunk(Vector3Int pos, IChunk chunk)
    {
        throw new System.NotImplementedException();
    }

    public bool BuilderExists(int x, int y, int z)
    {
        throw new System.NotImplementedException();
    }

    public bool BuilderGenerated(int x, int y, int z)
    {
        throw new System.NotImplementedException();
    }

    public Block GetBlock(int x, int y, int z)
    {
        throw new System.NotImplementedException();
    }

    public IVoxelBuilder GetBuilder(int x, int y, int z)
    {
        throw new System.NotImplementedException();
    }

    public GameObject getGameObject()
    {
        throw new System.NotImplementedException();
    }

    public void UpdateChunk(int x, int y, int z)
    {
        throw new System.NotImplementedException();
    }
}
