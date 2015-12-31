using UnityEngine;
using System.Collections;

public class GameControl : MonoBehaviour {
    public GameObject playerPrefab;
    public bool playSpawned = false;

    private GameObject playerObj;


	// Use this for initialization
	void Start () {
        TerrainController.Instance.OnRenderComplete += SpawnPlayer;
        TerrainController.Instance.init();
	}

    // Update is called once per frame
    void Update () {
	
	}

    void SpawnPlayer() {
        Debug.Log("spawning player.");
        if (!playSpawned) {
            playerObj = (GameObject)Instantiate(playerPrefab, new Vector3(50, 150, 0), Quaternion.identity);
            TerrainController.Instance.player = playerObj;
            playSpawned = true;
        }
    }
}
