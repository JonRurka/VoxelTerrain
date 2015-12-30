using UnityEngine;
using System.Collections;

public class GrassSript : MonoBehaviour {
    public Transform currentCam;
    public Texture2D[] textures;
    public LayerMask mask;

    private Texture2D activeTexture;
    private GameObject player;
    bool _ySet = false;
	// Use this for initialization
	void Start () {
        activeTexture = textures[Random.Range(0, textures.Length)];
        GetComponent<SpriteRenderer>().material.SetTexture("_MainTex", activeTexture);
	}
	
	// Update is called once per frame
	void Update () {
        if (!_ySet)
        {
            Ray ray = new Ray(new Vector3(transform.position.x, transform.position.y + 500, transform.position.z), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000, mask))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                _ySet = true;
            }
        }
	}
}
