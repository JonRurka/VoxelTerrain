using UnityEngine;
using System.Collections;
using LibNoise;
using LibNoise.Models;
using LibNoise.Modifiers;

namespace LibNoise
{
    public class TerrainModule : IModule
    {
        private RidgedMultifractal mountains;
        private ScaleInput inputScaledMountains;
        private ScaleBiasOutput scaleMountain;
        private ScaleOutput scaleSelector;

        private IModule _module;
        private int _seed;

        public TerrainModule(int seed)
        {
            _seed = seed;

            Perlin perlin_mountains = new Perlin();
            perlin_mountains.Seed = seed;
            perlin_mountains.Frequency = 0.5;
            perlin_mountains.NoiseQuality = NoiseQuality.High;

            //ScaleBiasOutput scalePerlinMountain = new ScaleBiasOutput(perlin_mountains);
            //scalePerlinMountain.Scale = 1;



            mountains = new RidgedMultifractal();
            mountains.Seed = 0;// seed;
            mountains.Frequency = 0.5;
            mountains.Lacunarity = 2;
            mountains.NoiseQuality = NoiseQuality.High;

            

            //scaleMountain = new ScaleBiasOutput(mountains);
            //scaleMountain.Scale = 0.5;

            //Add blendMountains = new Add(perlin_mountains, scaleMountain);

            double scale = 2;
            //inputScaledMountains = new ScaleInput(perlin_mountains, scale, 1, scale);



            /*Billow hills = new Billow();
            hills.Seed = seed;
            hills.Frequency = 2;

            ScaleBiasOutput scaleHill = new ScaleBiasOutput(hills);
            scaleHill.Scale = 0.04;
            scaleHill.Bias = 0;*/

            

            //Perlin selectorControl = new Perlin();
            //selectorControl.Seed = seed;
            //selectorControl.Frequency = 0.10;
            //selectorControl.Persistence = 0.25;

            //Select selector = new Select(selectorControl, scaleMountain, scaleHill);
            //selector.SetBounds(0, 1000);
            //selector.EdgeFalloff = 0.5;

            //scaleSelector = new ScaleOutput(inputScaledMountains, SmoothVoxelSettings.amplitude);
            _module = perlin_mountains;//new BiasOutput(scaleSelector, SmoothVoxelSettings.groundOffset);
        }

        public double GetValue(double x, double y, double z)
        {

            return _module.GetValue(x, 0, z);// / 3.5;
        }

        public double GetValue(double x, double y)
        {
            return GetValue(x, 0, y);
        }
    }
}
