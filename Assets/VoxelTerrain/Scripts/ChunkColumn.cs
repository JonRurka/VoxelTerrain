using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ChunkColumn : MonoBehaviour
{
    public int NumChunks;
    public Vector2Int Location;
    public SafeDictionary<int, SmoothChunk> Chunks;

    public bool NetworkMode { get; set; }

    public int f_sizeX = 10;
    public int f_sizeZ = 10;
    public int perMeterX = 4;
    public int perMeterZ = 4;

    public Color BaseColor;
    public Color ModColor1;

    public ComputeShader compoot_shader;

    public GameObject[] grasses;

    public ISampler Sampler;
    public IPageController Controller;

    public int[] grassDump;

    private int offset;
    

    Plant[] intermediate;
    VertexData[] vertexData;

    static float t = 0;
    int vertBuff_size;

    ComputeBuffer[] vertex_buffers;
    ComputeBuffer output_buff;
    ComputeBuffer height_buffer;
    ComputeBuffer plantMap_buffer;

    int k;
    int c_k;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //rend.material.SetFloat("__Time", MultiChunkController.T);
    }

    public void Init(Vector2Int location, IPageController controller)
    {
        Chunks = new SafeDictionary<int, SmoothChunk>();
        Location = location;
        Controller = controller;
        Sampler = new NetworkSampler();
        NetworkMode = true;
    }

    public void Init(Vector2Int location, IPageController controller, ISampler sampler)
    {
        Chunks = new SafeDictionary<int, SmoothChunk>();
        Location = location;
        Controller = controller;
        Sampler = sampler;

        

        vertBuff_size = f_sizeX * perMeterX * f_sizeZ * perMeterZ * 8 * 3;

        intermediate = new Plant[f_sizeX * perMeterX * f_sizeZ * perMeterZ];
        vertexData = new VertexData[vertBuff_size];

        vertex_buffers = new ComputeBuffer[] {
            new ComputeBuffer(vertBuff_size, VertexData.GetSize()),
            new ComputeBuffer(vertBuff_size, VertexData.GetSize()),
            new ComputeBuffer(vertBuff_size, VertexData.GetSize()),
            new ComputeBuffer(vertBuff_size, VertexData.GetSize()),
        };

        

        output_buff = new ComputeBuffer(intermediate.Length, Plant.GetSize());
        output_buff.SetData(intermediate);

        k = compoot_shader.FindKernel("CSMain");
        c_k = compoot_shader.FindKernel("CompileMesh");
        compoot_shader.SetInt("sizeX", SmoothVoxelSettings.ChunkSizeX);
        compoot_shader.SetInt("sizeZ", SmoothVoxelSettings.ChunkSizeZ);
        compoot_shader.SetInt("f_sizeX", f_sizeX);
        compoot_shader.SetInt("f_sizeZ", f_sizeZ);
        compoot_shader.SetInt("perMeterX", perMeterX);
        compoot_shader.SetInt("perMeterZ", perMeterZ);

        compoot_shader.SetVector("baseColor", BaseColor);
        compoot_shader.SetVector("modColor1", ModColor1);

        compoot_shader.SetBuffer(k, "intermediate", output_buff);

        compoot_shader.SetBuffer(c_k, "intermediate", output_buff);
    }

    public void SetHeightmap(float[] heightmap)
    {
        if (NetworkMode)
        {
            ((NetworkSampler)Sampler).SetSurfaceData(heightmap);
        }
    }

    public void Generate(int height)
    {
        int chunkSizeX = SmoothVoxelSettings.ChunkSizeX;
        int chunkSizeY = SmoothVoxelSettings.ChunkSizeY;
        int chunkSizeZ = SmoothVoxelSettings.ChunkSizeZ;

        int meterSizeX = SmoothVoxelSettings.MeterSizeX;
        int meterSizeY = SmoothVoxelSettings.MeterSizeY;
        int meterSizeZ = SmoothVoxelSettings.MeterSizeZ;

        Sampler.SetChunkSettings(SmoothVoxelSettings.voxelsPerMeter,
                               new Vector3Int(chunkSizeX, chunkSizeY, chunkSizeZ),
                               new Vector3Int(meterSizeX, meterSizeY, meterSizeZ),
                               Mathf.RoundToInt(1 / (float)SmoothVoxelSettings.voxelsPerMeter),
                               ((1.0f / (float)SmoothVoxelSettings.voxelsPerMeter) / 2.0f),
                               new Vector3(meterSizeX / (float)chunkSizeX, meterSizeY / (float)chunkSizeY, meterSizeZ / (float)chunkSizeZ));

        Vector3Int topVoxel = VoxelConversions.WorldToVoxel(new Vector3(0, (float)Sampler.GetMax(), 0));
        Vector3Int bottomVoxel = VoxelConversions.WorldToVoxel(new Vector3(0, (float)Sampler.GetMin(), 0));

        int topChunk = VoxelConversions.VoxelToChunk(topVoxel).y;
        int bottomChunk = VoxelConversions.VoxelToChunk(bottomVoxel).y;

        if (NetworkMode)
        {

            for (int y = 0; y <= topChunk; y++)
            {
                SmoothChunk.CreateChunk(new Vector3Int(Location.x, y, Location.y), Sampler, Controller);
            }


        }
        else
        {
            Vector2Int bottomLeft = new Vector2(Location.x * chunkSizeX, Location.y * chunkSizeZ);
            Vector2Int topRight = new Vector2(Location.x * chunkSizeX + chunkSizeX, Location.y * chunkSizeZ + chunkSizeZ);
            Sampler.SetSurfaceData(bottomLeft, topRight);

            for (int y = 0; y <= topChunk; y++)
            {
                SmoothChunk.CreateChunk(new Vector3Int(Location.x, y, Location.y), Sampler, Controller);
            }
            Loom.QueueOnMainThread(() =>
            {
                Debug.Log("Spawning grass...");
                SpawnGrass();
            });
        }
    }

    public void AddChunk(int pos, IChunk chunk)
    {
        Chunks[pos] = (SmoothChunk)chunk;
        Chunks[pos].BuildGPU_DataBuffer(true);
    }

    public bool ChunkExists(int y)
    {
        return Chunks.ContainsKey(y);
    }

    public SmoothChunk GetChunk(int y)
    {
        if (ChunkExists(y))
        {
            return Chunks[y];
        }
        return null;
    }

    public void SpawnGrass()
    {
        if (SmoothVoxelSettings.enableGrass)
        {
            height_buffer = new ComputeBuffer(Sampler.GetSurfaceData().Length, sizeof(float));
            height_buffer.SetData(Sampler.GetSurfaceData());

            plantMap_buffer = new ComputeBuffer(((TerrainSampler)Sampler).GetPlantMap().Length, sizeof(int));
            plantMap_buffer.SetData(((TerrainSampler)Sampler).GetPlantMap());

            grassDump = ((TerrainSampler)Sampler).GetPlantMap();

            SpawnGrass(0, 0, vertex_buffers[0], grasses[0]);
            SpawnGrass(0, 1, vertex_buffers[1], grasses[1]);
            SpawnGrass(1, 0, vertex_buffers[2], grasses[2]);
            SpawnGrass(1, 1, vertex_buffers[3], grasses[3]);
        }
    }

    public void SpawnGrass(int sec_x, int sec_z, ComputeBuffer v_buff, GameObject grass_obj)
    {
        int voxelOffset_x = sec_x * 10;
        int voxelOffset_z = sec_z * 10;

        output_buff.SetData(intermediate);

        //GraphicsBuffer g_buff = new GraphicsBuffer(GraphicsBuffer.Target.Index, PlantPolyCache.indices_4x4.Length, sizeof(int));
        //g_buff.SetData(PlantPolyCache.indices_4x4);

        v_buff.SetData(vertexData);

        float min = (float)Sampler.GetMin();
        float max = (float)Sampler.GetMax();

        float diff = (float)(max - min);
        float mid = (float)(max + min) / 2f;

        compoot_shader.SetFloat("average_height", mid);
        compoot_shader.SetInt("voxel_offset_x", voxelOffset_x);
        compoot_shader.SetInt("voxel_offset_z", voxelOffset_z);
        compoot_shader.SetBuffer(k, "heightmap", height_buffer);
        compoot_shader.SetBuffer(k, "pantMap", plantMap_buffer);
        

        //shader.SetBuffer(c_k, "indices", g_buff);
        compoot_shader.SetBuffer(c_k, "vertex", v_buff);

        compoot_shader.Dispatch(k, f_sizeX * 4, 1, f_sizeZ * 4);
        compoot_shader.Dispatch(c_k, 1, 1, 1);

        

        float meter_size_x = SmoothVoxelSettings.MeterSizeX / 2;
        float meter_size_z = SmoothVoxelSettings.MeterSizeZ / 2;


        Vector3 offset = new Vector3(voxelOffset_x + meter_size_x / 2, mid, voxelOffset_z + meter_size_z / 2);
        Vector3 size = new Vector3(meter_size_x, diff + diff / 2, meter_size_z);

        //Debug.DrawRay(new Vector3(offset.x, max, offset.z), Vector3.up, Color.red, 50000);
        //Debug.DrawRay(new Vector3(offset.x, min, offset.z), Vector3.down, Color.blue, 50000);

        grass_obj.transform.localPosition = offset;

        MeshRenderer rend = grass_obj.GetComponent<MeshRenderer>();
        MeshFilter filter = grass_obj.GetComponent<MeshFilter>();

        rend.material.SetPass(0);
        rend.material.SetBuffer("_Vertex", v_buff);

        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[vertBuff_size];//verts.ToArray();
        mesh.triangles = PlantPolyCache.indices_4x4;
        //mesh.uv = uv.ToArray();
        

        

        


        Vector3 corner = VoxelConversions.ChunkCoordToWorld(new Vector3Int(Location.x, 0, Location.y));
        Bounds b = new Bounds(Vector3.zero, size);

        mesh.bounds = b;

        

        filter.sharedMesh = mesh;

    }
}
