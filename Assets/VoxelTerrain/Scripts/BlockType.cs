using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

[Serializable]
public class BlockType {
    public BaseType baseType;
    public byte type;
    public string name;
    public int[] textureIndex;
    public float[] ScaledIndex;
    public Color color;
    GameObject prefab;

    public BlockType(BaseType _baseType, byte _type, string _name, Color col, int[] _index, GameObject _prefab) {
        baseType = _baseType;
        type = _type;
        name = _name;
        color = col;
        textureIndex = _index;
        prefab = _prefab;
        ScaledIndex = new float[_index.Length];
    }
    public override string ToString() {
        string UVs = string.Empty;
        for (int i = 0; i < 6; i++) {
            UVs += textureIndex[i];
            if (i != 5) {
                UVs += ", ";
            }
        }
        return type.ToString() + "; " + baseType.ToString() + "; " + name + ", " + UVs;
    }
    public void SetScaledIndices(int length)
    {
        for (int i = 0; i < textureIndex.Length; i++)
        {
            ScaledIndex[i] = VoxelConversions.Scale(textureIndex[i], 0, length, 0, 1);
        }
    }
}

public enum BaseType {
    air,
    liquid,
    solid,
    prefab
}

