using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public enum NormalizeMode
    {
        Local, Global
    }

    /// <summary>
    /// Class that generates noise maps for procedural terrains.
    /// </summary>
    public static class Noise
    {
        /// <summary>
        /// Generate a noise map of values between 0 and 1.
        /// </summary>
        /// <param name="mapWidth">Width of the map.</param>
        /// <param name="mapHeight">Height of the map.</param>
        /// <param name="noiseSettings">Parameters for controlling how the noise is generated.</param>
        /// <param name="sampleCenter">The world position of the noise map. Does not account for uniform scale.</param>
        /// <returns>2D array of values that represent the noise map. Values are between 0 and 1.</returns>
        public static float[,] GenerateMap(
            int mapWidth,
            int mapHeight,
            NoiseSettings noiseSettings,
            Vector2 sampleCenter)
        {
            var noiseMap = new float[mapWidth, mapHeight];
            var random = new System.Random(noiseSettings.Seed);

            var amplitude = 1f;
            var maxPossibleHeight = 0f;
            var octaveOffsets = new Vector2[noiseSettings.Octaves];
            for (var i = 0; i < noiseSettings.Octaves; i++)
            {
                var offsetX = random.Next(-100000, 100000) + noiseSettings.Offset.x + sampleCenter.x;
                var offsetY = random.Next(-100000, 100000) - noiseSettings.Offset.y - sampleCenter.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);

                maxPossibleHeight += amplitude;
                amplitude *= noiseSettings.Persistence;
            }

            var minNoiseHeight = float.MaxValue;
            var maxNoiseHeight = float.MinValue;

            for (var y = 0; y < mapHeight; y++)
            {
                for (var x = 0; x < mapWidth; x++)
                {
                    var noiseHeight = GetNoiseValue(x, y, mapWidth, mapHeight,
                        noiseSettings.Scale, noiseSettings.Persistence, noiseSettings.Lacunarity, octaveOffsets);

                    if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }
                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }

                    noiseMap[x, y] = noiseHeight;

                    if (noiseSettings.NormalizeMode == NormalizeMode.Global)
                    {
                        //Global normalization is calculated with a bit of estimation
                        //since we do not know the max height across multiple terrain chunks.
                        var normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight;
                        noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                    }
                }
            }

            if (noiseSettings.NormalizeMode == NormalizeMode.Local)
            {
                NormalizeNoiseMapLocally(noiseMap, minNoiseHeight, maxNoiseHeight);
            }

            return noiseMap;
        }

        private static float GetNoiseValue(
            int x, int y,
            int mapWidth,
            int mapHeight,
            float scale,
            float persistance,
            float lacunarity,
            Vector2[] octaveOffsets)
        {
            var amplitude = 1f;
            var frequency = 1f;
            var noiseHeight = 0f;

            for (var i = 0; i < octaveOffsets.Length; i++)
            {
                var sampleX = (x - mapWidth / 2f + octaveOffsets[i].x) / scale * frequency;
                var sampleY = (y - mapHeight / 2f + octaveOffsets[i].y) / scale * frequency;

                var perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }

            return noiseHeight;
        }

        private static void NormalizeNoiseMapLocally(
            float[,] noiseMap,
            float minNoiseHeight,
            float maxNoiseHeight)
        {
            for (var y = 0; y < noiseMap.GetLength(1); y++)
            {
                for (var x = 0; x < noiseMap.GetLength(0); x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }
        }
    }
}

