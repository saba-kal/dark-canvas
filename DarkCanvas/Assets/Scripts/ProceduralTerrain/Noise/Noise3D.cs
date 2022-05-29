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
        public static float[,,] GenerateMap(
            int mapWidth,
            int mapHeight,
            int mapDepth,
            int scale,
            NoiseSettings noiseSettings,
            Vector3 sampleCenter)
        {
            var noiseMap = new float[mapWidth, mapHeight, mapDepth];

            var offsetX = noiseSettings.Offset.x + sampleCenter.x;
            var offsetY = noiseSettings.Offset.y + sampleCenter.y;
            var offsetZ = noiseSettings.Offset.z + sampleCenter.z;

            var fastNoise = new FastNoise(noiseSettings.Seed);
            fastNoise.SetFractalOctaves(noiseSettings.Octaves);
            fastNoise.SetFractalLacunarity(noiseSettings.Lacunarity);
            fastNoise.SetFractalGain(noiseSettings.Persistence);
            fastNoise.SetNoiseType(FastNoise.NoiseType.PerlinFractal);


            for (var x = 0; x < mapWidth; x++)
            {
                for (var y = 0; y < mapHeight; y++)
                {
                    for (var z = 0; z < mapDepth; z++)
                    {
                        var noiseLocation = new Vector3(
                            (x + offsetX) * scale * noiseSettings.Scale,
                            (y + offsetY) * scale * noiseSettings.Scale,
                            (z + offsetZ) * scale * noiseSettings.Scale);

                        var noiseValue = fastNoise.GetNoise(
                            noiseLocation.x,
                            noiseLocation.y,
                            noiseLocation.z);
                        var noise2D = fastNoise.GetNoise(
                            noiseLocation.x,
                            noiseLocation.z);

                        //if (noiseLocation.y > noise2D * 100)
                        //{
                        //    noiseValue = -Mathf.Abs(noiseValue);
                        //}

                        //noiseMap[x, y, z] = (sbyte)Mathf.Clamp(-noiseValue * 500, -128, 127);
                        var noiseInt = (int)(noiseValue * 255);
                        noiseMap[x, y, z] = noiseValue;
                    }
                }
            }

            return noiseMap;
        }
    }
}