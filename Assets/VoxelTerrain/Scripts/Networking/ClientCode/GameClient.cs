using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class GameClient
{
    NetClient client;

    public event System.Action<ColumnPacket> OnColumnReceived;
    public event System.Action OnIdentified;

    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

    public GameClient()
    {
        client = new NetClient(NetClient.HostType.local, "localhost");
        client.OnConnectSuccess += Client_OnConnectSuccess;
        client.AddCommands(this);
    }

    public void Connect()
    {
        client.Start();
    }

    public void Update()
    {
        client.Update();
    }

    public void RequestColumn(Vector2Int col, LOD_Mode mode, bool has_heightmap)
    {
        DebugTimer.Start();

        BinaryWriter wrt = GetStream(20);
        wrt.Write(col.x);
        wrt.Write(0);
        wrt.Write(col.y);

        wrt.Write((int)mode);

        wrt.Write(has_heightmap);

        client.Send((byte)ServerCodes.RequestChunk, GetBytes(wrt.BaseStream));
        wrt.Close();
    }

    private void Client_OnConnectSuccess()
    {
        Debug.Log("Client_OnConnectSuccess");
        client.UdpConnect();
        client.Send((byte)ServerCodes.Identify, "nug700");
    }

    private BinaryWriter GetStream(int capacity)
    {
        return new BinaryWriter(new MemoryStream(capacity));
    }

    private byte[] GetBytes(Stream strm)
    {
        return ((MemoryStream)strm).ToArray();
    }

    [ClientCommand(ClientCodes.Identified)]
    private void Identified_cmd(Data data)
    {
        Debug.Log(data.Input);
        OnIdentified();
    }

    [ClientCommand(ClientCodes.ReceiveChunk)]
    private void ReceiveChunk_cmd(Data data)
    {
        DebugTimer.Stop();
        SafeDebug.Log(string.Format("Received chunk: {0}, Time: {1}.", data.Buffer.Length, DebugTimer.Elapsed()));
    }
}
