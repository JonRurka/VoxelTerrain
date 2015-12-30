using UnityEngine;
using System.Collections;
using LibNoise;
using LibNoise.Models;
using LibNoise.Modifiers;

namespace LibNoise
{
    public class TerrainModule : IModule
    {
        private IModule _module;
        private int _seed;

        public TerrainModule(int seed)
        {
            _seed = seed;

            RidgedMultifractal mountains = new RidgedMultifractal();
            mountains.Seed = seed;
            mountains.Frequency = 0.5;

            Billow hills = new Billow();
            hills.Seed = seed;
            hills.Frequency = 2;

            ScaleBiasOutput scaleHill = new ScaleBiasOutput(hills);
            scaleHill.Scale = 0.04;
            scaleHill.Bias = 0;

            ScaleBiasOutput scaleMountain = new ScaleBiasOutput(mountains);
            scaleMountain.Scale = 1.5;

            Perlin selectorControl = new Perlin();
            selectorControl.Seed = seed;
            selectorControl.Frequency = 0.10;
            selectorControl.Persistence = 0.25;

            Select selector = new Select(selectorControl, scaleMountain, scaleHill);
            selector.SetBounds(0, 1000);
            selector.EdgeFalloff = 0.5;
            _module = selector;

        }

        public double GetValue(double x, double y, double z)
        {
            return _module.GetValue(x, 0, z) / 3.5;
        }

        public double GetValue(double x, double y)
        {
            return GetValue(x, 0, y);
        }
    }
}
