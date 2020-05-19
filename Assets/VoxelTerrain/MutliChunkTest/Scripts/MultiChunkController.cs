using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using LibNoise;
using Debug = UnityEngine.Debug;

public class MultiChunkController : MonoBehaviour, IPageController
{
    public GameObject chunkPrefab;
    public BlockType[] BlocksArray;

    public Texture2D[] SourceTextures;
    public Texture2DArray textureArray;
    public Texture2D linearTextureBlending;



    public int width = 10;
    public int height = 5;
    public int depth = 10;
    public int chunksGenerated;
    public float Ambience = 1;

    public int NumSourceTextures { get { return SourceTextures.Length; } }
    public Texture2DArray TextureArray { get { return textureArray; } }
    public GameObject ChunkPrefab { get { return chunkPrefab; } }
    public BlockType[] BlockTypes { get { return blockTypes; } }
    public Texture2D LinearTextureBlending { get { return linearTextureBlending; } }
    public ComputeBuffer TextureComputeBuffer { get; set; }

    public static float T;

    public GameObject ColumnObj;

    public static MultiChunkController Instance { get; private set; }

    public static Dictionary<byte, BlockType> blockTypes_dict = new Dictionary<byte, BlockType>();
    public static BlockType[] blockTypes;

    public SafeDictionary<Vector2Int, ChunkColumn> Columns = new SafeDictionary<Vector2Int, ChunkColumn>();
    public SafeDictionary<Vector3Int, SmoothChunk> Chunks = new SafeDictionary<Vector3Int, SmoothChunk>();


    public delegate void GenerateComplete(Vector3Int[] chunks);
    public event GenerateComplete OnChunksGenerated;

    private bool _generating = false;
    private bool renderCompleteCalled = false;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(5 % 4);

        //SmoothVoxelSettings.seed = DateTime.Now.Millisecond;
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        RenderSettings.ambientIntensity = Ambience;
        T += Time.deltaTime;
        Shader.SetGlobalFloat("__Time", T);
    }

    public void Init()
    {
        Stopwatch plant_cache_watch = new Stopwatch();
        plant_cache_watch.Start();
        PlantPolyCache.Init();
        plant_cache_watch.Stop();
        Debug.Log("Plant cache generated in: " + plant_cache_watch.Elapsed);

        GenTextureArray();
        AddBlockType(BaseType.air, "Air", new int[] { -1, -1, -1, -1, -1, -1 }, null);
        AddBlockType(BaseType.solid, "Grass", new int[] { 0, 0, 0, 0, 0, 0 }, null);
        AddBlockType(BaseType.solid, "Soil", new int[] { 1, 0, 0, 0, 0, 0 }, null);
        AddBlockType(BaseType.solid, "MossyRock", new int[] { 2, 0, 0, 0, 0, 0 }, null);
        //AddBlockType(BaseType.solid, "Sand", new int[] { 3, 0, 0, 0, 0, 0 }, null);
        InitBlockAccessOptimization();
        CreateTextureComputeBuffer();

        GenerateChunks();
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

    public void AddBlockType(BaseType _baseType, string _name, int[] _textures, GameObject _prefab)
    {
        byte index = (byte)blockTypes_dict.Count;
        blockTypes_dict.Add(index, new BlockType(_baseType, index, _name, _textures, _prefab));
        InitBlockAccessOptimization();
        //blockTypes = blockTypes_dict.Values.ToArray();
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

    public void GenerateChunks()
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        List<Vector2Int> colCoords = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int col_pos = new Vector2Int(x, z);
                if (!Columns.ContainsKey(col_pos))
                {
                    GameObject obj = Instantiate(ColumnObj, VoxelConversions.ChunkCoordToWorld(new Vector3Int(x, 0, z)), Quaternion.identity, transform);
                    obj.name = "Column " + col_pos.ToString();
                    Columns[col_pos] = obj.GetComponent<ChunkColumn>();
                    Columns[col_pos].Init(col_pos, this, CreateTerrainSampler());
                }
                colCoords.Add(col_pos);

                /*for (int y = 0; y < height; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    chunkCoords.Add(pos);

                    Vector2Int col_pos = new Vector2Int(pos.x, pos.z);
                    if (!Columns.ContainsKey(col_pos))
                    {
                        GameObject obj = new GameObject("Column " + col_pos.ToString(), typeof(ChunkColumn));
                        Columns[col_pos] = obj.GetComponent<ChunkColumn>();
                        Columns[col_pos].Init(20, col_pos, CreateTerrainSampler());
                    }
                }*/
            }
        }
        watch.Stop();
        Debug.Log("Finished spawning columns: " + watch.Elapsed);
        GenerateChunks("Gen", colCoords.ToArray());
    }

    public void GenerateChunks(string threadName, Vector2Int[] columns)
    {
        Loom.QueueAsyncTask(threadName, () =>
        {

            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                for (int i = 0; i < columns.Length; i++)
                {
                    _generating = true;
                    Columns[columns[i]].Generate(height);
                    chunksGenerated+=height;
                }
                watch.Stop();
                Debug.Log("Finished generating chunks in: " + watch.Elapsed);

                if (!renderCompleteCalled)
                {
                    renderCompleteCalled = true;
                    //if (OnChunksGenerated != null)
                    //    Loom.QueueOnMainThread(() => { OnChunksGenerated(columns); });
                }
                SpawnGrass();
                _generating = false;
            }
            catch (Exception e)
            {
                SafeDebug.LogError(string.Format("{0}: {1}\n {2}", e.GetType().ToString(), e.Message, e.StackTrace));
            }
        });
    }

    public void SpawnGrass()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {

            }
        }
    }

    public static ISampler CreateTerrainSampler()
    {
        TerrainModule module = new TerrainModule(SmoothVoxelSettings.seed);
        ISampler result = new TerrainSampler(module,
                                    SmoothVoxelSettings.seed,
                                    SmoothVoxelSettings.enableCaves,
                                    SmoothVoxelSettings.amplitude,
                                    SmoothVoxelSettings.caveDensity,
                                    SmoothVoxelSettings.grassOffset);
        return result;
    }

    public void AddChunk(Vector3Int pos, IChunk chunk)
    {
        Columns[new Vector2Int(pos.x, pos.z)].AddChunk(pos.y, chunk);
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

        /*Loom.QueueAsyncTask("ChunkUpdate", () =>
        {
            foreach (Vector3Int d in dirs)
            {
                Vector3Int newPos = pos + d;
                if (BuilderExists(newPos.x, newPos.y, newPos.z))
                {
                    Chunks[newPos].Render(true);
                    Chunks[newPos].BuildGPU_DataBuffer(true);
                }
            }
        });*/
    }

    public bool BuilderExists(int x, int y, int z)
    {
        if (Chunks.ContainsKey(new Vector3Int(x, y, z)))
        {
            return Chunks[new Vector3Int(x, y, z)];
        }
        return false;
    }

    public bool BuilderGenerated(int x, int y, int z)
    {
        if (BuilderExists(x, y, z))
        {
            return Chunks[new Vector3Int(x, y, z)].Generated;
        }
        return false;
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

    public void UpdateChunk(int x, int y, int z)
    {
        if (BuilderExists(x, y, z))
        {
            Chunks[new Vector3Int(x, y, z)].Render(true);
        }
    }
}
