using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace UnityGameServer
{
    public class Region
    {
        public Vector2Int Location { get; private set; }
        public string RegionDirectory { get; private set; }

        public ConcurrentDictionary<Vector3Int, Column> Columns;
        public ConcurrentDictionary<Vector3Int, Column.LOD_Mode> LOD_Modes;


        public Region(string regionDir, Vector2Int location)
        {
            Location = location;
            RegionDirectory = regionDir.EndsWith(ServerBase.sepChar.ToString()) ? regionDir : regionDir + ServerBase.sepChar;
            Columns = new ConcurrentDictionary<Vector3Int, Column>();
            LOD_Modes = new ConcurrentDictionary<Vector3Int, Column.LOD_Mode>();

            
        }

        public void Init()
        {
            if (!Directory.Exists(RegionDirectory))
            {
                Logger.Log("Creating new Region: " + Location);
                Directory.CreateDirectory(RegionDirectory);
            }

            /*int x_start = Location.x * SmoothVoxelSettings.ChunksPerRegionX;
            int z_start = Location.y * SmoothVoxelSettings.ChunksPerRegionZ;
            for (int x = 0, x_global = x_start; x < SmoothVoxelSettings.ChunksPerRegionX; x++, x_global++)
            {
                for (int z = 0, z_global = z_start; z < SmoothVoxelSettings.ChunksPerRegionZ; z++, z_global++)
                {
                    LOD_Modes[new Vector3Int(x_global, 0, z_global)] = Column.LOD_Mode.Empty;
                }
            }*/
        }

        public bool ChunkExists(Vector3Int location)
        {
            return Directory.Exists(Column.GetColumnFile(RegionDirectory, location));
        }

        public bool ChunkLoaded(Vector3Int location)
        {
            return Columns.ContainsKey(location);
        }

        public bool ChunkFullyLoaded(Vector3Int location)
        {
            return Columns.ContainsKey(location) && Columns[location].FullyLoaded;
        }

        public Column LoadColumn(User requester, Vector3Int location, Column.LOD_Mode mode)
        {
            if (ChunkLoaded(location))
            {
                return Columns[location];
            }

            if (!ChunkExists(location))
                throw new Exception("Column must be created with Region.CreateColumn() before attempting to load chunk.");

            Column column = new Column(this, RegionDirectory, location);
            column.Init((float)SmoothVoxelSettings.voxelsPerMeter, SmoothVoxelSettings.MeterSizeX, SmoothVoxelSettings.MeterSizeY, SmoothVoxelSettings.MeterSizeZ);
            Column.LOD_Mode loaded_mode = column.Deserialize(mode);
            Columns[location] = column;

            return column;
        }

        public Column CreateColumn(User requester, Vector3Int location, Column.LOD_Mode mode)
        {
            if (ChunkLoaded(location))
            {
                return Columns[location];
            }

            if (ChunkExists(location))
            {
                return LoadColumn(requester, location, mode);
            }

            Column column = new Column(this, RegionDirectory, location);
            column.Init((float)SmoothVoxelSettings.voxelsPerMeter, SmoothVoxelSettings.MeterSizeX, SmoothVoxelSettings.MeterSizeY, SmoothVoxelSettings.MeterSizeZ);
            column.BuildChunk(mode);
            column.Serialize();

            Columns[location] = column;

            //Logger.Log("Column Size: " + column.GeneratedBlocks);
            //Logger.Log("Optimized Size: " + column.surfaceBlocksCount);

            return column;
        }

        public Column RegenerateColumn(User requester, Vector3Int location, Column.LOD_Mode mode)
        {
            if (!ChunkExists(location))
                throw new Exception("Chunk must exist in order to be regenerated.");

            Column column = null;
            if (ChunkLoaded(location))
            {
                column = Columns[location];
            }
            else
            {
                column = LoadColumn(requester, location, mode);
            }

            column.BuildChunk(mode);

            return column;
            
        }
    }
}
