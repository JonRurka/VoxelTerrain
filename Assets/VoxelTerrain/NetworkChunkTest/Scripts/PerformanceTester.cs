using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

public class PerformanceTester : MonoBehaviour
{
    public struct Result
    {
        public float iso;
        public uint type;

        public static int Size()
        {
            return sizeof(float) + sizeof(uint);
        }
    }
    ComputeShader shader;

    ComputeBuffer Min_Max;
    ComputeBuffer Count;
    ComputeBuffer Data;
    ComputeBuffer Raw;

    // Start is called before the first frame update
    void Start()
    {
        uint val = 0;

        uint loc = 52428;
        uint type_P2 = 255 << 16;
        uint iso_p2 = (uint)VoxelConversions.Scale(Mathf.Clamp(2, -2, 2), -2, 2, byte.MinValue, byte.MaxValue) << 24;

        val = loc;
        val |= type_P2;
        val |= iso_p2;



        byte[] us_b = System.BitConverter.GetBytes(val);

        Debug.Log(System.BitConverter.ToString(us_b));
    }

    void AppendBufferTest()
    {
        shader = (ComputeShader)Resources.Load("shaders/ChunkCompute");

        ComputeBuffer argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);



        int[] args = new int[] { 0, 1, 0, 0 };

        argBuffer.SetData(args);


        Min_Max = new ComputeBuffer(1, sizeof(float) * 2);
        Data = new ComputeBuffer(18688, sizeof(float) + sizeof(uint), ComputeBufferType.Append);
        Raw = new ComputeBuffer(18688, sizeof(float) + sizeof(uint));

        Data.SetCounterValue(0);

        int k = shader.FindKernel("CSMain");
        shader.SetBuffer(k, "Min_Max", Min_Max);
        shader.SetBuffer(k, "Data", Data);
        shader.SetBuffer(k, "Raw", Raw);


        Stopwatch watch = new Stopwatch();
        watch.Start();

        shader.Dispatch(k, 18688 / 16, 1, 1);

        Vector2[] min_max = new Vector2[1];

        
        Min_Max.GetData(min_max);

        Min_Max.GetNativeBufferPtr();
        


        ComputeBuffer.CopyCount(Data, argBuffer, 0);

        argBuffer.GetData(args);

        watch.Stop();

        Debug.LogFormat("Get Metadata: {0}: {1} - {2}", watch.Elapsed, min_max[0].x, min_max[0].y);
        Debug.Log("count: " + args[0]);

        Debug.Log("instance count: " + args[1]);

        Debug.Log("start count: " + args[2]);

        Debug.Log("start instance:  " + args[3]);

        watch.Restart();





        Result[] data = new Result[args[0]];
        Data.GetData(data);

        watch.Stop();

        Debug.LogFormat("Get Data: {0}: {1}", watch.Elapsed, Mathf.Max(0,0));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
