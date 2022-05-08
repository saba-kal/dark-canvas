using DarkCanvas.Data.ProceduralTerrain;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public static class NoiseMapGenerator
    {

        public static NoiseMap3D GenerateNoiseMap3D(
            int width, int height, int depth, HeightMapSettings settings, Vector3 sampleCenter)
        {
            var values = Noise3D.GenerateMap(width, height, depth, settings.NoiseSettings, sampleCenter);

            return new NoiseMap3D
            {
                Values = values,
                MinValue = 0,
                MaxValue = 1
            };
        }
    }
}