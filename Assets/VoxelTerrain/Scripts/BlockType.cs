using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

[Serializable]
public struct BlockType {
    public BaseType baseType;
    public byte type;
    public string name;
    public int[] textureIndex;
    GameObject prefab;

    public BlockType(BaseType _baseType, byte _type, string _name, int[] _index, GameObject _prefab) {
        baseType = _baseType;
        type = _type;
        name = _name;
        textureIndex = _index;
        prefab = _prefab;
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
}

public enum BaseType {
    air,
    solid,
    prefab
}

