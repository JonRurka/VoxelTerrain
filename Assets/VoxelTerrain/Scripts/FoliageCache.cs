using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageCache : MonoBehaviour
{
    public class Plant
    {


        public Vector3[] GetVertices(float y)
        {
            return new Vector3[0];
        }

        public int[] GetTriangles(int offset)
        {
            return new int[0];
        }
    }

    public class PlantBlock
    {

    }

    public int perBlockX = 4;
    public int perBlockZ = 4;


    public PlantBlock[,] plantBlocks;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
