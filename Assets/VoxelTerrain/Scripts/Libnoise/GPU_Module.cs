using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibNoise
{
    public class GPU_Module : IModule
    {
        ComputeShader shader;
        ComputeBuffer buffer;

        public GPU_Module()
        {
            shader = (ComputeShader)Resources.Load("shaders/TerrainModule");
            
        }

        public void ExecuteHeightMap()
        {

        }

        public double GetValue(double x, double y, double z)
        {
            throw new NotImplementedException();
        }
    }
}
