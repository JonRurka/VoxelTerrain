using LibNoise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibNoise.Modifiers
{
    public class Interpolate : IModule
    {
        /// <summary>
        /// The first module from which to retrieve noise to be blended.
        /// </summary>
        public IModule SourceModule1 { get; set; }
        /// <summary>
        /// The second module from which to retrieve noise to be blended.
        /// </summary>
        public IModule SourceModule2 { get; set; }

        public double T { get; set; }

        public Interpolate(IModule sourceModule1, IModule sourceModule2, double t)
        {
            SourceModule1 = sourceModule1;
            SourceModule2 = sourceModule2;
            T = t;
        }

        public double GetValue(double x, double y, double z)
        {
            return Mathf.Lerp((float)SourceModule1.GetValue(x, y, z), (float)SourceModule2.GetValue(x, y, z), (float)T);
        }
    }
}
