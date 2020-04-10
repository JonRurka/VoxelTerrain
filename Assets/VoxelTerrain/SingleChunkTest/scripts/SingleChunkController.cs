using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using LibNoise;
using Debug = UnityEngine.Debug;

public class SingleChunkController : MonoBehaviour, IPageController
{
    public enum Samplers
    {
        Sphere,
        Terrain,
    }

    public GameObject chunkPrefab;
    public BlockType[] BlocksArray;

    public Texture2D[] SourceTextures;
    public Texture2DArray textureArray;
    public Texture2D linearTextureBlending;

    public int NumSourceTextures { get { return SourceTextures.Length; } }
    public Texture2DArray TextureArray { get { return textureArray; } }
    public GameObject ChunkPrefab { get { return chunkPrefab; } }
    public BlockType[] BlockTypes { get { return blockTypes; } }
    public Texture2D LinearTextureBlending { get { return linearTextureBlending; } }
    public ComputeBuffer TextureComputeBuffer { get; set; }

    public static SingleChunkController Instance { get; private set; }

    public static Dictionary<byte, BlockType> blockTypes_dict = new Dictionary<byte, BlockType>();
    public static BlockType[] blockTypes;

    public SafeDictionary<Vector3Int, SmoothChunk> Chunks = new SafeDictionary<Vector3Int, SmoothChunk>();


    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(System.Math.Round(0.014, 2, MidpointRounding.AwayFromZero) * 100);
        Init();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void CreateTextureBlend()
    {
        Color[] col = new Color[512*512];
        for (int x = 0; x < 512; x++)
        {
            for (int y = 0; y < 512; y++)
            {
                float x_smpl = x / 512f;
                float y_smpl = y / 512f;

                col[x * (512) + y] = new Color(x_smpl, y_smpl, 0);
            }
        }
        linearTextureBlending = new Texture2D(512, 512, TextureFormat.ARGB32, true);
        linearTextureBlending.SetPixels(col);
        linearTextureBlending.Apply();
    }

    public int[] textureArr;
    public void CreateTextureComputeBuffer()
    {
        //textureArr = new int[] {  };
        textureArr = new int[blockTypes.Length * 6];
        for (int i = 0; i < blockTypes.Length; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                textureArr[i * 6 + j] = blockTypes[i].textureIndex[j];
            }
        }

        Debug.Log(textureArr);
        TextureComputeBuffer = new ComputeBuffer(textureArr.Length, sizeof(int));
        TextureComputeBuffer.SetData(textureArr);

        //TextureComputeBuffer = new ComputeBuffer(1, sizeof(float));
        //TextureComputeBuffer.SetData(new float[] { 0.5f });
    }

    public void GenTextureArray()
    {
        int sourceWidth = SourceTextures[0].width;
        int sourceHeight = SourceTextures[0].height;

        textureArray = new Texture2DArray(sourceWidth, sourceHeight, SourceTextures.Length, TextureFormat.ARGB32, true);

        for (int i = 0; i < SourceTextures.Length; i++)
        {
            textureArray.SetPixels(SourceTextures[i].GetPixels(), i);
        }
        textureArray.Apply();
    }

    public void Init()
    {
        CreateTextureBlend();
        GenTextureArray();
        AddBlockType(BaseType.air, "Air", new int[] { -1, -1, -1, -1, -1, -1 }, null);
        AddBlockType(BaseType.solid, "Grass", new int[] { 0, 0, 0, 0, 0, 0 }, null);
        AddBlockType(BaseType.solid, "Soil", new int[] { 1, 0, 0, 0, 0, 0 }, null);
        AddBlockType(BaseType.solid, "MossyRock", new int[] { 2, 0, 0, 0, 0, 0 }, null);
        AddBlockType(BaseType.solid, "Sand", new int[] { 3, 0, 0, 0, 0, 0 }, null);
        InitBlockAccessOptimization();
        CreateTextureComputeBuffer();

        CreateChunk();
    }

    public void CreateChunk()
    {
        Debug.Log("Creating chunks...");

        //CreateSampler(Samplers.Sphere)

        SmoothChunk.CreateChunk(new Vector3Int(0, 0, 0), CreateSampler(Samplers.Sphere), this);
        //SmoothChunk.CreateChunk(new Vector3Int(1, 0, 0), CreateSampler(Samplers.Sphere), this);

    }

    public static ISampler CreateSampler(Samplers type)
    {
        ISampler result = null;
        switch(type)
        {
            case Samplers.Sphere:
                result = new SphereSampler(new Vector3(SmoothVoxelSettings.MeterSizeX / 2f, SmoothVoxelSettings.MeterSizeY / 2f, SmoothVoxelSettings.MeterSizeZ / 2f),
                                                    SmoothVoxelSettings.MeterSizeX / 3f);
                break;

            case Samplers.Terrain:
                TerrainModule module = new TerrainModule(SmoothVoxelSettings.seed);
                result = new TerrainSampler(module,
                                            SmoothVoxelSettings.seed,
                                            SmoothVoxelSettings.enableCaves,
                                            SmoothVoxelSettings.amplitude,
                                            SmoothVoxelSettings.caveDensity,
                                            SmoothVoxelSettings.grassOffset);
                break;
        }
        return result;
    }

    public Block GetBlock(Vector3Int location)
    {
        return GetBlock(location.x, location.y, location.z);
    }

    public Block GetBlock(int x, int y, int z)
    {
        Vector3Int chunk = VoxelConversions.VoxelToChunk(new Vector3Int(x, y, z));
        Vector3Int localVoxel = VoxelConversions.GlobalToLocalChunkCoord(chunk, new Vector3Int(x, y, z));
        Block result = default(Block);
        if (BuilderExists(chunk.x, chunk.y, chunk.z))
        {
            result = Chunks[chunk].GetBlock(localVoxel.x, localVoxel.y, localVoxel.z);
        }
        return result;
    }

    public void AddBlockType(BaseType _baseType, string _name, int[] _textures, GameObject _prefab)
    {
        byte index = (byte)blockTypes_dict.Count;
        blockTypes_dict.Add(index, new BlockType(_baseType, index, _name, _textures, _prefab));
        BlocksArray = blockTypes_dict.Values.ToArray();
    }

    public void InitBlockAccessOptimization()
    {
        blockTypes = new BlockType[blockTypes_dict.Count];
        foreach (byte index in blockTypes_dict.Keys.ToArray())
        {
            blockTypes[index] = blockTypes_dict[index];
        }
        SetBlockTypeScaledTextureIndices(SourceTextures.Length);
    }

    public void SetBlockTypeScaledTextureIndices(int length)
    {
        for (int i = 0; i < blockTypes.Length; i++)
        {
            blockTypes[i].SetScaledIndices(length);
        }
    }

    public bool BuilderExists(int x, int y, int z)
    {
        return Chunks.ContainsKey(new Vector3Int(x, y, z));
    }

    public bool BuilderGenerated(int x, int y, int z)
    {
        if (BuilderExists(x, y, z))
        {
            return Chunks[new Vector3Int(x, y, z)].Generated;
        }
        return false;
    }

    public IVoxelBuilder GetBuilder(int x, int y, int z)
    {
        if (BuilderExists(x, y, z))
        {
            return Chunks[new Vector3Int(x, y, z)].builder;
        }
        return null;
    }

    public GameObject getGameObject()
    {
        return gameObject;
    }

    public void DoBlendPass(List<Vector3Int> chunkPositions, bool full_gen)
    {
        foreach(Vector3Int loc in chunkPositions)
        {
            if (BuilderExists(loc.x, loc.y, loc.z))
            {
                Chunks[loc].BuildGPU_DataBuffer(full_gen);
            }
        }
    }

    public void UpdateChunk(int x, int y, int z)
    {
        if (BuilderExists(x, y, z))
        {
            Chunks[new Vector3Int(x, y, z)].Render(true);
        }
    }

    public void AddChunk(Vector3Int pos, IChunk chunk)
    {
        Chunks[pos] = (SmoothChunk)chunk;
        
        Chunks[pos].BuildGPU_DataBuffer(true);

        Vector3Int[] dirs = new Vector3Int[]{
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        foreach (Vector3Int d in dirs)
        {
            Vector3Int newPos = pos + d;
            if (BuilderExists(newPos.x, newPos.y, newPos.z))
            {
                Chunks[newPos].Render(true);
                Chunks[newPos].BuildGPU_DataBuffer(true);
            }
        }
    }
}
