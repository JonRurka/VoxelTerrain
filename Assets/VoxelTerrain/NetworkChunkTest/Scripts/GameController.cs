using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public LocalServer Server;
    public NetworkChunkController ChunkController;

    // Start is called before the first frame update
    void Start()
    {
        DebugTimer.Init();
        Application.runInBackground = true;
        Debug.Log("Run in background: " + Application.runInBackground);

        Server.OnServerInitialized += ServerInited;
        Server.Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ServerInited()
    {
        Debug.Log("Server initialized.");
        ChunkController.Init();
    }
}
