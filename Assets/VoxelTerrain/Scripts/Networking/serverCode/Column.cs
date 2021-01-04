using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;

namespace UnityGameServer
{
    public class Column
    {
        public enum UpdateMode
        {
            EmptyToHeightmap,
            EmptyToReduced,
            EmptyToFull,
            HeightmapToReduced,
            HeightmapToFull,
            ReducedToFull,
        }

        public Vector3Int Location { get; private set; }

        public string ColumnFile { get; private set; }
        public ISampler Sampler { get; private set; }

        public bool FullyLoaded { get; private set; }
        public Region Region { get; private set; }
        public LOD_Mode Current_Mode { get; private set; }
        public LOD_Mode Max_Mode { get; private set; }
        public int GeneratedBlocks { get; private set; }
        public bool ReduceDepth { get; set; }
        public float Depth { get; set; }
        public bool LoadedFromDisk { get; set; }

        public int Min {
            get
            {
                return col_data.Min;
            }
            private set
            {
                col_data.Min = value;
            }
        }
        public int Max {
            get
            {
                return col_data.Max;
            }
            private set
            {
                col_data.Max = value;
            }
        }

        public byte[] blocks_type {
            get
            {
                return col_data.blocks_type;
            }
            set
            {
                col_data.blocks_type = value;
            }
        }

        public float[] blocks_iso {
            get
            {
                return col_data.blocks_iso;
            }
            set
            {
                col_data.blocks_iso = value;
            }
        }

        public bool[] blocks_set {
            get
            {
                return col_data.blocks_set;
            }
            set
            {
                col_data.blocks_set = value;
            }
        }

        public bool[] blocks_surface {
            get
            {
                return col_data.blocks_surface;
            }
            set
            {
                col_data.blocks_surface = value;
            }
        }

        public uint[] surfaceBlocks {
            get
            {
                return col_data.surfaceBlocks;
            }
            set
            {
                col_data.surfaceBlocks = value;
            }
        }

        public int surfaceBlocksCount {
            get
            {
                return col_data.surfaceBlocksCount;
            }
            private set
            {
                col_data.surfaceBlocksCount = value;
            }
        }

        public bool Initialized;
        public bool SurfaceGenerated;
        public bool deactivated;

        public float[] SurfaceData { get { return col_data.SurfaceData; } }

        private IColumnBuilder builder;
        private ColumnResult col_data;

        private float VoxelsPerMeter;
        private int ChunkMeterSizeX;
        private int ChunkMeterSizeY;
        private int ChunkMeterSizeZ;
        private int ChunkSizeX;
        private int ChunkSizeY;
        private int ChunkSizeZ;
        private int skipDist;
        private float half;
        private float xSideLength;
        private float ySideLength;
        private float zSideLength;
        private bool empty;
        private static Vector3Int[] directionOffsets = new Vector3Int[8]
        {
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 1),
                new Vector3Int(1, 1, 1),
                new Vector3Int(1, 1, 0),
                new Vector3Int(0, 1, 0),
        };
        #region edgeTable
        private static int[] edgeTable = new int[256]
        {0x0 , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
        0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
        0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
        0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
        0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
        0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
        0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
        0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
        0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
        0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
        0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
        0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
        0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
        0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
        0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
        0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
        0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0};
        #endregion

        private Vector3[] locOffset;
        private Vector3Int[] globalOffsets;

        private UpdateMode updateMode;

        public Column(Region region, string folder, Vector3Int location)
        {
            Region = region;
            Location = location;
            ColumnFile = GetColumnFile(folder, location);
            Sampler = VoxelServer.Instance.GetSampler();
        }

        public void Init(float voxelsPerMeter, int chunkMeterSizeX, int chunkMeterSizeY, int chunkMeterSizeZ)
        {
            VoxelsPerMeter = voxelsPerMeter;
            ChunkMeterSizeX = chunkMeterSizeX;
            ChunkMeterSizeY = chunkMeterSizeY;
            ChunkMeterSizeZ = chunkMeterSizeZ;
            CalculateVariables();
            Sampler.SetChunkSettings(VoxelsPerMeter,
                                     new Vector3Int(ChunkSizeX, ChunkSizeY, ChunkSizeZ),
                                     new Vector3Int(ChunkMeterSizeX, ChunkMeterSizeY, ChunkMeterSizeZ),
                                     skipDist,
                                     half,
                                     new Vector3(xSideLength, ySideLength, zSideLength));

            col_data = new ColumnResult();
            col_data.SurfaceData = new float[(ChunkSizeX + 2) * (ChunkSizeZ + 2)];

            if (VoxelServer.Instance.Gpu_Acceloration)
            {
                GPU_ColumnBuilder _builder = new GPU_ColumnBuilder(col_data, Sampler);
                _builder.Init(Location, VoxelsPerMeter, ChunkMeterSizeX, ChunkMeterSizeY, ChunkMeterSizeZ);
                builder = _builder;
            }
            else
            {
                StandardColumnBuilder _builder = new StandardColumnBuilder(col_data, Sampler);
                _builder.Init(Location, VoxelsPerMeter, ChunkMeterSizeX, ChunkMeterSizeY, ChunkMeterSizeZ);
                builder = _builder;
            }
        }

        public void BuildChunk(LOD_Mode mode = LOD_Mode.Full)
        {
            Logger.Log("BuildChunk - mode: {0}, Current_Mode: {1}, Max_Mode:{2}", mode, Current_Mode, Max_Mode);
            if (mode <= Current_Mode)
                return;

            if (mode > Current_Mode && mode <= Max_Mode)
            {
                Deserialize(mode);
                return;
            }

            // mode must be greater than max
            if (Current_Mode == LOD_Mode.Empty)
            {
                switch (mode)
                {
                    case LOD_Mode.Heightmap:
                        updateMode = UpdateMode.EmptyToHeightmap;
                        break;

                    case LOD_Mode.ReducedDepth:
                        updateMode = UpdateMode.EmptyToReduced;
                        break;

                    case LOD_Mode.Full:
                        updateMode = UpdateMode.HeightmapToFull;
                        break;
                }
            }
            else if (Current_Mode == LOD_Mode.Heightmap)
            {
                switch(mode)
                {
                    case LOD_Mode.ReducedDepth:
                        updateMode = UpdateMode.HeightmapToReduced;
                        break;

                    case LOD_Mode.Full:
                        updateMode = UpdateMode.HeightmapToFull;
                        break;
                }
            }
            else if (Current_Mode == LOD_Mode.ReducedDepth)
            {
                updateMode = UpdateMode.ReducedToFull;
            }
            else
                return;


            Max_Mode = mode;
            Current_Mode = mode;




            GenerateHeightMap();

            if (Max_Mode == LOD_Mode.Heightmap)
                return;
            if (Max_Mode == LOD_Mode.ReducedDepth)
                ReduceDepth = true;

            Generate();

            if (Max_Mode == LOD_Mode.Full)
            {
                FullyLoaded = true;
            }
        }

        public  void GenerateHeightMap()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (!SurfaceGenerated)
            {
                UnityGameServer.Logger.Log("Heightmap Generting...");
                builder.GenerateHeightMap();
                SurfaceGenerated = true;
            }

            watch.Stop();
            UnityGameServer.Logger.Log("Min: {0}, Max: {1}", Sampler.GetMin(), Sampler.GetMax());
            UnityGameServer.Logger.Log("Heightmap Gen: " + watch.Elapsed);
        }

        public void Generate()
        {
            if (empty)
                return;

            if (!Initialized)
                return;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            //surfaceBlocksCount = 0;
            GeneratedBlocks = 0;

            int cx = Location.x;
            int cy = Location.y;
            int cz = Location.z;

            int xStart = cx * ChunkSizeX;
            int xEnd = cx * ChunkSizeX + ChunkSizeX;

            int yStart = cy * ChunkSizeY;
            int yEnd = cy * ChunkSizeY + ChunkSizeY;

            int zStart = cz * ChunkSizeZ;
            int zEnd = cz * ChunkSizeZ + ChunkSizeZ;

            int globalLocX = 0;
            int globalLocY = 0;
            int globalLocZ = 0;

            int x = 0;
            int y = 0;
            int z = 0;

            int Y_Min = 0;
            int Y_Max = ChunkSizeY;

            int heightmapMin = VoxelConversions.WorldToVoxel(new Vector3(0, Math.Max(0, (float)Sampler.GetMin() - Depth), 0)).y;
            Logger.Log("heightmapMin: {0}", heightmapMin);

            int heightmapMin_local = VoxelConversions.GlobalToLocalChunkCoord(new Vector3Int(0, heightmapMin, 0)).y;
            Logger.Log("heightmapMin_local: {0}", heightmapMin_local);

            int heightmapMax = VoxelConversions.WorldToVoxel(new Vector3(0, Math.Min(SmoothVoxelSettings.MeterSizeY, (float)Sampler.GetMax()), 0)).y;
            Logger.Log("heightmapMax: {0}", heightmapMax);

            int heightmapMax_local = VoxelConversions.GlobalToLocalChunkCoord(new Vector3Int(0, heightmapMax, 0)).y;
            Logger.Log("heightmapMax_local: {0}", heightmapMax_local);

            Logger.Log("Generating with updated mode {0}: {1}", updateMode.ToString(), DebugTimer.Elapsed());

            if (updateMode == UpdateMode.EmptyToHeightmap)
            {
                return;
            }
            else if (updateMode == UpdateMode.EmptyToReduced || updateMode == UpdateMode.EmptyToFull ||
                     updateMode == UpdateMode.HeightmapToReduced || updateMode == UpdateMode.HeightmapToFull)
            {
                //Min = int.MaxValue;
                //Max = int.MinValue;
                switch (updateMode)
                {
                    case UpdateMode.EmptyToReduced:
                    case UpdateMode.HeightmapToReduced:
                        Y_Min = heightmapMin_local;
                        yStart += Y_Min;
                        Y_Max = heightmapMax_local;
                        break;

                    case UpdateMode.EmptyToFull:
                    case UpdateMode.HeightmapToFull:
                        Y_Max = heightmapMax_local;
                        break;
                }
            }
            else if (updateMode == UpdateMode.ReducedToFull)
            {
                Y_Max = heightmapMin_local;
                Logger.Log("ReducedToFull: " + heightmapMin_local);
            }

            //else if(updateMode == UpdateMode.)

            //if (LoadedFromDisk && !ReduceDepth && Max_Mode == LOD_Mode.Full)
            //    Y_Max = VoxelConversions.GlobalToLocalChunkCoord(VoxelConversions.WorldToVoxel(new Vector3(0, (float)Sampler.GetMin() - Depth, 0))).y;

            UnityGameServer.Logger.Log("Column: Y_Min: {0}, Y_Max: {1}", Y_Min, Y_Max);

            col_data.Allocate(ChunkSizeX, ChunkSizeY, ChunkSizeZ);
            builder.Generate(Y_Min, Y_Max, xStart, zStart);


            /*for (globalLocY = yStart, y = Y_Min; y < Y_Max; globalLocY++, y++)
            {

                Max = Mathf.Max(y, Max);
                Min = Mathf.Min(y, Min);

                for (globalLocZ = zStart, z = 0; z < ChunkSizeZ; globalLocZ++, z++)
                {
                    for (globalLocX = xStart, x = 0; x < ChunkSizeX; globalLocX++, x++)
                    {
                        if (deactivated)
                        {
                            break;
                        }

                        Vector3 worldPos = new Vector3(x * xSideLength, y * ySideLength, z * zSideLength);
                        Vector3Int globalPos = new Vector3Int(globalLocX * skipDist, globalLocY * skipDist, globalLocZ * skipDist);
                        Vector3Int localPos = new Vector3Int(x, y, z);
                        GridPoint[] grid = new GridPoint[8];
                        for (int i = 0; i < grid.Length; i++)
                            grid[i] = GetGridPoint(worldPos + locOffset[i], localPos + directionOffsets[i], globalPos + globalOffsets[i]);
                        ProcessBlock(grid, 0);
                        GeneratedBlocks++;
                    }
                }
            }*/

            watch.Stop();
            Logger.Log("Generated from {0} to {1}: " + DebugTimer.Elapsed(), Y_Min, Y_Max);
            Logger.Log("Generated surface blocks: {0}", col_data.surfaceBlocksCount);
        }

        public void MarkAsSet(int x, int y, int z)
        {
            if (!deactivated)
                blocks_set[Get_Flat_Index(x, y, z)] = true;
        }

        public void SetBlock(int x, int y, int z, Block block)
        {
            if (!deactivated)
            {
                int index = Get_Flat_Index(x, y, z);
                blocks_type[index] = (byte)block.type;
                blocks_iso[index] = block.iso;
            }
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (!deactivated)
            {
                //if (IsInBounds(x, y, z)) {
                x = Mathf.Clamp(x, 0, ChunkSizeX);
                y = Mathf.Clamp(y, 0, ChunkSizeY);
                z = Mathf.Clamp(z, 0, ChunkSizeZ);

                int index = Get_Flat_Index(x, y, z);

                Block res = new Block(blocks_type[index], blocks_iso[index]);
                res.set = blocks_set[index];

                return res;
            }
            return default(Block);
        }

        public int Get_Flat_Index(int x, int y, int z)
        {
            return x + ChunkSizeX * (y + ChunkSizeY * z);
        }

        public void SetBlockValue(int x, int y, int z, float value)
        {
            if (!deactivated)
            {
                blocks_iso[Get_Flat_Index(x, y, z)] = value;
            }
        }

        public float GetBlockValue(int x, int y, int z)
        {
            if (!deactivated)
            {
                return blocks_iso[Get_Flat_Index(x, y, z)];
            }
            return 0;
        }

        public bool IsBlockSet(int x, int y, int z)
        {
            if (!deactivated)
                return blocks_set[Get_Flat_Index(x, y, z)];
            return false;
        }

        public bool IsInBounds(int x, int y, int z)
        {
            return ((x < ChunkSizeX) && x >= 0) && ((y < ChunkSizeY) && y >= 0) && ((z < ChunkSizeZ) && z >= 0);
        }

        public void Serialize()
        {
            return;
            FileStream stream = new FileStream(ColumnFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.Default, false);

            writer.Write((int)Max_Mode);
            writer.Write(Min);
            writer.Write(Max);
            writer.Write(ReduceDepth);
            writer.Write(Depth);

            // Save heightmap
            byte[] buff = new byte[SurfaceData.Length * 4];
            Buffer.BlockCopy(SurfaceData, 0, buff, 0, buff.Length);
            writer.Write(buff);

            // Save surface blocks

            buff = new byte[surfaceBlocksCount * 4];
            Buffer.BlockCopy(surfaceBlocks, 0, buff, 0, buff.Length);
            writer.Write(surfaceBlocksCount);
            writer.Write(buff);

            int start = Get_Flat_Index(0, Math.Max(Min - 1, 0), 0);
            int length = ((Max + 1) - (Min - 1)) * ChunkSizeX * ChunkSizeZ;

            // Save all block types
            writer.Write(blocks_type, start, length);

            // Save all block ISO
            buff = new byte[length * 4];
            Buffer.BlockCopy(blocks_iso, start, buff, 0, buff.Length);
            writer.Write(buff);


            writer.Close();
            writer.Dispose();

            Logger.Log("Serialized from {0} to {1}: {2}", Min, Max, DebugTimer.Elapsed());
        }

        public LOD_Mode Deserialize(LOD_Mode load_mode)
        {
            if (!File.Exists(ColumnFile))
                return LOD_Mode.Empty;

            FileStream stream = new FileStream(ColumnFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.Default);

            LoadedFromDisk = true;

            Max_Mode = (LOD_Mode)reader.ReadInt32();
            Current_Mode = (LOD_Mode)Math.Min((int)load_mode, (int)Max_Mode);
            Min = reader.ReadInt32();
            Max = reader.ReadInt32();
            ReduceDepth = reader.ReadBoolean();
            Depth = reader.ReadSingle();

            // Load heightmap
            byte[] buff = new byte[SurfaceData.Length * 4];
            reader.Read(buff, 0, buff.Length);
            Buffer.BlockCopy(buff, 0, SurfaceData, 0, buff.Length);
            Sampler.SetSurfaceData(SurfaceData);
            SurfaceGenerated = true;

            if (Max_Mode <= LOD_Mode.Heightmap || Current_Mode <= LOD_Mode.Heightmap)
            {
                reader.Close();
                return LOD_Mode.Heightmap;
            }

            col_data.Allocate(ChunkSizeX, ChunkSizeY, ChunkSizeZ);

            // Load surface blocks
            surfaceBlocksCount = reader.ReadInt32();
            buff = new byte[surfaceBlocksCount * 4];
            reader.Read(buff, 0, buff.Length);
            Buffer.BlockCopy(buff, 0, surfaceBlocks, 0, buff.Length);


            Logger.Log("Deserializing from {0} to {1}.", Min, Max);
            int start = Get_Flat_Index(0, Math.Max(Min - 1, 0), 0);
            int length = ((Max + 1) - (Min - 1)) * ChunkSizeX * ChunkSizeZ;

            // Load all block types
            reader.Read(blocks_type, start, length);

            // Load all block ISO
            buff = new byte[length * 4];
            reader.Read(buff, 0, buff.Length);
            Buffer.BlockCopy(buff, 0, blocks_iso, start, buff.Length);


            reader.Close();

            FullyLoaded = true;
            return (LOD_Mode)Math.Min((int)Current_Mode, (int)Max_Mode);
        }

        public static string GetColumnFile(string folder, Vector3Int location)
        {
            folder = folder.EndsWith(ServerBase.sepChar.ToString()) ? folder : folder + ServerBase.sepChar; ;
            return folder + "column_" + location.File_String() + ".clm";
        }

        private void CalculateVariables()
        {
            Current_Mode = LOD_Mode.Empty;
            Max_Mode = LOD_Mode.Empty;
            Depth = 10;
            ChunkSizeX = (int)(ChunkMeterSizeX * VoxelsPerMeter);
            ChunkSizeY = (int)(ChunkMeterSizeY * VoxelsPerMeter);
            ChunkSizeZ = (int)(ChunkMeterSizeZ * VoxelsPerMeter);
            half = ((1.0f / (float)VoxelsPerMeter) / 2.0f);
            xSideLength = ChunkMeterSizeX / (float)ChunkSizeX;
            ySideLength = ChunkMeterSizeY / (float)ChunkSizeY;
            zSideLength = ChunkMeterSizeZ / (float)ChunkSizeZ;
            skipDist = Mathf.RoundToInt(1 / (float)VoxelsPerMeter);
            locOffset = new Vector3[8]
            {
            new Vector3(0,0,zSideLength),
            new Vector3(xSideLength,0,zSideLength),
            new Vector3(xSideLength,0,0),
            new Vector3(0,0,0),
            new Vector3(0,ySideLength,zSideLength),
            new Vector3(xSideLength,ySideLength,zSideLength),
            new Vector3(xSideLength,ySideLength,0),
            new Vector3(0,ySideLength,0),
            };
            globalOffsets = new Vector3Int[8]
            {
            new Vector3Int(0, 0, 1) * skipDist,
            new Vector3Int(1, 0, 1) * skipDist,
            new Vector3Int(1, 0, 0) * skipDist,
            new Vector3Int(0, 0, 0) * skipDist,
            new Vector3Int(0, 1, 1) * skipDist,
            new Vector3Int(1, 1, 1) * skipDist,
            new Vector3Int(1, 1, 0) * skipDist,
            new Vector3Int(0, 1, 0) * skipDist,
            };
            //AllocateBlockArray(ChunkSizeX, ChunkSizeY, ChunkSizeZ);
            Initialized = true;
        }



        
    }
}
