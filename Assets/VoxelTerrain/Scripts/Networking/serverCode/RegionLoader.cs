using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UnityGameServer
{
    public class RegionLoader
    {
        private ConcurrentDictionary<Vector2Int, Region> loadedRegions;

        public string RootRegionDirectory { get; private set; }

        public RegionLoader(string directory)
        {
            loadedRegions = new ConcurrentDictionary<Vector2Int, Region>();
            RootRegionDirectory = directory.EndsWith(ServerBase.sepChar.ToString()) ? directory : directory + ServerBase.sepChar;

            Logger.Log("Region Folder: " + RootRegionDirectory);
        }

        public bool RegionExists(Vector2Int pos)
        {
            return Directory.Exists(GetRegionFolder(pos));
        }

        public bool RegionLoaded(Vector2Int pos)
        {
            return loadedRegions.ContainsKey(pos);
        }
        
        public string GetRegionFolder(Vector2Int pos)
        {
            return RootRegionDirectory + "r_" + pos.File_String() + ServerBase.sepChar;
        }

        public Region LoadORCreate(Vector2Int pos)
        {
            if (RegionLoaded(pos))
                return loadedRegions[pos];

            if (RegionExists(pos))
                return Load(pos);

            return CreateRegion(pos);
        }

        public Region CreateRegion(Vector2Int pos)
        {
            if (RegionLoaded(pos))
                return loadedRegions[pos];

            if (RegionExists(pos))
                return Load(pos);

            Region res = new Region(GetRegionFolder(pos), pos);
            res.Init();
            loadedRegions[pos] = res;
            return res;
        }

        public Region Load(Vector2Int pos)
        {
            if (RegionExists(pos))
            {
                Region res = new Region(GetRegionFolder(pos), pos);
                res.Init();
                loadedRegions[pos] = res;
                return res;
            }
            throw new Exception("Region must be created before it can be loaded.");
        }

        public void MarshalTest()
        {
            UnityEngine.Debug.Log("MarshalTest");
            SaveStructure testStruct = new SaveStructure();

            testStruct.location_x = 7;
            testStruct.location_y = 2;
            testStruct.location_z = 3;

            testStruct.c0_min = 2;
            testStruct.c0_max = 3;

            testStruct.heightmap = new float[20 * 20];
            testStruct.heightmap[testStruct.heightmap.Length - 1] = 123;

            testStruct.blocks_type_c0 = new byte[20 * 20 * 128];
            testStruct.blocks_iso_c0 = new float[20 * 20 * 128];
            testStruct.blocks_type_c0[testStruct.blocks_type_c0.Length - 1] = 5;
            testStruct.blocks_iso_c0[testStruct.blocks_iso_c0.Length - 1] = 4.44f;

            testStruct.blocks_type_c1 = new byte[20 * 20 * 128];
            testStruct.blocks_iso_c1 = new float[20 * 20 * 128];
            testStruct.blocks_type_c1[testStruct.blocks_type_c1.Length - 1] = 8;
            testStruct.blocks_iso_c1[testStruct.blocks_iso_c1.Length - 1] = 33.3f;

            //MemoryStream stream = new MemoryStream();

            FileStream stream = new FileStream(@"D:\Users\jon\Documents\test_data.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            /*testStruct.blocks_c0 = new SaveBlock[20 * 20 * 128];
            testStruct.blocks_c0[0] = new SaveBlock(5, 4.44f);

            testStruct.blocks_c1 = new SaveBlock[20 * 20 * 128];
            testStruct.blocks_c1[0] = new SaveBlock(4, 3.33f);*/

            Stopwatch fullwatch = new Stopwatch();
            fullwatch.Start();

            Stopwatch watch1 = new Stopwatch();
            watch1.Start();


            testStruct.Serialize(stream);

            UnityEngine.Debug.Log("stream length: " + stream.Length);

            /*IntPtr pnt = Marshal.AllocHGlobal(size);
            byte[] buf = new byte[size];

            Marshal.StructureToPtr(testStruct, pnt, false);
            Marshal.Copy(pnt, buf, 0, buf.Length);*/

            watch1.Stop();

            /*byte[] tmp = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(tmp, 0, tmp.Length);
            Logger.Log(BitConverter.ToString(tmp));
            stream.Close();
            stream.Dispose();
            stream = new MemoryStream(tmp);*/
            //stream.Seek(0, SeekOrigin.Begin);
            stream.Close();
            stream = new FileStream(@"D:\Users\jon\Documents\test_data.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            Stopwatch watch2 = new Stopwatch();
            watch2.Start();

            testStruct = new SaveStructure();
            testStruct.Deserialize(stream);
            stream.Close();

            /*IntPtr pnt2 = Marshal.AllocHGlobal(Marshal.SizeOf(testStruct));
            Marshal.Copy(buf, 0, pnt2, buf.Length);
            testStruct = (SaveStructure)Marshal.PtrToStructure(pnt2, typeof(SaveStructure));*/

            watch2.Stop();
            fullwatch.Stop();

            Logger.Log("Structure To Byte: " + watch1.Elapsed);
            Logger.Log("Byte To Structure: " + watch2.Elapsed);
            Logger.Log("Full Test: " + fullwatch.Elapsed);

            Logger.Log("Marshal location_x: " + testStruct.location_x);
            Logger.Log("Marshal location_y: " + testStruct.location_y);
            Logger.Log("Marshal location_z: " + testStruct.location_z);

            Logger.Log("Marshal min: " + testStruct.c0_min);
            Logger.Log("Marshal max: " + testStruct.c0_max);

            Logger.Log("Marshal heightmap[0]: " + testStruct.heightmap[testStruct.heightmap.Length - 1]);
            Logger.Log("Marshal blocks_type_c0[0]: " + testStruct.blocks_type_c0[testStruct.blocks_type_c0.Length - 1]);
            Logger.Log("Marshal blocks_iso_c0[0]: " + testStruct.blocks_iso_c0[testStruct.blocks_iso_c0.Length - 1]);
            Logger.Log("Marshal blocks_type_c1[0]: " + testStruct.blocks_type_c1[testStruct.blocks_type_c1.Length - 1]);
            Logger.Log("Marshal blocks_iso_c1[0]: " + testStruct.blocks_iso_c1[testStruct.blocks_iso_c1.Length - 1]);
            /*Logger.Log("Marshal test: " + testStruct.blocks_c0[1].type);
            Logger.Log("Marshal test: " + testStruct.blocks_c1[1].iso);*/
        }
    }
}
