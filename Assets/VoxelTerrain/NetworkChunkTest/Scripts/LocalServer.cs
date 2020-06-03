using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalServer : MonoBehaviour
{
    VoxelServer server;

    public System.Action OnServerInitialized;

    // Start is called before the first frame update
    void Awake()
    {
        server = new VoxelServer(new string[0]);
        
    }

    // Update is called once per frame
    void Update()
    {
        server.Update();
    }

    public void Init()
    {
        Debug.Log("Starting server...");
        server.Init();
        server.Server.OnAcceptingClients += CanAcceptClients;
        server.Server.StartListen();
    }

    public void CanAcceptClients()
    {
        OnServerInitialized();
    }
}
