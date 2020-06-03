using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityGameServer;
using UnityGameServer.Networking;
using LibNoise;
using Debug = UnityEngine.Debug;

public class VoxelServer : GameServer<VoxelServer>
{
    public Settings ServSettings { get; private set; }
    public RegionLoader Regions { get; private set; }

    public VoxelServer(string[] args) : base(args)
    {
        
    }

    // loads settings, called after constructor and before init.
    public override void LoadSettings(string file)
    {
        // don't call base.LoadSettings if you have your own settings class. instead
        // set BaseSettings to it.

        ServSettings = new Settings(file);
        BaseSettings = ServSettings;

        //Logger.Log("Load settings.");
    }

    // initializes objects, called after Load settings and before update.
    public override void Init()
    {
        base.Init();
        //Debug.Log("Init");

        //Debug.Log("Size: " + System.Runtime.InteropServices.Marshal.SizeOf(typeof(SaveStructure)));

        //Regions = new RegionLoader(System.IO.Directory.GetCurrentDirectory());
        //Region reg = Regions.CreateRegion(new Vector3Int(0, 0, 0));
        //reg.CreateColumn(new Vector3Int(0, 0, 0));
        //CommandInput.LoadCommands(this); // add to load custom console commands.
    }

    // called every frame.
    public override void Update()
    {
        base.Update();
    }

    // base simply sets Run to false, and stops the loop.
    public override void Stop()
    {
        base.Stop();
        Logger.Log("Stop");
    }

    // disposes all objects, stops threads in task queue, etc. Also sets Run to false.
    public override void Dispose()
    {
        base.Dispose();
        Logger.Log("Dispose"); // not printed to console or logged.
    }

    public override void UserConnected(SocketUser user)
    {
        Logger.Log("Voxel Server: User Connected! " + user.SessionToken);
    }

    public override void UserPinged(SocketUser user, Protocal protocal, bool pingBack)
    {
        //if (protocal == Protocal.Udp)
        //    Logger.Log("Received UDP ping for " + user.SessionToken);
    }

    public override void UserUdpEnabled(SocketUser user)
    {
        //Logger.Log("Voxel Server: UDP Enabled! " + user.SessionToken);
    }

    public ISampler GetSampler()
    {
        TerrainModule module = new TerrainModule(SmoothVoxelSettings.seed);
        ISampler result = new TerrainSampler(module,
                                    SmoothVoxelSettings.seed,
                                    SmoothVoxelSettings.enableCaves,
                                    1.5f,
                                    SmoothVoxelSettings.caveDensity,
                                    SmoothVoxelSettings.grassOffset);
        return result;
    }

    // A test network command. Can be called from both tcp and udp.
    [Command(ServerCodes.Identify)]
    public void Identify_CMD(SocketUser user, Data data)
    {
        User server_user = new User(data.Input);
        server_user.SetSocket(user);
        user.Send((byte)ClientCodes.Identified, "Welcome " + data.Input);
    }

    [Command(ServerCodes.GetChunk)]
    public void GetChunk_Cmd(SocketUser user, Data data)
    {
        int chunkX = BitConverter.ToInt32(data.Buffer, 0);
        int chunkY = BitConverter.ToInt32(data.Buffer, 4);
        int chunkZ = BitConverter.ToInt32(data.Buffer, 8);
        int lod_Version = BitConverter.ToInt32(data.Buffer, 12);

        Vector3Int chunkCord = new Vector3Int(chunkX, chunkY, chunkZ);
        Vector2Int Region = VoxelConversions.ChunkToRegion(chunkCord);

        Region region = Regions.LoadORCreate(Region);


    }
}
