using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameClient
{
    NetClient client;

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

    private void Client_OnConnectSuccess()
    {
        Debug.Log("Client_OnConnectSuccess");
        client.UdpConnect();
        client.Send((byte)ServerCodes.Identify, "nug700");
    }

    [ClientCommand(ClientCodes.Identified)]
    private void Identified_cmd(Data data)
    {
        Debug.Log(data.Input);
    }
}
