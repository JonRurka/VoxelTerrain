using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential), Serializable]
public struct Vector3Int : IEquatable<Vector3Int>
{
    public int x;
    public int y;
    public int z;
    public Vector3Int(int _x, int _y, int _z) {
        x = _x;
        y = _y;
        z = _z;
    }
    public static Vector3Int[] Vector2ArrayToSVector2Int(Vector3[] inVector) {
        Vector3Int[] result = new Vector3Int[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static Vector3[] SVector3ArrayToVector3(Vector3Int[] inVector) {
        Vector3[] result = new Vector3[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static implicit operator Vector3Int(Vector3 inVector) {
        return new Vector3Int((int)inVector.x, (int)inVector.y, (int)inVector.z);
    }
    public static implicit operator Vector3(Vector3Int inVector) {
        return new Vector3(inVector.x, inVector.y, inVector.z);
    }
    public static bool operator ==(Vector3Int first, Vector3Int second) {
        return (first.x == second.x && first.y == second.y && first.z == second.z);
    }
    public static bool operator !=(Vector3Int first, Vector3Int second) {
        return (first.x != second.x || first.y != second.y || first.z != second.z);
    }
    public static Vector3Int operator +(Vector3Int first, Vector3Int second) {
        Vector3Int result = new Vector3Int();
        result.x = first.x + second.x;
        result.y = first.y + second.y;
        result.z = first.z + second.z;
        return result;
    }
    public static Vector3Int operator *(Vector3Int first, Vector3Int second) {
        Vector3Int result = new Vector3Int();
        result.x = first.x * second.x;
        result.y = first.y * second.y;
        result.z = first.z * second.z;
        return result;
    }
    public static Vector3Int operator *(Vector3Int first, int val) {
        Vector3Int result = new Vector3Int();
        result.x = first.x * val;
        result.y = first.y * val;
        result.z = first.z * val;
        return result;
    }
    public static int Distance(Vector3Int a, Vector3Int b) {
        int deltaX = b.x - a.x;
        int deltaY = b.y - a.y;
        int deltaZ = b.z - a.z;
        return (int)Mathf.Abs(Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ)));
    }
    public override bool Equals(object obj) {
        if (obj is Vector3Int)
        {
            return Equals((Vector3Int)obj);
        }
        return false;
    }
    public override int GetHashCode() {
        return (x << 16) ^ (y << 8) ^ z;
    }
    public override string ToString() {
        return string.Format("({0}, {1}, {2})", x, y, z);
    }
    public string File_String()
    {
        return string.Format("{0}_{1}_{2}", x, y, z);
    }
    public bool Equals(Vector3Int other)
    {
        return (x == other.x && y == other.y && z == other.z);
    }
}

[StructLayout(LayoutKind.Sequential), Serializable]
public struct Vector2Int {
    public int x;
    public int y;
    public Vector2Int(int _x, int _y) {
        x = _x;
        y = _y;
    }
    public static implicit operator Vector2(Vector2Int inVector) {
        return new Vector2(inVector.x, inVector.y);
    }
    public static implicit operator Vector2Int(Vector2 inVector) {
        return new Vector2Int((int)inVector.x, (int)inVector.y);
    }
    public static Vector2Int[] Vector2ArrayToSVector2Int(Vector2[] inVector) {
        Vector2Int[] result = new Vector2Int[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static Vector2[] SVector3ArrayToVector3(Vector2Int[] inVector) {
        Vector2[] result = new Vector2[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static bool operator ==(Vector2Int first, Vector2Int second) {
        return (first.x == second.x && first.y == second.y);
    }
    public static bool operator !=(Vector2Int first, Vector2Int second) {
        return (first.x != second.x || first.y != second.y);
    }
    public static Vector2Int operator +(Vector2Int first, Vector2Int second) {
        Vector2Int result = new Vector2Int();
        result.x = first.x + second.x;
        result.y = first.y + second.y;
        return result;
    }
    public static Vector2Int operator *(Vector2Int first, Vector2Int second) {
        Vector2Int result = new Vector2Int();
        result.x = first.x * second.x;
        result.y = first.y * second.y;
        return result;
    }
    public static float Distance(Vector2Int a, Vector2Int b) {
        int deltaX = b.x - a.x;
        int deltaY = b.y - a.y;
        return Mathf.Abs(Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY)));
    }
    public override bool Equals(object obj) {
        return base.Equals(obj);
    }
    public override int GetHashCode() {
        return (x << 8) ^ y;
    }
    public override string ToString() {
        return string.Format("({0}, {1})", x, y);
    }
    public string File_String()
    {
        return string.Format("{0}_{1}", x, y);
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Vector2[] UVs;
    //public Color[] Colors;
    //public Vector2[] UVs2;
    //public Vector2[] UVs3;
    public MeshData(Vector3[] _vertices, int[] _triangles, Vector2[] _UVs) {
        vertices = _vertices;
        triangles = _triangles;
        UVs = _UVs;
        normals = null;
        //Colors = null;
        //UVs2 = null;
        //UVs3 = null;
    }
    public MeshData(Vector3[] _vertices, int[] _triangles, Vector2[] _UVs, Vector3[] _normals)
    {
        vertices = _vertices;
        triangles = _triangles;
        UVs = _UVs;
        normals = _normals;
        //Colors = data;
        //UVs2 = _UVs2;
        //UVs3 = _UVs3;
    }
    public int GetSize()
    {
        int vertSize = vertices.Length * sizeof(float) * 3;
        int triSize = triangles.Length * sizeof(int);
        int uvSize = 0;// UVs.Length * 8;
        return vertSize + triSize + uvSize;
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Block
{
    public uint type;
    public float iso;
    public bool set;
    public Block(uint type)
    {
        this.type = type;
        this.iso = 0;
        this.set = false;
    }
    public Block(float iso)
    {
        this.iso = iso;
        this.type = 0;
        this.set = false;
    }
    public Block(uint type, float iso)
    {
        this.iso = iso;
        this.type = type;
        this.set = false;
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)] 
public struct GridPoint
{
    public float x;
    public float y;
    public float z;
    public float iso;
    public uint type;
    public Vector3Int OriginLocal;
    public Vector3Int OriginGlobal;

    public GridPoint(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        iso = 0;
        type = 0;
        OriginLocal = new Vector3Int();
        OriginGlobal = new Vector3Int();
    }

    public GridPoint(float x, float y, float z, float iso)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.iso = iso;
        type = 0;
        OriginLocal = new Vector3Int();
        OriginGlobal = new Vector3Int();
    }

    public GridPoint(float x, float y, float z, float iso, uint type)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.iso = iso;
        this.type = type;
        OriginLocal = new Vector3Int();
        OriginGlobal = new Vector3Int();
    }

    public GridPoint(Vector3Int local, Vector3Int global, float iso, uint type)
    {
        this.x = global.x;
        this.y = global.y;
        this.z = global.z;
        this.iso = iso;
        this.type = type;
        OriginLocal = local;
        OriginGlobal = global;
    }

    public static implicit operator Vector3(GridPoint input)
    {
        return new Vector3(input.x, input.y, input.z);
    }
}

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Quad
{
    public Vector3 vertices_0;
    public Vector3 vertices_1;
    public Vector3 vertices_2;
    public Vector3 vertices_3;
    public Vector3 vertices_4;
    public Vector3 vertices_5;
    public Vector3 vertices_6;
    public Vector3 vertices_7;

    public int tris_0;
    public int tris_1;
    public int tris_2;
    public int tris_3;
    public int tris_4;
    public int tris_5;
    public int tris_6;
    public int tris_7;
    public int tris_8;
    public int tris_9;
    public int tris_10;
    public int tris_11;

    public Vector2 uv_0;
    public Vector2 uv_1;
    public Vector2 uv_2;
    public Vector2 uv_3;
    public Vector2 uv_4;
    public Vector2 uv_5;
    public Vector2 uv_6;
    public Vector2 uv_7;

    public static int GetSize()
    {
        return (sizeof(float) * 3 * 8) + (sizeof(int) * 12) + (sizeof(float) * 2 * 8);
    }

    public Vector3[] GetVerts()
    {
        return new Vector3[] { vertices_0, vertices_1, vertices_2, vertices_3,
            vertices_4, vertices_5, vertices_6, vertices_7};
    }

    public int[] GetTris(int offset)
    {
        return new int[] { offset + tris_0, offset + tris_1, offset + tris_2, offset + tris_3, offset + tris_4, offset + tris_5,
             offset + tris_6, offset + tris_7, offset + tris_8, offset + tris_9, offset + tris_10, offset + tris_11};
    }

    public Vector2[] GetUVs()
    {
        return new Vector2[] { uv_0, uv_1, uv_2, uv_3,
            uv_4, uv_5, uv_6, uv_7};
    }
};

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Plant
{
    public Quad q1;
    public Quad q2;
    public Quad q3;

    public int type;

    public static int GetSize()
    {
        return Quad.GetSize() * 3 + sizeof(int);
    }
};

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct VertexData
{
    public Vector3 Vertex;
    public Vector2 UV;
    public Vector3 Normal;
    public Vector3 Color;

    public static int GetSize()
    {
        return sizeof(float) * 3 + sizeof(float) * 2 + sizeof(float) * 3 + sizeof(float) * 3;
    }
};

[StructLayout(LayoutKind.Sequential, Size = 5), Serializable]
struct SaveBlock
{
    public byte type;
    public float iso;

    public SaveBlock(byte type, float iso)
    {
        this.type = type;
        this.iso = iso;
    }
}

[StructLayout(LayoutKind.Sequential), Serializable]
struct SaveStructure
{

    public int location_x;
    public int location_y;
    public int location_z;

    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20 * 20)]
    public float[] heightmap;


    public byte c0_min;
    public byte c0_max;
    public byte[] blocks_type_c0;
    public float[] blocks_iso_c0;

    public byte c1_min;
    public byte c1_max;
    public byte[] blocks_type_c1;
    public float[] blocks_iso_c1;

    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20 * 20 * 128 * 5)]
    //public SaveBlock[] blocks_c0;

    /*public byte c1_min;
    public byte c1_max;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20 * 20 * 128 * 5)]
    public SaveBlock[] blocks_c1;*/

    public void Serialize(System.IO.Stream stream)
    {
        stream.Seek(0, System.IO.SeekOrigin.Begin);
        System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, System.Text.Encoding.Default, true);

        writer.Write(location_x);
        writer.Write(location_y);
        writer.Write(location_z);

        byte[] h_buff = new byte[heightmap.Length * 4];
        Buffer.BlockCopy(heightmap, 0, h_buff, 0, h_buff.Length);
        writer.Write(h_buff);

        // Chunk 0
        writer.Write(c0_min);
        writer.Write(c0_max);

        writer.Write(blocks_type_c0);

        byte[] iso_buf = new byte[blocks_iso_c0.Length * 4];
        Buffer.BlockCopy(blocks_iso_c0, 0, iso_buf, 0, iso_buf.Length);
        writer.Write(iso_buf);

        // Chunk 1
        writer.Write(c1_min);
        writer.Write(c1_max);

        writer.Write(blocks_type_c1);

        //iso_buf = new byte[blocks_iso_c1.Length * 4];
        Buffer.BlockCopy(blocks_iso_c1, 0, iso_buf, 0, iso_buf.Length);
        writer.Write(iso_buf);

        writer.Flush();
        writer.Close();
    }

    public void Deserialize(System.IO.Stream stream)
    {
        //stream.Seek(0, System.IO.SeekOrigin.Begin);
        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream, System.Text.Encoding.Default, true);


        location_x = reader.ReadInt32();
        location_y = reader.ReadInt32();
        location_z = reader.ReadInt32();

        heightmap = new float[20 * 20];
        byte[] h_buff = new byte[heightmap.Length * 4];
        reader.Read(h_buff, 0, h_buff.Length);
        Buffer.BlockCopy(h_buff, 0, heightmap, 0, h_buff.Length);

        // Chunk 0
        c0_min = reader.ReadByte();
        c0_max = reader.ReadByte();

        blocks_type_c0 = new byte[20 * 20 * 128]; 
        reader.Read(blocks_type_c0, 0, blocks_type_c0.Length);

        blocks_iso_c0 = new float[20 * 20 * 128];
        byte[] iso_buf = new byte[blocks_iso_c0.Length * 4];
        reader.Read(iso_buf, 0, iso_buf.Length);
        Buffer.BlockCopy(iso_buf, 0, blocks_iso_c0, 0, iso_buf.Length);

        // Chunk 1
        c1_min = reader.ReadByte();
        c1_max = reader.ReadByte();

        blocks_type_c1 = new byte[20 * 20 * 128];
        reader.Read(blocks_type_c1, 0, blocks_type_c1.Length);

        blocks_iso_c1 = new float[20 * 20 * 128];
        reader.Read(iso_buf, 0, iso_buf.Length);
        Buffer.BlockCopy(iso_buf, 0, blocks_iso_c1, 0, iso_buf.Length);

        reader.Close();
    }
}

[Serializable]
public class VoxelMaterial
{
    public string Name;
    public BaseType Type;
    public Color Color;
    public Texture2D[] Textures;
}

public enum LOD_Mode
{
    Empty,
    Heightmap,
    ReducedDepth,
    Full,
}