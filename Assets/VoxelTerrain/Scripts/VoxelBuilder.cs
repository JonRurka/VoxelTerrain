using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibNoise;
using LibNoise.Models;
using LibNoise.Modifiers;

[Serializable]
public class VoxelBuilder : IVoxelBuilder {
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Neighbor {
        public bool Exists;
        public bool generated;
        public Vector3Int pagePositon;
        public IVoxelBuilder controller;
        public Neighbor(bool _exists, bool _generated, Vector3Int _location, IVoxelBuilder _controller) {
            Exists = _exists;
            generated = _generated;
            pagePositon = _location;
            controller = _controller;
        }
    }
    public Block[][][] blocks;
    public float[,] SurfaceData;
    public IPageController controller;
    public BlockType[] BlockTypes;
    public Rect[] AtlasUvs;
    public Neighbor[] neighbors;
    public bool Initialized;
    public bool enableBackgroundX = false;
    public bool enableBackgroundY = false;
    public bool enableBackgroundZ = false;
    public bool deactivated;

    public int VoxelsPerMeter;
    public int ChunkMeterSizeX;
    public int ChunkMeterSizeY;
    public int ChunkMeterSizeZ;
    public int ChunkSizeX;
    public int ChunkSizeY;
    public int ChunkSizeZ;
    public float half;

    public Vector3Int Location;

    public int seed;
    public float heightmapSize;
    public bool enableCaves;
    public float amp;
    public float caveDensity;
    public float groundOffset;
    public float grassOffset;

    public IModule NoiseModule;
    public IModule caveModule;
    public Noise2D NoisePlane;

    public string totalTime;
    public string GetSideTime;
    public string renderTime;
    public string GetBlockTime;

    System.Diagnostics.Stopwatch getBlockTypeWatch;

    public VoxelBuilder(IPageController _controller) {
        controller = _controller;
        Location = new Vector3Int();
        VoxelsPerMeter = 1;
        ChunkMeterSizeX = 10;
        ChunkMeterSizeY = 10;
        ChunkMeterSizeZ = 10;
        CalculateVariables();
    }

    public VoxelBuilder(IPageController _controller, Vector3Int _Location, int _VoxelsPerMeter, int _ChunkMeterSize) {
        controller = _controller;
        Location = _Location;
        VoxelsPerMeter = _VoxelsPerMeter;
        ChunkMeterSizeX = _ChunkMeterSize;
        ChunkMeterSizeY = _ChunkMeterSize;
        ChunkMeterSizeZ = _ChunkMeterSize;
        CalculateVariables();
    }

    public VoxelBuilder(IPageController _controller, Vector3Int _Location, int _VoxelsPerMeter, int _ChunkMeterSizeX, int _ChunkMeterSizeY, int _ChunkMeterSizeZ) {
        controller = _controller;
        Location = _Location;
        VoxelsPerMeter = _VoxelsPerMeter;
        ChunkMeterSizeX = _ChunkMeterSizeX;
        ChunkMeterSizeY = _ChunkMeterSizeY;
        ChunkMeterSizeZ = _ChunkMeterSizeZ;
        CalculateVariables();
    }

    public void SetBlockTypes(BlockType[] _blockTypeList, Rect[] _AtlasUvs) {
        BlockTypes = _blockTypeList;
        AtlasUvs = _AtlasUvs;
    }

    public void Generate(IModule module, int _seed, bool _enableCaves, float _amp, float _caveDensity, float _grassOffset)
    {
        try
        {
            NoiseModule = module;
            seed = _seed;
            enableCaves = _enableCaves;
            amp = _amp;
            caveDensity = _caveDensity;
            grassOffset = _grassOffset;

            RidgedMultifractal _caves = new RidgedMultifractal();
            _caves.Seed = _seed;
            _caves.Frequency = 0.3;
            caveModule = _caves;

            Vector2Int bottomLeft = new Vector2(Location.x * ChunkSizeX, Location.z * ChunkSizeZ);
            Vector2Int topRight = new Vector2(Location.x * ChunkSizeX + ChunkSizeX, Location.z * ChunkSizeZ + ChunkSizeZ);

            SetSurfaceData(bottomLeft, topRight);
        }
        catch (Exception e)
        {
            SafeDebug.LogError(string.Format("{0}\nFunction: Generate\n Chunk: {1}", e.Message, Location.ToString()), e);
        }
    }

    public void Generate(int _seed, bool _enableCaves, float _amp, float _caveDensity, float _grassOffset) {
        try {
            seed = _seed;
            enableCaves = _enableCaves;
            amp = _amp;
            caveDensity = _caveDensity;
            grassOffset = _grassOffset;

            Vector2Int bottomLeft = new Vector2(Location.x * ChunkSizeX, Location.z * ChunkSizeZ);
            Vector2Int topRight = new Vector2(Location.x * ChunkSizeX + ChunkSizeX, Location.z * ChunkSizeZ + ChunkSizeZ);

            MazeGen mountainTerrain = new MazeGen(ChunkSizeX/2, 4, ChunkMeterSizeY/2, seed, 2, 1);

            NoiseModule = mountainTerrain;

            //NoisePlane = new LibNoise.Models.Plane(NoiseModule);

            //SetSurfaceData(bottomLeft, topRight);
        }
        catch (Exception e) {
            SafeDebug.LogError(e.Message + "\nFunction: Generate, Chunk: " + Location.ToString(), e);
        }
    }

    public MeshData Render(bool renderOnly) {
        if (renderOnly) {
            return RenderOnly();
        }
        else {
            return Render();
        }
    }

    public MeshData Render() {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        if (Initialized) {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            System.Diagnostics.Stopwatch getSideWatch = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch renderWatch = new System.Diagnostics.Stopwatch();
            getBlockTypeWatch = new System.Diagnostics.Stopwatch();
            SetSurroundingChunks();
            float sideLength = 1.0f / (float)VoxelsPerMeter;

            int cx = Location.x;
            int cy = Location.y;
            int cz = Location.z;

            int xStart = cx * ChunkSizeX;
            int xEnd = cx * ChunkSizeX + ChunkSizeX;

            int yStart = cy * ChunkSizeY;
            int yEnd = cy * ChunkSizeY + ChunkSizeY;

            int zStart = cz * ChunkSizeZ;
            int zEnd = cz * ChunkSizeZ + ChunkSizeZ;

            int localX = 0;
            int localY = 0;
            int localZ = 0;

            int superLocX = 0;
            int superLocY = 0;
            int superLocZ = 0;
            try {
                for (superLocX = xStart, localX = 0; localX < ChunkSizeX; superLocX++, localX++) {
                    for (superLocZ = zStart, localZ = 0; localZ < ChunkSizeZ; superLocZ++, localZ++) {
                        int NoiseLocationX = superLocX;
                        int NoiseLocationZ = superLocZ;
                        for (superLocY = yStart, localY = 0; localY < ChunkSizeY; superLocY++, localY++) {
                            if (!deactivated)
                            {
                                int NoiseLocationY = (Location.y * ChunkSizeY) + localY;
                                int NoiseLocationY_Scaled = NoiseLocationY * VoxelsPerMeter;
                                Vector3Int noisePos = new Vector3Int(NoiseLocationX, NoiseLocationY, NoiseLocationZ);
                                Vector3Int globalPos = new Vector3Int(superLocX, superLocY, superLocZ);
                                Vector3Int localPos = new Vector3Int(localX, localY, localZ);
                                byte _byteType = GetBlockType(localPos, noisePos, globalPos, true);
                                BlockType _type = BlockTypes[_byteType];
                                int[] _textures = _type.textureIndex;

                                getSideWatch.Start();
                                byte[] surroundingBlocks = GetSurroundingBlocks(noisePos, globalPos, localPos, true);
                                getSideWatch.Stop();

                                renderWatch.Start();
                                RenderSides(_type, surroundingBlocks, globalPos, localPos, ref vertices, ref triangles, ref uv, sideLength, _type.textureIndex);
                                renderWatch.Stop();
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                SafeDebug.LogError("Message: " + e.Message + ", \nFunction: Render,\nLocation: " + Location +
                                    string.Format("\nglobal: {0}/{1}, {2}/{3}, {4}/{5}\nlocal: {6}/{7}, {8}/{9}, {10}/{11}", 
                                    superLocX, VoxelSettings.SuperSizeX, superLocY, VoxelSettings.SuperSizeY, superLocZ, VoxelSettings.SuperSizeZ, localX, ChunkSizeX, localY, ChunkSizeY, localZ, ChunkSizeZ), e);
            }
            watch.Stop();
            totalTime = watch.Elapsed.ToString();
            GetSideTime = getSideWatch.Elapsed.ToString();
            renderTime = renderWatch.Elapsed.ToString();
            GetBlockTime = getBlockTypeWatch.Elapsed.ToString();
        }
        else {
            Debug.LogError("Render called without being initialized.");
        }
        return new MeshData(vertices.ToArray(), triangles.ToArray(), uv.ToArray());
    }

    public MeshData RenderOnly() {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        if (Initialized) {
            SetSurroundingChunks();
            float sideLength = 1.0f / (float)VoxelsPerMeter;

            int cx = Location.x;
            int cy = Location.y;
            int cz = Location.z;

            int xStart = cx * ChunkSizeX;
            int xEnd = cx * ChunkSizeX + ChunkSizeX;

            int yStart = cy * ChunkSizeY;
            int yEnd = cy * ChunkSizeY + ChunkSizeY;

            int zStart = cz * ChunkSizeZ;
            int zEnd = cz * ChunkSizeZ + ChunkSizeZ;

            int localX = 0;
            int localY = 0;
            int localZ = 0;

            int superLocX = 0;
            int superLocY = 0;
            int superLocZ = 0;

            for (superLocX = xStart, localX = 0; localX < ChunkSizeX; superLocX++, localX++) {
                for (superLocZ = zStart, localZ = 0; localZ < ChunkSizeZ; superLocZ++, localZ++) {
                    for (superLocY = yStart, localY = 0; localY < ChunkSizeY; superLocY++, localY++) {
                        if (!deactivated)
                        {
                            byte _byteType = GetBlock(localX, localY, localZ).type;
                            BlockType _type = BlockTypes[_byteType];
                            int[] _textures = _type.textureIndex;
                            Vector3Int globalPos = new Vector3Int(superLocX, superLocY, superLocZ);
                            Vector3Int localPos = new Vector3Int(localX, localY, localZ);

                            byte[] surroundingBlocks = GetSurroundingBlocks(globalPos, globalPos, localPos, false);

                            RenderSides(_type, surroundingBlocks, globalPos, localPos, ref vertices, ref triangles, ref uv, sideLength, _type.textureIndex);
                        }
                    }
                }
            }
            ResetPageNeighbors();
        }
        return new MeshData(vertices.ToArray(), triangles.ToArray(), uv.ToArray());
    }

    public byte GetBlockType(int LocalPositionX, int LocalPositionY, int LocalPositionZ, int NoiseLocationX, int NoiseLocationY, int NoiseLocationZ, int globalY, bool generate) {
        return GetBlockType(new Vector3Int(LocalPositionX, LocalPositionY, LocalPositionZ), new Vector3Int(NoiseLocationX, NoiseLocationY, NoiseLocationZ), new Vector3Int(0, globalY, 0), generate);
    }

    public byte GetBlockType(Vector3Int LocalPosition, Vector3Int NoiseLocation, Vector3Int globalLocation, bool generate) {
        try {
            if (generate && !IsBlockSet(LocalPosition.x, LocalPosition.y, LocalPosition.z)) {
                byte generatedBlock = GetBlockType(LocalPosition.x, LocalPosition.y, LocalPosition.z, NoiseLocation.x, NoiseLocation.y, NoiseLocation.z, globalLocation.y);
                SetBlock(LocalPosition.x, LocalPosition.y, LocalPosition.z, new Block(generatedBlock));
                MarkAsSet(LocalPosition.x, LocalPosition.y, LocalPosition.z);
            }
            return GetBlock(LocalPosition.x, LocalPosition.y, LocalPosition.z).type;
        }
        catch (Exception e) {
            if (SurfaceData != null)
                SafeDebug.LogError(string.Format("Message: {0}\nnoiseX={1}, noiseZ={2}\nGenerate: {3}", e.Message, NoiseLocation.x, NoiseLocation.z, generate.ToString()), e);
            else
                SafeDebug.LogError(string.Format("Message: {0}\nnoiseX={1}, noiseZ={2}\nGenerate: {3}", e.Message, NoiseLocation.x, NoiseLocation.z, generate.ToString()), e);
            return 0;
        }
    }

    public byte GetBlockType(int LocalPositionX, int LocalPositionY, int LocalPositionZ, int NoiseLocationX, int NoiseLocationY, int NoiseLocationZ, int globalY)
    {
        
        byte result = 0;
        try
        {
            float _stoneHeight = GetSurfaceHeight(LocalPositionX, LocalPositionZ);
            float _dirtHeight = _stoneHeight + grassOffset - 1;
            float _grassHeight = _dirtHeight + 1;

            // Set surface layers.
            byte groundType = 0;

            if (_stoneHeight > NoiseLocationY)
            {
                groundType = 2;
            }
            else if (_dirtHeight > NoiseLocationY)
            {
                groundType = 3;
            }
            else if (_grassHeight > NoiseLocationY)
            {
                groundType = 1;
            }

            // set caves.
            if (groundType != 0)
            {
                if (enableCaves && Noise(caveModule, NoiseLocationX, NoiseLocationY, NoiseLocationZ, 16.0 * VoxelsPerMeter, 17.0, 1.0) > caveDensity)
                {
                        groundType = 0;
                }
            }

            result = groundType;

            //if (NoiseLocationY == 0)
            //    result = 1;
            //else result = 0;
        }
        catch(Exception e)
        {
            SafeDebug.LogError(string.Format("Message: {0}\nnoiseX={1}, noiseZ={2}\nlocalX={3}/{4}, localZ={5}/{6}",
                e.Message, NoiseLocationX, NoiseLocationZ, LocalPositionX, SurfaceData.GetLength(0), LocalPositionZ, SurfaceData.GetLength(1)), e);
        }
        
        return result;
    }

    public void SetBlock(int _x, int _y, int _z, Block block) {
        if (!deactivated)
        {
            //if (IsInBounds(_x, _y, _z))
            //{
                blocks[_x][_y][_z].type = block.type;
            /*}
            else
            {
                SafeDebug.LogError(string.Format("Error: {0}/{1}, {2}/{3}, {4}/{5} is out of range. Function: SetBlock", _x, ChunkSizeX, _y, ChunkSizeY, _z, ChunkSizeZ));
            }*/
        }
    }

    public Block GetBlock(int _x, int _y, int _z)
    {
        if (!deactivated)
        {
            //if (IsInBounds(_x, _y, _z))
            //{
                return blocks[_x][_y][_z];
            /*}
            else
            {
                SafeDebug.LogError(string.Format("Error: {0}/{1}, {2}/{3}, {4}/{5} is out of range. Function: GetBlock", _x, ChunkSizeX, _y, ChunkSizeY, _z, ChunkSizeZ));
            }*/
        }
        return default(Block);
    }

    public void MarkAsSet(int _x, int _y, int _z) {
        if (!deactivated)
            //if (IsInBounds(_x, _y, _z))
            //{
                blocks[_x][_y][_z].set = true;
            /*}
            else
            {
                SafeDebug.LogError(string.Format("Error: {0}/{1}, {2}/{3}, {4}/{5} is out of range", _x, ChunkSizeX, _y, ChunkSizeY, _z, ChunkSizeZ));
            }*/
    }

    public bool IsBlockSet(int _x, int _y, int _z) {
        if (!deactivated)
            //if (IsInBounds(_x, _y, _z))
            //{
                return blocks[_x][_y][_z].set;
            /*}
            else
            {
                SafeDebug.LogError(string.Format("Error: {0}/{1}, {2}/{3}, {4}/{5} is out of range. Function: IsBlockSet", _x, ChunkSizeX, _y, ChunkSizeY, _z, ChunkSizeZ));
                return false;
            }*/
        return false;
    }

    public float GetSurfaceHeight(int NoiseLocationX, int NoiseLocationZ)
    {
        float value = 0;
        getBlockTypeWatch.Start();
        value = SurfaceData[NoiseLocationX + 1, NoiseLocationZ + 1];
        getBlockTypeWatch.Stop();
        /*if (!deactivated && IsInBounds(NoiseLocationX, 0, NoiseLocationZ))
        {
            value = SurfaceData[NoiseLocationX, NoiseLocationZ];
        }
        else
        {
            getBlockTypeWatch.Start();
            value = GetHeight(NoiseLocationX, NoiseLocationZ);
            getBlockTypeWatch.Stop();
        }*/
        
        return value;
    }

    public int Noise(IModule module, int _x, int _y, int _z, double _scale, double _height, double _power) {
        double rValue = 0;
        if (module != null)
        {
            rValue = module.GetValue(((double)_x) / _scale, ((double)_y) / _scale, ((double)_z) / _scale);
            rValue *= _height;

            if (_power != 0)
            {
                rValue = Mathf.Pow((float)rValue, (float)_power);
            }
        }
        return (int)rValue;
    }

    private byte[] GetSurroundingBlocks(Vector3Int _noise, Vector3Int _global, Vector3Int _local, bool generate) {
        byte[] surroundingBlocks = new byte[6];

        // Y+
        if (_local.y >= ChunkSizeY - 1)
        {
            if (neighbors[0].Exists && neighbors[0].generated)
            {
                surroundingBlocks[0] = neighbors[0].controller.GetBlock(_local.x, 0, _local.z).type;
            }
            else if (generate)
            {
                surroundingBlocks[0] = GetBlockType(_local.x, _local.y + 1, _local.z, _noise.x, _noise.y + 1, _noise.z, _global.y);
            }
            else
                surroundingBlocks[0] = 0;
        }
        else
        {
            surroundingBlocks[0] = GetBlockType(_local.x, _local.y + 1, _local.z, _noise.x, _noise.y + 1, _noise.z, _global.y + 1, generate);
        }

        // Y-
        if (_local.y <= 0)
        {
            if (neighbors[1].Exists && neighbors[1].generated)
            {
                surroundingBlocks[1] = neighbors[1].controller.GetBlock(_local.x, ChunkSizeY - 1, _local.z).type;
            }
            else if (generate)
            {
                surroundingBlocks[1] = GetBlockType(_local.x, _local.y - 1, _local.z, _noise.x, _noise.y - 1, _noise.z, _global.y);
            }
            else
                surroundingBlocks[1] = 0;
        }
        else
        {
            surroundingBlocks[1] = GetBlockType(_local.x, _local.y - 1, _local.z, _noise.x, _noise.y - 1, _noise.z, _global.y, generate);
        }

        // Z+
        if (_local.z >= ChunkSizeZ - 1)
        {
            if (neighbors[2].Exists && neighbors[2].generated)
            {
                surroundingBlocks[2] = neighbors[2].controller.GetBlock(_local.x, _local.y, 0).type;
            }
            else if (generate)
            {
                surroundingBlocks[2] = GetBlockType(_local.x, _local.y, _local.z + 1, _noise.x, _noise.y, _noise.z + 1, _global.y);
            }
            else
                surroundingBlocks[2] = 0;
        }
        else
        {
            surroundingBlocks[2] = GetBlockType(_local.x, _local.y, _local.z + 1, _noise.x, _noise.y, _noise.z + 1, _global.y, generate);
        }

        // Z-
        if (_local.z <= 0)
        {
            if (neighbors[3].Exists && neighbors[3].generated)
            {
                surroundingBlocks[3] = neighbors[3].controller.GetBlock(_local.x, _local.y, ChunkSizeZ - 1).type;
            }
            else if (generate)
            {
                surroundingBlocks[3] = GetBlockType(_local.x, _local.y, _local.z - 1, _noise.x, _noise.y, _noise.z - 1, _global.y);
            }
            else
                surroundingBlocks[3] = 0;
        }
        else
        {
            surroundingBlocks[3] = GetBlockType(_local.x, _local.y, _local.z - 1, _noise.x, _noise.y, _noise.z - 1, _global.y, generate);
        }

        // X+
        if (_local.x >= ChunkSizeX - 1)
        {
            if (neighbors[4].Exists && neighbors[4].generated)
            {
                surroundingBlocks[4] = neighbors[4].controller.GetBlock(0, _local.y, _local.z).type;
            }
            else if (generate)
            {
                surroundingBlocks[4] = GetBlockType(_local.x + 1, _local.y, _local.z, _noise.x + 1, _noise.y, _noise.z, _global.y);
            }
            else
                surroundingBlocks[4] = 0;
        }
        else
        {
            surroundingBlocks[4] = GetBlockType(_local.x + 1, _local.y, _local.z, _noise.x + 1, _noise.y, _noise.z, _global.y, generate);
        }

        // X-
        if (_local.x <= 0)
        {
            if (neighbors[5].Exists && neighbors[5].generated)
            {
                surroundingBlocks[5] = neighbors[5].controller.GetBlock(ChunkSizeX - 1, _local.y, _local.z).type;
            }
            else if (generate)
            {
                surroundingBlocks[5] = GetBlockType(_local.x - 1, _local.y, _local.z, _noise.x - 1, _noise.y, _noise.z, _global.y);
            }
            else
                surroundingBlocks[5] = 0;
        }
        else
        {
            surroundingBlocks[5] = GetBlockType(_local.x - 1, _local.y, _local.z, _noise.x - 1, _noise.y, _noise.z, _global.y, generate);
        }
        return surroundingBlocks;
    }

    private void RenderSides(BlockType _type, byte[] _sides, Vector3Int _GlobalPosition, Vector3Int _localPosition, ref List<Vector3> _vertices, ref List<int> _triangles, ref List<Vector2> _uv, float _sideLength, int[] _textureIndex) {
        if (_type.baseType == BaseType.solid) {
            int x = _localPosition.x;
            int y = _localPosition.y;
            int z = _localPosition.z;
            try {
                // y+
                if (BlockTypes[_sides[0]].baseType == BaseType.air) {
                    int vertexIndex = _vertices.Count;
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength + _sideLength, z * _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength + _sideLength, z * _sideLength + _sideLength));

                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength + _sideLength, z * _sideLength + _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength + _sideLength, z * _sideLength));

                    _triangles.Add(vertexIndex);
                    _triangles.Add(vertexIndex + 1);
                    _triangles.Add(vertexIndex + 2);

                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex + 3);
                    _triangles.Add(vertexIndex);

                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[0]].x, AtlasUvs[_textureIndex[0]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[0]].x + AtlasUvs[_textureIndex[0]].width, AtlasUvs[_textureIndex[0]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[0]].x + AtlasUvs[_textureIndex[0]].width, AtlasUvs[_textureIndex[0]].y + AtlasUvs[_textureIndex[0]].height));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[0]].x, AtlasUvs[_textureIndex[0]].y + AtlasUvs[_textureIndex[0]].height));
                }

                // Y-
                if (BlockTypes[_sides[1]].baseType == BaseType.air) {
                    int vertexIndex = _vertices.Count;
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength, z * _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength, z * _sideLength + _sideLength));

                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength, z * _sideLength + _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength, z * _sideLength));

                    _triangles.Add(vertexIndex);
                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex + 1);

                    _triangles.Add(vertexIndex + 3);
                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex);

                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[1]].x, AtlasUvs[_textureIndex[1]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[1]].x + AtlasUvs[_textureIndex[1]].width, AtlasUvs[_textureIndex[1]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[1]].x + AtlasUvs[_textureIndex[1]].width, AtlasUvs[_textureIndex[1]].y + AtlasUvs[_textureIndex[1]].height));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[1]].x, AtlasUvs[_textureIndex[1]].y + AtlasUvs[_textureIndex[1]].height));
                }

                // Z+
                if (BlockTypes[_sides[2]].baseType == BaseType.air) {
                    int vertexIndex = _vertices.Count;
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength, z * _sideLength + _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength, z * _sideLength + _sideLength));

                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength + _sideLength, z * _sideLength + _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength + _sideLength, z * _sideLength + _sideLength));

                    _triangles.Add(vertexIndex);
                    _triangles.Add(vertexIndex + 1);
                    _triangles.Add(vertexIndex + 2);

                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex + 3);
                    _triangles.Add(vertexIndex);

                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[2]].x, AtlasUvs[_textureIndex[2]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[2]].x + AtlasUvs[_textureIndex[2]].width, AtlasUvs[_textureIndex[2]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[2]].x + AtlasUvs[_textureIndex[2]].width, AtlasUvs[_textureIndex[2]].y + AtlasUvs[_textureIndex[2]].height));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[2]].x, AtlasUvs[_textureIndex[2]].y + AtlasUvs[_textureIndex[2]].height));
                }

                // Z-
                if (BlockTypes[_sides[3]].baseType == BaseType.air) {
                    int vertexIndex = _vertices.Count;
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength, z * _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength, z * _sideLength));

                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength + _sideLength, z * _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength + _sideLength, z * _sideLength));

                    _triangles.Add(vertexIndex);
                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex + 1);

                    _triangles.Add(vertexIndex + 3);
                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex);

                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[3]].x, AtlasUvs[_textureIndex[3]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[3]].x + AtlasUvs[_textureIndex[3]].width, AtlasUvs[_textureIndex[3]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[3]].x + AtlasUvs[_textureIndex[3]].width, AtlasUvs[_textureIndex[3]].y + AtlasUvs[_textureIndex[3]].height));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[3]].x, AtlasUvs[_textureIndex[3]].y + AtlasUvs[_textureIndex[3]].height));
                }

                // X+
                if (BlockTypes[_sides[4]].baseType == BaseType.air) {
                    int vertexIndex = _vertices.Count;
                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength, z * _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength + _sideLength, z * _sideLength));

                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength + _sideLength, z * _sideLength + _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength + _sideLength, y * _sideLength, z * _sideLength + _sideLength));

                    _triangles.Add(vertexIndex);
                    _triangles.Add(vertexIndex + 1);
                    _triangles.Add(vertexIndex + 2);

                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex + 3);
                    _triangles.Add(vertexIndex);

                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[4]].x, AtlasUvs[_textureIndex[4]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[4]].x + AtlasUvs[_textureIndex[4]].width, AtlasUvs[_textureIndex[4]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[4]].x + AtlasUvs[_textureIndex[4]].width, AtlasUvs[_textureIndex[4]].y + AtlasUvs[_textureIndex[4]].height));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[4]].x, AtlasUvs[_textureIndex[4]].y + AtlasUvs[_textureIndex[4]].height));
                }

                // X-
                if (BlockTypes[_sides[5]].baseType == BaseType.air) {
                    int vertexIndex = _vertices.Count;
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength, z * _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength + _sideLength, z * _sideLength));

                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength + _sideLength, z * _sideLength + _sideLength));
                    _vertices.Add(new Vector3(x * _sideLength, y * _sideLength, z * _sideLength + _sideLength));

                    _triangles.Add(vertexIndex);
                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex + 1);

                    _triangles.Add(vertexIndex + 3);
                    _triangles.Add(vertexIndex + 2);
                    _triangles.Add(vertexIndex);

                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[5]].x, AtlasUvs[_textureIndex[5]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[5]].x + AtlasUvs[_textureIndex[5]].width, AtlasUvs[_textureIndex[5]].y));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[5]].x + AtlasUvs[_textureIndex[5]].width, AtlasUvs[_textureIndex[5]].y + AtlasUvs[_textureIndex[5]].height));
                    _uv.Add(new Vector2(AtlasUvs[_textureIndex[5]].x, AtlasUvs[_textureIndex[5]].y + AtlasUvs[_textureIndex[5]].height));

                }
            }
            catch (Exception e) {
                SafeDebug.LogError(string.Format("Message: {0}, Info:", e.Message), e);
                string _UVs = string.Empty;
                for (int i = 0; i < _textureIndex.Length; i++) {
                    _UVs += _textureIndex[i] + " ";
                }
                SafeDebug.Log("texture indexes: " + _UVs);
                SafeDebug.Log("Blocktype: " + _type.ToString());
                if (AtlasUvs == null) {
                    SafeDebug.Log("AtlasUvs is null!");
                }
            }
        }
    }

    private bool IsEdge(int x, int y, int z) {
        return (x == ChunkSizeX - 1) || (y == ChunkSizeY - 1) || (z == ChunkSizeZ - 1);
    }

    private bool IsInBounds(int x, int y, int z) {
        return ((x <= ChunkSizeX - 1) && x >= 0) && ((y <= ChunkSizeY - 1) && y >= 0) && ((z <= ChunkSizeZ - 1) && z >= 0);
    }

    private void CalculateVariables() {
        ChunkSizeX = ChunkMeterSizeX * VoxelsPerMeter;
        ChunkSizeY = ChunkMeterSizeY * VoxelsPerMeter;
        ChunkSizeZ = ChunkMeterSizeZ * VoxelsPerMeter;
        half = ((1.0f / (float)VoxelsPerMeter) / 2.0f);
        AllocateBlockArray(ChunkSizeX, ChunkSizeY, ChunkSizeZ);
        SetSurroundingChunks();
        Initialized = true;
    }

    private void SetSurfaceData(Vector2Int bottomLeft, Vector2Int topRight) {
        try {
            for (int noiseX = bottomLeft.x - 1, x = 0; noiseX < topRight.x + 1; noiseX++, x++) {
                for (int noiseZ = bottomLeft.y - 1, z = 0; noiseZ < topRight.y + 1; noiseZ++, z++) {
                    SurfaceData[x, z] = GetHeight(noiseX, noiseZ);
                }
            }
        }
        catch (Exception e) {
            SafeDebug.LogError(e.Message + "\nFunction: SetSurfaceData", e);
        }
    }

    public void ResetPageNeighbors() {
        for (int i = 0; i < neighbors.Length; i++) {
            if (neighbors[i].controller != null)
                neighbors[i].controller.SetSurroundingChunks();
        }
    }

    public void SetSurroundingChunks() {
        neighbors = new Neighbor[6];
        Setpage(0, new Vector3Int(Location.x, Location.y + 1, Location.z));
        Setpage(1, new Vector3Int(Location.x, Location.y - 1, Location.z));
        Setpage(2, new Vector3Int(Location.x, Location.y, Location.z + 1));
        Setpage(3, new Vector3Int(Location.x, Location.y, Location.z - 1));
        Setpage(4, new Vector3Int(Location.x + 1, Location.y, Location.z));
        Setpage(5, new Vector3Int(Location.x - 1, Location.y, Location.z));
    }

    private void Setpage(int index, Vector3Int pageLoc) {
        if (controller.BuilderExists(pageLoc.x, pageLoc.y, pageLoc.z)) {
            IVoxelBuilder _builder = controller.GetBuilder(pageLoc.x, pageLoc.y, pageLoc.z);
            if (_builder != null) {
                neighbors[index] = new Neighbor(true, controller.BuilderGenerated(pageLoc.x, pageLoc.y, pageLoc.z), pageLoc, _builder);
            }
            else
            {
                SafeDebug.LogError("Controller says chunk builder exists, but chunk builder is null!.");
                neighbors[index] = new Neighbor(false, false, pageLoc, null);
            }
        }
        else {
            neighbors[index] = new Neighbor(false, false, pageLoc, null);
        }
    }

    private void AllocateBlockArray(int sizeX, int sizeY, int sizeZ) {
        blocks = new Block[sizeX][][];
        SurfaceData = new float[sizeX + 2, sizeZ + 2];
        for (int i = 0; i < sizeX; ++i) {
            blocks[i] = new Block[sizeY][];
            for (int j = 0; j < sizeY; ++j) {
                blocks[i][j] = new Block[sizeZ];
            }
        }
    }

    private float GetHeight(int x, int y)
    {
        if (NoiseModule != null)
            return (((float)NoiseModule.GetValue((double)x / 150, 0, (double)y / 150) * amp) + groundOffset);
        return 0;
    }

    public static int GetKeyFromVector(Vector3Int inVector) {
        return (100 * inVector.x) + (10 * inVector.y) + inVector.z;
    }

    public void Dispose()
    {
        deactivated = true;
        //blocks = null;
        //SurfaceData = null;
        //BlockTypes = null;
        //AtlasUvs = null;
        //neighbors = null;
        if (NoisePlane != null)
        {
            NoisePlane.Clear();
            NoisePlane.Dispose();
        }
        NoisePlane = null;
        NoiseModule = null;
        caveModule = null;
        
    }
}