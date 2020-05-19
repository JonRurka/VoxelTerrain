using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FoliageTest : MonoBehaviour
{
    public ComputeShader shader;

    public float Ambience = 1;

    public GameObject obj;
    public Plant[] intermediate;
    public float[] Heightmap;

    public VertexData[] vertexData;
    public int[] indices;
    public List<Vector3> verts = new List<Vector3>();
    public List<Vector2> uv = new List<Vector2>();
    public List<Vector3> normals = new List<Vector3>();

    private int[] p = new int[512];
    private int[] permutation = new int[512];
    private Texture2D perm_tex;
    private bool inited = false;
    private MeshRenderer rend;
    private float t = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RenderSettings.ambientIntensity = Ambience;

        if (inited)
        {
            t += Time.deltaTime;
            rend.material.SetFloat("__Time", t);
        }
    }

    void Init_perm(int s)
    {
        print("-> init2");
        Random.InitState(s);
        Color32[] colors = new Color32[512];
        for (int i = 0; i < 256; i++) {
            p[256 + i] = p[i] = (int)(Random.value * 255);
            colors[256 + i] = colors[i] = new Color32((byte)p[i], 0, 0, 0);
        }

        perm_tex = new Texture2D(512, 1, TextureFormat.ARGB32, false);
        perm_tex.Apply();
    }

    public int[] GetQuadTris(int offset, int num)
    {
        int tri_offset = num * 8;

        int[] result = new int[12];
        result[0] = offset + tri_offset + 0;
        result[1] = offset + tri_offset + 1;
        result[2] = offset + tri_offset + 2;
        result[3] = offset + tri_offset + 2;
        result[4] = offset + tri_offset + 1;
        result[5] = offset + tri_offset + 3;

        result[6] = offset + tri_offset + 2;
        result[7] = offset + tri_offset + 1;
        result[8] = offset + tri_offset + 0;
        result[9] = offset + tri_offset + 3;
        result[10] = offset + tri_offset + 1;
        result[11] = offset + tri_offset + 2;

        return result;
    }

    public int[] GenIndices(int size_x, int size_z)
    {
        List<int> tris = new List<int>();

        int verts = 0;
        for (int x = 0; x < size_x; x++)
        {
            for (int z = 0; z < size_z; z++)
            {
                int offset = verts;

                tris.AddRange(GetQuadTris(offset, 0));
                verts += 8;

                tris.AddRange(GetQuadTris(offset, 1));
                verts += 8;

                tris.AddRange(GetQuadTris(offset, 2));
                verts += 8;
            }
        }

        return tris.ToArray();
    }

    Vector3 GetTangent(Vector3 normal, Vector3 other)
    {
        Vector3 tangent;
        Vector3 t1 = Vector3.Cross(normal, other);
        Vector3 t2 = Vector3.Cross(normal, Vector3.up);
        if (t1.magnitude > t2.magnitude)
        {
            tangent = t1;
        }
        else
        {
            tangent = t2;
        }
        return tangent;
    }

    public void TestComputeShader(Vector3 postion, float[] heightmap)
    {
        Init_perm(0);

        int f_sizeX = 10;
        int f_sizeZ = 10;
        int perMeterX = 4;
        int perMeterZ = 4;

        int voxelOffset_x = 10;
        int voxelOffset_y = 10;

        Heightmap = heightmap;

        intermediate = new Plant[f_sizeX * perMeterX * f_sizeZ * perMeterZ];
        int indexBuff_size = f_sizeX * perMeterX * f_sizeZ * perMeterZ * 8 * 3;

        Debug.Log("indexBuff_size: " + indexBuff_size);
        vertexData = new VertexData[indexBuff_size];
        indices = GenIndices(f_sizeX * perMeterX, f_sizeZ * perMeterZ);


        ComputeBuffer buffer = new ComputeBuffer(heightmap.Length, sizeof(float));
        buffer.SetData(heightmap);

        ComputeBuffer output_buff = new ComputeBuffer(intermediate.Length, Plant.GetSize());
        output_buff.SetData(intermediate);

        //ComputeBuffer g_buff = new ComputeBuffer(indexBuff_size, sizeof(int));
        GraphicsBuffer g_buff = new GraphicsBuffer(GraphicsBuffer.Target.Index, indices.Length, sizeof(int));
        g_buff.SetData(indices);

        ComputeBuffer v_buff = new ComputeBuffer(indexBuff_size, VertexData.GetSize());
        v_buff.SetData(vertexData);
       

        int k = shader.FindKernel("CSMain");
        int c_k = shader.FindKernel("CompileMesh");
        shader.SetInt("sizeX", SmoothVoxelSettings.ChunkSizeX);
        shader.SetInt("sizeZ", SmoothVoxelSettings.ChunkSizeZ);
        shader.SetInt("f_sizeX", f_sizeX);
        shader.SetInt("f_sizeZ", f_sizeZ);
        shader.SetInt("perMeterX", perMeterX);
        shader.SetInt("perMeterZ", perMeterZ);
        shader.SetInt("voxel_offset_x", voxelOffset_x);
        shader.SetInt("voxel_offset_z", voxelOffset_y);
        shader.SetBuffer(k, "heightmap", buffer);
        shader.SetBuffer(k, "intermediate", output_buff);

        shader.SetBuffer(c_k, "intermediate", output_buff);
        //shader.SetBuffer(c_k, "indices", g_buff);
        shader.SetBuffer(c_k, "vertex", v_buff);

        shader.Dispatch(k, f_sizeX * 4, 1, f_sizeZ * 4);
        shader.Dispatch(c_k, 1, 1, 1);


        v_buff.GetData(vertexData);
        g_buff.GetData(indices);

        verts = new List<Vector3>();
        uv = new List<Vector2>();
        normals = new List<Vector3>();

        for (int i = 0; i < indexBuff_size; i++)
        {
            verts.Add(vertexData[i].Vertex);
            uv.Add(vertexData[i].UV);
            normals.Add(vertexData[i].Normal);
        }

        rend = GetComponent<MeshRenderer>();

        rend.material.SetPass(0);
        rend.material.SetBuffer("_Vertex", v_buff);
        rend.material.SetTexture("_permutation", perm_tex);

        MeshFilter filter = gameObject.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[indexBuff_size];//verts.ToArray();
        mesh.triangles = indices;
        //mesh.uv = uv.ToArray();

        Vector3 corner = postion;
        //new Bounds()
        Bounds b = new Bounds(corner + new Vector3(SmoothVoxelSettings.MeterSizeX / 2 - 0.5f, SmoothVoxelSettings.MeterSizeY / 2 - 0.5f, SmoothVoxelSettings.MeterSizeZ / 2 - 0.5f),
                              new Vector3(SmoothVoxelSettings.MeterSizeX + 1, SmoothVoxelSettings.MeterSizeY + 1, SmoothVoxelSettings.MeterSizeZ + 1));

        mesh.bounds = b;

        filter.sharedMesh = mesh;

        inited = true;

        //Vector3 corner = postion + new Vector3(voxelOffset_x, 0, voxelOffset_y);
        //Bounds b = new Bounds(corner + new Vector3(f_sizeX / 2, SmoothVoxelSettings.ChunkSizeY / 2, f_sizeZ / 2), new Vector3(f_sizeX / 2, SmoothVoxelSettings.ChunkSizeY / 2, f_sizeZ / 2));





        //Graphics.DrawProcedural(rend.material, b, MeshTopology.Triangles, indexBuff_size);
        // Graphics.DrawProceduralIndirect(rend.material, b, MeshTopology.Triangles, g_buff);
        //Graphics.DrawProcedural(rend.material, b, MeshTopology.Triangles, g_buff, indices.Length);

        return;

        output_buff.GetData(intermediate);


        List<int> tris = new List<int>();

        for (int x = 0; x < f_sizeX * 4; x++)
        {
            for (int z = 0; z < f_sizeZ * 4; z++)
            {
                Plant res = intermediate[x * (f_sizeZ * 4) + z];

                int offset = verts.Count;

                verts.AddRange(res.q1.GetVerts());
                tris.AddRange(res.q1.GetTris(offset));
                uv.AddRange(res.q1.GetUVs());

                verts.AddRange(res.q2.GetVerts());
                tris.AddRange(res.q2.GetTris(offset));
                uv.AddRange(res.q2.GetUVs());

                verts.AddRange(res.q3.GetVerts());
                tris.AddRange(res.q3.GetTris(offset));
                uv.AddRange(res.q3.GetUVs());

                //Debug.DrawRay(verts[0], res.normal, Color.green, 1000000);

                //Debug.Log("|" + res.q1.ToString() + " " + res.q2.ToString() + " " + res.q3.ToString() + " | ");

                //Vector4 res = output[x * (20 * 4) + z];

                //Vector3 normal = res;
                //float height = res.w;

                //Vector3 point = new Vector3(x / 4f, height, z / 4f);
                //Debug.DrawRay(point, normal, Color.green, 1000000);

                /*if (x % 8 == 0 &&
                    z % 8 == 0)
                {
                    Vector3 t1 = GetTangent(normal, Vector3.forward);
                    Vector3 t2 = GetTangent(normal, Vector3.right);

                    Quaternion q1 = Quaternion.LookRotation(normal, Vector3.up);
                    //Quaternion q2 = Quaternion.LookRotation(t2, Vector3.up);



                    Debug.DrawRay(point, normal, Color.green, 1000000);
                    //Debug.DrawRay(point, t1, Color.blue, 1000000);
                    //Debug.DrawRay(point, t2, Color.red, 1000000);
                    //Quaternion quat = q1 * q2;
                    //Instantiate(obj, point, q1);
                }*/
            }
        }

        Debug.Log("Num tris: " + tris.Count);

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uv.ToArray();

        mesh.RecalculateNormals();

        filter.sharedMesh = mesh;
    }
}
