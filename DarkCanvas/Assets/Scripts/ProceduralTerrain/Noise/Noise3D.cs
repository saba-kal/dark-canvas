using ProceduralToolkit.FastNoiseLib;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Class for generating 3D noise maps.
    /// </summary>
    public static class Noise3D
    {
        /// <summary>
        /// Generates a 3D noise map.
        /// </summary>
        public static int[,,] GenerateMap(
            int mapWidth,
            int mapHeight,
            int mapDepth,
            NoiseSettings noiseSettings,
            Vector3 sampleCenter)
        {
            var noiseMap = new int[mapWidth, mapHeight, mapDepth];

            var offsetX = noiseSettings.Offset.x + sampleCenter.x;
            var offsetY = noiseSettings.Offset.y + sampleCenter.y;
            var offsetZ = noiseSettings.Offset.z + sampleCenter.z;

            var fastNoise = new FastNoise(noiseSettings.Seed);
            fastNoise.SetFractalOctaves(noiseSettings.Octaves);
            fastNoise.SetFractalLacunarity(noiseSettings.Lacunarity);
            fastNoise.SetFractalGain(noiseSettings.Persistence);
            fastNoise.SetNoiseType(FastNoise.NoiseType.PerlinFractal);

            var minNoiseValue = float.MaxValue;
            var maxNoiseValue = float.MinValue;
            for (var z = 0; z < mapDepth; z++)
            {
                for (var y = 0; y < mapHeight; y++)
                {
                    for (var x = 0; x < mapWidth; x++)
                    {
                        var noiseValue = fastNoise.GetNoise(x + offsetX, y + offsetY, z + offsetZ);
                        if (noiseValue < minNoiseValue)
                        {
                            minNoiseValue = noiseValue;
                        }
                        if (noiseValue > maxNoiseValue)
                        {
                            maxNoiseValue = noiseValue;
                        }

                        noiseMap[x, y, z] = (int)(255 * noiseValue);
                    }
                }
            }

            return noiseMap;
        }
    }
}