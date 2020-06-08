using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using LibNoise;
using Debug = UnityEngine.Debug;

public class NetworkChunkController : MonoBehaviour, IPageController
{
    public GameObject chunkPrefab;
    public BlockType[] BlocksArray;

    public VoxelMaterial[] SourceMaterials;

    public Texture2D[] SourceTextures;
    public Texture2DArray textureArray;
    public Texture2D linearTextureBlending;

    public int cores = 1;

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

    public ComputeBuffer TypeColorsComputeBuffer { get; set; }

    public static float T;

    public GameObject ColumnObj;

    public static NetworkChunkController Instance { get; private set; }

    public static Dictionary<byte, BlockType> blockTypes_dict = new Dictionary<byte, BlockType>();
    public static BlockType[] blockTypes;

    public SafeDictionary<Vector2Int, ChunkColumn> Columns = new SafeDictionary<Vector2Int, ChunkColumn>();
    public SafeDictionary<Vector3Int, SmoothChunk> Chunks = new SafeDictionary<Vector3Int, SmoothChunk>();


    public delegate void GenerateComplete(Vector3Int[] chunks);
    public event GenerateComplete OnChunksGenerated;

    private bool _generating = false;
    private bool renderCompleteCalled = false;

    private GameClient gameClient;

    // Start is called before the first frame update
    void Start()
    {
        /*System.Byte dir = 7;
        System.Byte tst = 2;

        byte val = (byte)(tst | (dir << 5));
        

        Debug.Log("flag: " + BufferUtils.PrintFlag(val));

        int res_dir = val >> 5;
        Debug.Log("Dir result: " + res_dir);

        int res_type = val & 31;
        Debug.Log("Dir result: " + res_type);*/
    }

    // Update is called once per frame
    void Update()
    {
        if (gameClient != null)
            gameClient.Update();
    }

    public void Init()
    {
        Instance = this;
        Debug.Log("Starting game client...");
        gameClient = new GameClient();
        gameClient.OnIdentified += GenerateChunksCircular;
        gameClient.Connect();

        Stopwatch plant_cache_watch = new Stopwatch();
        plant_cache_watch.Start();
        PlantPolyCache.Init();
        plant_cache_watch.Stop();
        Debug.Log("Plant cache generated in: " + plant_cache_watch.Elapsed);


        AddBlockType(BaseType.air, "Air", new Color(), new int[] { -1, -1, -1, -1, -1, -1 }, null);

        List<Texture2D> s_tex = new List<Texture2D>();
        for (int i = 0; i < SourceMaterials.Length; i++)
        {
            int[] _tex_inds = new int[6];
            for (int j = 0; j < SourceMaterials[i].Textures.Length; j++)
            {
                s_tex.Add(SourceMaterials[i].Textures[j]);
                _tex_inds[j] = s_tex.Count - 1;
            }
            AddBlockType(SourceMaterials[i].Type, SourceMaterials[i].Name, SourceMaterials[i].Color, _tex_inds, null);
        }
        SourceTextures = s_tex.ToArray();

        //AddBlockType(BaseType.solid, "Grass", new int[] { 0, 0, 0, 0, 0, 0 }, null);
        //AddBlockType(BaseType.solid, "Soil", new int[] { 1, 0, 0, 0, 0, 0 }, null);
        //AddBlockType(BaseType.solid, "MossyRock", new int[] { 2, 0, 0, 0, 0, 0 }, null);
        //AddBlockType(BaseType.solid, "Sand", new int[] { 3, 0, 0, 0, 0, 0 }, null);

        GenTextureArray();

        InitBlockAccessOptimization();
        CreateTextureComputeBuffer();

        //GenerateChunksCircular();
    }

    public void GenerateChunksCircular()
    {

        /*for (int i = 0; i < SmoothVoxelSettings.radius; i++)
        {
            Vector3Int[] chunkBand = GetChunkLocationsAroundPoint(i, Vector3.zero);
            
        }*/

        GenerateChunks(new Vector2Int[] { new Vector2Int(0, 0) });
    }

    public void GenerateChunks(Vector2Int[] columns)
    {
        try
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < columns.Length; i++)
            {
                _generating = true;

                if (!Columns.ContainsKey(columns[i]))
                {
                    GameObject obj = Instantiate(ColumnObj, VoxelConversions.ChunkCoordToWorld(new Vector3Int(columns[i].x, 0, columns[i].y)), Quaternion.identity, transform);
                    obj.name = "Column " + columns[i].ToString();
                    Columns[columns[i]] = obj.GetComponent<ChunkColumn>();
                    Columns[columns[i]].Init(columns[i], this);
                    gameClient.RequestColumn(columns[i], LOD_Mode.ReducedDepth, false);

                }

                Columns[columns[i]].Generate(height);
                chunksGenerated += height;
            }
            watch.Stop();
            Debug.Log("Finished generating chunks in: " + watch.Elapsed);

            if (!renderCompleteCalled)
            {
                renderCompleteCalled = true;
                //if (OnChunksGenerated != null)
                //    Loom.QueueOnMainThread(() => { OnChunksGenerated(columns); });
            }
            _generating = false;
        }
        catch (Exception e)
        {
            SafeDebug.LogError(string.Format("{0}: {1}\n {2}", e.GetType().ToString(), e.Message, e.StackTrace));
        }
    }


    public int[] textureArr;
    public Color[] colArr;
    public void CreateTextureComputeBuffer()
    {
        //textureArr = new int[] {  };
        textureArr = new int[blockTypes.Length * 6];
        colArr = new Color[blockTypes.Length];
        for (int i = 0; i < blockTypes.Length; i++)
        {
            colArr[i] = blockTypes[i].color;
            for (int j = 0; j < 6; j++)
            {
                textureArr[i * 6 + j] = blockTypes[i].textureIndex[j];
            }
        }

        TypeColorsComputeBuffer = new ComputeBuffer(colArr.Length, sizeof(float) * 4);
        TypeColorsComputeBuffer.SetData(colArr);

        //Debug.Log(textureArr);
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

    public void AddBlockType(BaseType _baseType, string _name, Color col, int[] _textures, GameObject _prefab)
    {
        byte index = (byte)blockTypes_dict.Count;
        blockTypes_dict.Add(index, new BlockType(_baseType, index, _name, col, _textures, _prefab));
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

    public Vector3Int[] GetChunkLocationsAroundPoint(int radius, Vector3Int center)
    {
        return new Vector3Int[] { center };
    }

    public Vector3Int[][] GetChunkLocationsAroundPoint(int threads, int radius, Vector3Int center)
    {
        Vector3Int[][] result = new Vector3Int[0][];
        try
        {
            List<List<Vector3Int>> chunksInSphere = new List<List<Vector3Int>>();
            if (radius > 0)
            {
                List<Vector3Int> counted = new List<Vector3Int>();
                for (int i = 0; i < threads; i++)
                    chunksInSphere.Add(new List<Vector3Int>());
                int threadIndex = 0;
                int circumference = (int)((2 * Mathf.PI * radius) + 1) / 2;
                for (int i = 0; i <= circumference; i++)
                {
                    float angle = VoxelConversions.Scale(i, 0, circumference, 0, 360);
                    int x = (int)(center.x + radius * Mathf.Cos(angle * (Mathf.PI / 180)));
                    int z = (int)(center.z + radius * Mathf.Sin(angle * (Mathf.PI / 180)));

                    for (int y = -SmoothVoxelSettings.maxChunksY / 2; y < SmoothVoxelSettings.maxChunksY / 2; y++)
                    {
                        Vector3Int[] positions = new Vector3Int[] {
                        new Vector3Int(x, y, z),
                        new Vector3Int(x + 1, y, z),
                        new Vector3Int(x - 1, y, z),
                        new Vector3Int(x, y, z + 1),
                        new Vector3Int(x, y, z - 1),
                        new Vector3Int(x - 1, y, z - 1),
                        new Vector3Int(x + 1, y, z - 1),
                        new Vector3Int(x + 1, y, z + 1),
                        new Vector3Int(x - 1, y, z + 1),
                    };
                        if (threadIndex >= threads)
                            threadIndex = 0;
                        for (int posIndex = 0; posIndex < positions.Length; posIndex++)
                        {
                            if (!BuilderExists(positions[posIndex].x, positions[posIndex].y, positions[posIndex].z) && !counted.Contains(positions[posIndex]))
                            {
                                chunksInSphere[threadIndex].Add(positions[posIndex]);
                                counted.Add(positions[posIndex]);
                            }
                        }
                        threadIndex++;
                    }
                }
            }
            else
            {
                chunksInSphere.Add(new List<Vector3Int>());
                chunksInSphere[0].Add(center);
            }

            result = new Vector3Int[chunksInSphere.Count][];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = chunksInSphere[i].ToArray();
            }
        }
        catch (Exception e)
        {
            SafeDebug.LogError(string.Format("{0}", e.Message));
        }
        return result;
    }
}
