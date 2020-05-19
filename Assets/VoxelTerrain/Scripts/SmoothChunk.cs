using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using LibNoise;

using UnityEngine.Rendering;
using Unity.Collections;

public class SmoothChunk : MonoBehaviour, IChunk
{
    [Serializable]
    public struct BlockChange
    {
        public Vector3Int position;
        public byte type;
        public BlockChange(Vector3Int position, byte type)
        {
            this.position = position;
            this.type = type;
        }
    }

    public Vector3Int chunkPosition;
    public Vector3 globalPosition;
    public IVoxelBuilder builder;
    public SmoothVoxelBuilder builderInstance;
    public IPageController pageController;
    public List<BlockChange> editQueue;
    public GameObject grassPrefab;
    public GameObject[] treePrefabs;
    public int disappearDistance = SmoothVoxelSettings.radius;
    public int maxGrassDistance = 3;
    public int grassPerMeter = 1;
    public int size = 0;
    public int vertSize = 0;
    public int triSize = 0;
    public int LODlevel = 0;
    public List<Vector3Int> surface;
    public List<Vector2Int> surface2D;
    public bool canUpdateForLOD = false;


    public bool Generated {
        get { return _generated; }
    }

    MeshFilter _filter;
    MeshRenderer _renderer;
    MeshCollider _collider;
    GameObject _player;
    IModule surfaceModule;
    public int voxelsPerMeter = 1;
    object _lockObj;

    Texture3D tex;
    public Color32[] type_data;

    public float[] Mat_index_array;


    ManualResetEvent _resetEvent = new ManualResetEvent(false);

    bool _enableTest = false;
    bool _generated = false;
    bool _rendered = false;
    bool _grassEnabled = false;
    bool _destroyed = false;
    bool _treesPlaced = false;
    int oldLODlevel = -1;

    List<GameObject> _grassList = new List<GameObject>();


    uint[] buffer;
    ComputeBuffer data_buffer;
    ComputeBuffer out_buff;

    // Update is called once per frame
    public void ChunkUpdate () {
        return;
        if (_rendered && !_destroyed)
        {
            /*if (!grassEnabled && Vector3.Distance(TerrainController.Instance.newPlayerChunkPos, ChunkPosition) < maxGrassDistance)
            {
                grassEnabled = true;
                SpawnGrass();
            }
            if (grassEnabled && Vector3.Distance(TerrainController.Instance.newPlayerChunkPos, ChunkPosition) > maxGrassDistance)
            {
                grassEnabled = false;
                for (int i = 0; i < grassList.Count; i++)
                    Destroy(grassList[i]);
                grassList.Clear();
            }*/


            if (!_treesPlaced) {
                System.Random rand = new System.Random();
                _treesPlaced = true;
            }

            float distance = Vector3.Distance(globalPosition, TerrainController.Instance.LODtarget);
            if (distance > (SmoothVoxelSettings.radius * SmoothVoxelSettings.MeterSizeX)) {
                _destroyed = true;
                TerrainController.Instance.DestroyChunk(chunkPosition);
                Debug.Log("Destoying chunk");
                return;
            }

            UpdateLOD();

            if (editQueue.Count > 0)
            {
                List<BlockChange> EditQueueCopy = new List<BlockChange>(editQueue);
                editQueue.Clear();
                Loom.QueueAsyncTask(TerrainController.setBlockThreadName, () =>
                {

                    lock (_lockObj)
                    {
                        List<Vector3Int> updateChunks = new List<Vector3Int>();
                        foreach (BlockChange change in EditQueueCopy)
                        {
                            Vector3Int position = change.position;
                            byte type = change.type;
                            if (position.x == 0)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x - 1, chunkPosition.y, chunkPosition.z)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x - 1, chunkPosition.y, chunkPosition.z));
                            }
                            if (position.x == SmoothVoxelSettings.ChunkSizeX - 1)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x + 1, chunkPosition.y, chunkPosition.z)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x + 1, chunkPosition.y, chunkPosition.z));
                            }

                            if (position.y == 0)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x, chunkPosition.y - 1, chunkPosition.z)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y - 1, chunkPosition.z));

                            }
                            if (position.y == SmoothVoxelSettings.ChunkSizeY - 1)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x, chunkPosition.y + 1, chunkPosition.z)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y + 1, chunkPosition.z));
                            }

                            if (position.z == 0)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z - 1)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z - 1));
                            }
                            if (position.z == SmoothVoxelSettings.ChunkSizeZ - 1)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z + 1)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z + 1));
                            }
                            builder.SetBlock(position.x, position.y, position.z, new Block(type));
                        }
                        Render(true);
                        foreach (Vector3Int chunk in updateChunks)
                        {
                            pageController.UpdateChunk(chunk.x, chunk.y, chunk.z);
                        }
                    }
                });
            }
        }
	}

    public void UpdateLOD() {
        return;
        Loom.QueueAsyncTask("LODupdate", () => {
            if (canUpdateForLOD) {
                Vector3 worldPos = VoxelConversions.ChunkCoordToWorld(chunkPosition);
                float distance = Vector3.Distance(worldPos, TerrainController.Instance.LODtarget);
                float radius = SmoothVoxelSettings.radius * SmoothVoxelSettings.MeterSizeX;
                double voxelsPerMeter = SmoothVoxelSettings.voxelsPerMeter;
                int LODlevel = 1;
                if (distance > (radius / 10f) && distance < (radius / 10f) * 5) {
                    voxelsPerMeter /= 2;
                    LODlevel = 2;
                }
                else if (distance > (radius / 10f) * 5 && distance < radius) {
                    voxelsPerMeter /= 4;
                    LODlevel = 3;
                }
                /*else if (distance > (radius / 10f) * 6 && distance < radius) {
                    voxelsPerMeter /= 10;
                    LODlevel = 4;
                }*/
                else if (distance >= radius) {
                    return;
                }

                if (LODlevel != oldLODlevel) {
                    oldLODlevel = LODlevel;
                    builderInstance.CalculateVariables(voxelsPerMeter, SmoothVoxelSettings.MeterSizeX, SmoothVoxelSettings.MeterSizeY, SmoothVoxelSettings.MeterSizeZ);
                    builderInstance.Generate(surfaceModule,
                        SmoothVoxelSettings.seed,
                        SmoothVoxelSettings.enableCaves && (LODlevel <= 2),
                        SmoothVoxelSettings.amplitude,
                        SmoothVoxelSettings.caveDensity,
                        SmoothVoxelSettings.grassOffset);
                    Render(false);
                }
            }
        });
    }

    public void SpawnGrass()
    {
        surface = new List<Vector3Int>(builderInstance.GetSurfacePoints());
        for (int i = 0; i < surface.Count; i++)
        {
            //GameObject grassObj = (GameObject)Instantiate(TerrainController.Instance.grassPrefab, new Vector3(surface[i].x, 0, surface[i].z),
            //    Quaternion.identity);
            //grassObj.transform.parent = transform;
        }


    //TerrainController.Instance.SetSurfacePoints(surface.ToArray());
        //Vector3Int[] surfacePoints = builderInstance.GetSurfacePoints();
        //System.Random rand = new System.Random(VoxelSettings.seed);
        /*//int maxGrass = Mathf.RoundToInt((float)grassPerMeter / (float)VoxelSettings.voxelsPerMeter);
        for (int i = 0; i < surfacePoints.Length; i++)
        {

            for (int j = 0; j < maxGrass; j++)
            {
                //Vector3 pos = new Vector3((float)rand.Next(-100, 100) / 10f / VoxelSettings.voxelsPerMeter, 0, (float)rand.Next(-100, 100) / 10f / VoxelSettings.voxelsPerMeter);
                //GameObject grassObj = (GameObject)Instantiate(grassPrefab, pos, Quaternion.identity);
                //grassObj.transform.parent = transform;
                //grassList.Add(grassObj);
            }
        }*/
        //Debug.LogFormat("surface points: {0}", surfacePoints.Length);
        //Debug.LogFormat("Grass per point: {0}", maxGrass);
        //Debug.LogFormat("Spawned {0} grass.", _grassList.Count);
    }

    public void EditNextFrame(BlockChange[] changes)
    {
        editQueue.AddRange(changes);
    }

    public void EditNextFrame(BlockChange change)
    {
        Vector3Int position = change.position;
        byte type = change.type;
        if (position.x >= 0 && position.x < SmoothVoxelSettings.ChunkSizeX && position.y >= 0 && position.y < SmoothVoxelSettings.ChunkSizeY && position.z >= 0 && position.z < SmoothVoxelSettings.ChunkSizeZ)
        {
            editQueue.Add(new BlockChange(position, type));
        }
        else
        {
            SafeDebug.LogError(string.Format("Out of Bounds: chunk: {0}, localVoxel: {1}, Function: EditNextFrame", chunkPosition, position));
        }
    }

    public void Init(Vector3Int chunkPos, Vector3 worldPos, IModule module, IPageController controller, int lodLevel, SmoothVoxelBuilder smoothBuilder) {
        editQueue = new List<BlockChange>();
        _lockObj = new object();
        surface = new List<Vector3Int>();
        chunkPosition = chunkPos;
        surfaceModule = module;
        LODlevel = lodLevel;
        oldLODlevel = lodLevel;
        pageController = controller;
        transform.position = worldPos;
        transform.parent = controller.getGameObject().transform;
        globalPosition = transform.position;
        _renderer = gameObject.GetComponent<MeshRenderer>();
        _filter = gameObject.GetComponent<MeshFilter>();
        _collider = gameObject.GetComponent<MeshCollider>();
        //_renderer.material.SetTexture("_MainTex", TerrainController.Instance.textureAtlas);
        //_player = TerrainController.Instance.player;
        builderInstance = smoothBuilder;
        builder = smoothBuilder;
    }

    public void DebugFill(byte type)
    {
        for(int x = 0; x < SmoothVoxelSettings.ChunkSizeX; x++)
            for(int y = 0; y < SmoothVoxelSettings.ChunkSizeY; y++)
                for(int z = 0; z < SmoothVoxelSettings.ChunkSizeZ; z++)
                {
                    if (y == 0)
                        builder.SetBlock(x, y, z, new Block(type));
                }
    }

    public void DebugColor(Color color)
    {
        _renderer.material.SetColor("_color", color);
    }

    public Block GetBlock(int x, int y, int z)
    {
        Block result = default(Block);
        if (builder != null)
        {
            result = builder.GetBlock(x, y, z);
        }

        return result;
    }

    public void BuildGPU_DataBuffer(bool full_generate)
    {
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        Vector3Int start = VoxelConversions.LocalToGlobalCoord(chunkPosition, new Vector3Int(0, 0, 0));
        start = new Vector3Int(start.x - 1, start.y - 1, start.z - 1);

        //Debug.Log(chunkPosition + ": " + start.ToString());

        if (full_generate)
        {
            buffer = new uint[(builderInstance.ChunkSizeX + 2) * (builderInstance.ChunkSizeY + 2) * (builderInstance.ChunkSizeZ + 2)];
            for (int g_x = start.x, x = 0; x < builderInstance.ChunkSizeX + 2; x++, g_x++)
            {
                for (int g_y = start.y, y = 0; y < builderInstance.ChunkSizeY + 2; y++, g_y++)
                {
                    for (int g_z = start.z, z = 0; z < builderInstance.ChunkSizeZ + 2; z++, g_z++)
                    {
                        Block block = pageController.GetBlock(g_x, g_y, g_z);

                        if (!block.set)
                        {
                            if (x == 0)
                            {
                                block = pageController.GetBlock(g_x + 1, g_y, g_z);
                            }
                            else if (x == builderInstance.ChunkSizeX + 1)
                            {
                                block = pageController.GetBlock(g_x - 1, g_y, g_z);
                            }

                            if (y == 0)
                            {
                                block = pageController.GetBlock(g_x, g_y + 1, g_z);
                            }
                            else if (y == builderInstance.ChunkSizeY + 1)
                            {
                                block = pageController.GetBlock(g_x, g_y - 1, g_z);
                            }

                            if (z == 0)
                            {
                                block = pageController.GetBlock(g_x, g_y, g_z + 1);
                            }
                            else if (z == builderInstance.ChunkSizeZ + 1)
                            {
                                block = pageController.GetBlock(g_x, g_y, g_z - 1);
                            }
                        }

                        buffer[x + (builderInstance.ChunkSizeY + 2) * (y + (builderInstance.ChunkSizeZ + 2) * z)] = block.type;
                    }
                }
            }
        }

        watch.Stop();

        //Debug.Log("BuildGPU_DataBuffer set buffer: " + watch.Elapsed);

        watch.Restart();

        Loom.QueueOnMainThread(() =>
        {
            if (data_buffer == null)
                data_buffer = new ComputeBuffer(buffer.Length, sizeof(uint), ComputeBufferType.Default);
            data_buffer.SetData(buffer);
            _renderer.material.SetBuffer("_Data", data_buffer);
        });

        watch.Stop();

        //Debug.Log("BuildGPU_DataBuffer set material: " + watch.Elapsed);

    }

    void OnApplicationQuit() {
        Close();
    }

    void OnRenderObject()
    {
        /*_renderer.material.SetPass(0);

        if (SingleChunkController.Instance.TextureComputeBuffer != null)
            _renderer.material.SetBuffer("_textureMap", SingleChunkController.Instance.TextureComputeBuffer);

        if (data_buffer != null)
            _renderer.material.SetBuffer("_Data", data_buffer);*/
    }

    public void Close()
    {
        _destroyed = true;
        if (builder != null) {
            builder.Dispose();
            builder = null;
            builderInstance = null;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct ExampleVertex
    {
        public Vector3 pos;
        public ushort normalX, normalY;
        public Color32 tangent;
    }

    public void Render(bool renderOnly) {
        if (builder != null && !_destroyed) {
            Render(builder.Render(renderOnly));
        }
    }

    public void Render (MeshData meshData, bool firstRun = false) {
        if (!_destroyed) {

            Vector3[] smoothNormals = builderInstance.GetSmoothNormals(meshData.normals);
            Loom.QueueOnMainThread(() => {
                if (_filter != null && _collider != null && _renderer != null && !_destroyed) {
                    _rendered = true;
                    Mesh mesh = new Mesh();
                    canUpdateForLOD = meshData.vertices.Length > 0;
                    /*if (!canUpdateForLOD && LODlevel >= 2) {
                        _destroyed = true;
                        //MultiChunkController.Instance.DestroyChunk(chunkPosition);
                        return;
                    }*/
                    mesh.vertices = meshData.vertices;
                    mesh.triangles = meshData.triangles;
                    mesh.normals = smoothNormals;

                    //IntPtr ptr = mesh.GetNativeVertexBufferPtr();

                    //GraphicsBuffer buff = new GraphicsBuffer(GraphicsBuffer.Target.Vertex, 0, 0);
                    //ComputeShader shader = null;

                    
                    
                   //Graphics.DrawProcedural()

                    //mesh.SetIndexBufferData



                    //mesh.colors = meshData.Colors;
                    //mesh.uv = meshData.UVs;
                    if (firstRun)
                    {
                        
                        //FoliageTest test = gameObject.GetComponentInChildren<FoliageTest>();
                        //test.TestComputeShader(transform.position, builderInstance.Sampler.GetSurfaceData());

                        //if (out_buff == null)
                        //    out_buff = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);

                        //_renderer.material.SetTexture("_MainTex", SingleChunkController.Instance.LinearTextureBlending);

                        _renderer.material.SetBuffer("_textureMap", pageController.TextureComputeBuffer);
                        _renderer.material.SetTexture("_Textures", pageController.TextureArray);
                        _renderer.material.SetVector("_chunk", new Vector4(chunkPosition.x, chunkPosition.y, chunkPosition.z));
                        _renderer.material.SetVector("_Dimensions", new Vector4(builderInstance.ChunkSizeX, builderInstance.ChunkSizeY, builderInstance.ChunkSizeZ));
                        _renderer.material.SetFloat("_voxelsPerMeter", (float)builderInstance.VoxelsPerMeter);
                        _renderer.material.SetFloat("_NumTextures", pageController.NumSourceTextures);
                    }

                    

                    //mesh.RecalculateNormals();

                    _filter.sharedMesh = mesh;
                    if (LODlevel <= 2)
                        _collider.sharedMesh = mesh;
                    //_renderer.material.SetTexture("_MainTex", TerrainController.Instance.textureAtlas);
                    

                    size = meshData.GetSize();
                    vertSize = meshData.vertices.Length;
                    triSize = meshData.triangles.Length;
                    meshData.vertices = null;
                    meshData.triangles = null;
                    meshData.UVs = null;
                }
            });
        }
    }

    public float[] GetBlockTypeTexture()
    {
        int data_size = (builderInstance.ChunkSizeX + 2) * (builderInstance.ChunkSizeY + 2) * (builderInstance.ChunkSizeZ + 2);

        tex = new Texture3D(builderInstance.ChunkSizeX + 2, builderInstance.ChunkSizeY + 2, builderInstance.ChunkSizeZ + 2, TextureFormat.RGBA32, false);

        Vector3Int orig = VoxelConversions.ChunkToVoxel(chunkPosition);

        Debug.Log(orig);

        //type_data = new Color32[data_size];
        Mat_index_array = new float[data_size];
        for (int x_g = orig.x - 1, x = 0; x < builderInstance.ChunkSizeX + 2; x_g++, x++)
        {
            for (int y_g = orig.y - 1, y = 0; y < builderInstance.ChunkSizeY + 2; y_g++, y++)
            {
                for (int z_g = orig.z - 1, z = 0; z < builderInstance.ChunkSizeZ + 2; z_g++, z++)
                {
                    uint type = SingleChunkController.Instance.GetBlock(x_g, y_g, z_g).type;
                    if (type == 0)
                    {
                        //type_data[builderInstance.Get_Flat_Index(x, y, z)] = new Color32(0, 255, 0, 0);
                        Mat_index_array[builderInstance.Get_Flat_Index(x, y, z)] = -1f;
                    }
                    else
                    {
                        int index = SingleChunkController.Instance.BlockTypes[type].textureIndex[0];
                        Mat_index_array[builderInstance.Get_Flat_Index(x, y, z)] = index;
                        //type_data[builderInstance.Get_Flat_Index(x, y, z)] = new Color32((byte)index, 0, 0, 0);
                    }
                }
            }
        }

        //Debug.Log("pos: " + builderInstance.Get_Flat_Index(8, 8, 10));

        //tex.SetPixels32(type_data, 0);
        //tex.Apply();

        return Mat_index_array;
    }

    public static void CreateChunk(Vector3Int chunkPos, ISampler sampler, IPageController controller)
    {
        Vector3 worldPos = VoxelConversions.ChunkCoordToWorld(chunkPos);
        double voxelsPerMeter = SmoothVoxelSettings.voxelsPerMeter;

        SmoothVoxelBuilder builder = new SmoothVoxelBuilder(controller, chunkPos);
        builder.SetBlockTypes(controller.BlockTypes, null);
        builder.CalculateVariables(voxelsPerMeter, SmoothVoxelSettings.MeterSizeX, SmoothVoxelSettings.MeterSizeY, SmoothVoxelSettings.MeterSizeZ);
        builder.Generate(sampler);


        MeshData meshData = builder.Render(false);
        //if (meshData.vertices.Length == 0)
        //    return;

        Loom.QueueOnMainThread(() => {
            SmoothChunk chunk = GameObject.Instantiate(controller.ChunkPrefab).GetComponent<SmoothChunk>();
            chunk.name = string.Format("Chunk_{0}.{1}.{2}", chunkPos.x, chunkPos.y, chunkPos.z);
            chunk.Init(chunkPos, worldPos, null, controller, 1, builder);
            chunk.Render(meshData, true);
            Loom.QueueAsyncTask("gen2", () => controller.AddChunk(chunkPos, chunk));

            /*if (!TerrainController.Instance.Chunks.ContainsKey(chunkPos))
            {
                SmoothChunk chunk = GameObject.Instantiate(TerrainController.Instance.chunkPrefab).GetComponent<SmoothChunk>();
                chunk.name = string.Format("Chunk_{0}.{1}.{2}", chunkPos.x, chunkPos.y, chunkPos.z);
                chunk.Init(chunkPos, worldPos, null, controller, 1, builder);
                chunk.Render(meshData);
                TerrainController.Instance.AddChunk(chunkPos, chunk);
            }*/
        });
    }

}
