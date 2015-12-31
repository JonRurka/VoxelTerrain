using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using LibNoise;

public class Chunk : MonoBehaviour {
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
    public int disappearDistance = VoxelSettings.radius;
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
    

    ManualResetEvent _resetEvent = new ManualResetEvent(false);

    bool _enableTest = false;
    bool _generated = false;
    bool _rendered = false;
    bool _grassEnabled = false;
    bool _destroyed = false;
    bool _treesPlaced = false;
    int oldLODlevel = -1;

    List<GameObject> _grassList = new List<GameObject>();
	
	// Update is called once per frame
	public void ChunkUpdate () {

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
            if (distance > (VoxelSettings.radius * VoxelSettings.MeterSizeX)) {
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
                            if (position.x == VoxelSettings.ChunkSizeX - 1)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x + 1, chunkPosition.y, chunkPosition.z)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x + 1, chunkPosition.y, chunkPosition.z));
                            }

                            if (position.y == 0)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x, chunkPosition.y - 1, chunkPosition.z)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y - 1, chunkPosition.z));

                            }
                            if (position.y == VoxelSettings.ChunkSizeY - 1)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x, chunkPosition.y + 1, chunkPosition.z)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y + 1, chunkPosition.z));
                            }

                            if (position.z == 0)
                            {
                                if (!updateChunks.Contains(new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z - 1)))
                                    updateChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z - 1));
                            }
                            if (position.z == VoxelSettings.ChunkSizeZ - 1)
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
        Loom.QueueAsyncTask("LODupdate", () => {
            if (canUpdateForLOD) {
                Vector3 worldPos = VoxelConversions.ChunkCoordToWorld(chunkPosition);
                float distance = Vector3.Distance(worldPos, TerrainController.Instance.LODtarget);
                float radius = VoxelSettings.radius * VoxelSettings.MeterSizeX;
                double voxelsPerMeter = VoxelSettings.voxelsPerMeter;
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
                    builderInstance.CalculateVariables(voxelsPerMeter, VoxelSettings.MeterSizeX, VoxelSettings.MeterSizeY, VoxelSettings.MeterSizeZ);
                    builderInstance.Generate(surfaceModule,
                        VoxelSettings.seed,
                        VoxelSettings.enableCaves && (LODlevel <= 2),
                        VoxelSettings.amplitude,
                        VoxelSettings.caveDensity,
                        VoxelSettings.grassOffset);
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
        if (position.x >= 0 && position.x < VoxelSettings.ChunkSizeX && position.y >= 0 && position.y < VoxelSettings.ChunkSizeY && position.z >= 0 && position.z < VoxelSettings.ChunkSizeZ)
        {
            editQueue.Add(new BlockChange(position, type));
        }
        else
        {
            SafeDebug.LogError(string.Format("Out of Bounds: chunk: {0}, localVoxel: {1}, Function: EditNextFrame", chunkPosition, position));
        }
    }

    public void Init(Vector3Int chunkPos, IModule module, IPageController pageController, double voxelsPerMeter = VoxelSettings.voxelsPerMeter) {
        editQueue = new List<BlockChange>();
        _lockObj = new object();
        surface = new List<Vector3Int>();
        chunkPosition = chunkPos;
        surfaceModule = module;
        this.pageController = pageController;
        transform.position = VoxelConversions.ChunkCoordToWorld(chunkPos);
        transform.parent = TerrainController.Instance.transform;
        globalPosition = transform.position;
        _renderer = gameObject.GetComponent<MeshRenderer>();
        _filter = gameObject.GetComponent<MeshFilter>();
        _collider = gameObject.GetComponent<MeshCollider>();
        _renderer.material.SetTexture("_MainTex", TerrainController.Instance.textureAtlas);
        _player = TerrainController.Instance.player;
        createChunkBuilder(voxelsPerMeter);
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
        transform.parent = TerrainController.Instance.transform;
        globalPosition = transform.position;
        _renderer = gameObject.GetComponent<MeshRenderer>();
        _filter = gameObject.GetComponent<MeshFilter>();
        _collider = gameObject.GetComponent<MeshCollider>();
        _renderer.material.SetTexture("_MainTex", TerrainController.Instance.textureAtlas);
        _player = TerrainController.Instance.player;
        builderInstance = smoothBuilder;
        builder = smoothBuilder;
    }

    public void createChunkBuilder(double voxelsPerMeter = VoxelSettings.voxelsPerMeter) {
        float distance = Vector3.Distance(transform.position, TerrainController.Instance.LODtarget);
        float radius = VoxelSettings.radius * VoxelSettings.MeterSizeX;
        LODlevel = 1;
        if (distance > (radius / 10f) && distance < (radius / 10f) * 5) {
            voxelsPerMeter /= 2;
            LODlevel = 2;
        }
        else if (distance > (radius / 10f) * 5 && distance < radius) {
            voxelsPerMeter /= 4;
            LODlevel = 3;
        }
        else if (distance >= radius) {
            _destroyed = true;
            TerrainController.Instance.DestroyChunk(chunkPosition);
            return;
        }

        builder = new SmoothVoxelBuilder(TerrainController.Instance, chunkPosition);
        builder.SetBlockTypes(TerrainController.Instance.BlocksArray, TerrainController.Instance.AtlasUvs);
        builderInstance = (SmoothVoxelBuilder)builder;
        builderInstance.CalculateVariables(voxelsPerMeter, VoxelSettings.MeterSizeX, VoxelSettings.MeterSizeY, VoxelSettings.MeterSizeZ);

    }

    public void DebugFill(byte type)
    {
        for(int x = 0; x < VoxelSettings.ChunkSizeX; x++)
            for(int y = 0; y < VoxelSettings.ChunkSizeY; y++)
                for(int z = 0; z < VoxelSettings.ChunkSizeZ; z++)
                {
                    if (y == 0)
                        builder.SetBlock(x, y, z, new Block(type));
                }
    }

    public void DebugColor(Color color)
    {
        _renderer.material.SetColor("_color", color);
    }

    public void Generate() {
        /*if (voxelsPerMeter != oldVoxelsPerMeter) {
            oldVoxelsPerMeter = voxelsPerMeter;
            BuilderGenerate();
            Render(false);
        }*/
        if (!_destroyed) {
            BuilderGenerate();
            Render(false);
        }
    }

    public void BuilderGenerate()
    {
        ((SmoothVoxelBuilder)builder).Generate(surfaceModule,
                                               VoxelSettings.seed,
                                               VoxelSettings.enableCaves && (LODlevel <= 2),
                                               VoxelSettings.amplitude,
                                               VoxelSettings.caveDensity,
                                               VoxelSettings.grassOffset);
        _generated = true;
    }

    /*public float[,] GenerateChunk() {
        float[,] result = null;
        result = builder.Generate( VoxelSettings.seed,
                                 VoxelSettings.enableCaves,
                                 VoxelSettings.amplitude,
                                 VoxelSettings.caveDensity,
                                 VoxelSettings.groundOffset,
                                 VoxelSettings.grassOffset );
        _generated = true;
        return result;
    }*/

    public Block GetBlock(int x, int y, int z)
    {
        Block result = default(Block);
        if (builder != null)
        {
            result = builder.GetBlock(x, y, z);
        }

        return result;
    }

    void OnApplicationQuit() {
        Close();
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

    public void Render(bool renderOnly) {
        if (builder != null && !_destroyed) {
            Render(builder.Render(renderOnly));
        }
    }

    public void Render (MeshData meshData) {
        if (!_destroyed) {
            Loom.QueueOnMainThread(() => {
                if (_filter != null && _collider != null && _renderer != null && !_destroyed) {
                    _rendered = true;
                    Mesh mesh = new Mesh();
                    canUpdateForLOD = meshData.vertices.Length > 0;
                    if (!canUpdateForLOD && LODlevel >= 2) {
                        _destroyed = true;
                        TerrainController.Instance.DestroyChunk(chunkPosition);
                        return;
                    }
                    mesh.vertices = meshData.vertices;
                    mesh.triangles = meshData.triangles;
                    //mesh.uv = meshData.UVs;

                    mesh.RecalculateNormals();

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

    public static void CreateChunk(Vector3Int chunkPos, string thread, int threadIndex, IModule module, TerrainController controller) {
        Vector3 worldPos = VoxelConversions.ChunkCoordToWorld(chunkPos);
        float distance = Vector3.Distance(worldPos, TerrainController.Instance.LODtarget);
        float radius = VoxelSettings.radius * VoxelSettings.MeterSizeX;
        double voxelsPerMeter = VoxelSettings.voxelsPerMeter;
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
        SmoothVoxelBuilder builder = new SmoothVoxelBuilder(TerrainController.Instance, chunkPos);
        builder.SetBlockTypes(TerrainController.Instance.BlocksArray, TerrainController.Instance.AtlasUvs);
        builder.CalculateVariables(voxelsPerMeter, VoxelSettings.MeterSizeX, VoxelSettings.MeterSizeY, VoxelSettings.MeterSizeZ);
        builder.Generate(module,
                        VoxelSettings.seed,
                        VoxelSettings.enableCaves && (LODlevel <= 2),
                        VoxelSettings.amplitude,
                        VoxelSettings.caveDensity,
                        VoxelSettings.grassOffset);
        MeshData meshData = builder.Render(false);
        if (meshData.vertices.Length == 0)
            return;

        Loom.QueueOnMainThread(() => {
            if (!TerrainController.Instance.Chunks.ContainsKey(chunkPos)) {
                Chunk chunk = GameObject.Instantiate(TerrainController.Instance.chunkPrefab).GetComponent<Chunk>();
                chunk.name = string.Format("Chunk_{0}.{1}.{2}", chunkPos.x, chunkPos.y, chunkPos.z);
                chunk.Init(chunkPos, worldPos, module, controller, LODlevel, builder);
                chunk.Render(meshData);
                TerrainController.Instance.AddChunk(chunkPos, chunk);
            }
        });

    }
}
