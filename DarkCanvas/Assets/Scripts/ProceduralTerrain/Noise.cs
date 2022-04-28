using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public enum NormalizeMode
    {
        Local, Global
    }

    public static class Noise
    {
        public static float[,] GenerateMap(
            int mapWidth,
            int mapHeight,
            int seed,
            float scale,
            int octaves,
            float persistance,
            float lacunarity,
            Vector2 offset,
            NormalizeMode normalizeMode)
        {
            var noiseMap = new float[mapWidth, mapHeight];
            var random = new System.Random(seed);

            var amplitude = 1f;
            var maxPossibleHeight = 0f;
            var octaveOffsets = new Vector2[octaves];
            for (var i = 0; i < octaves; i++)
            {
                var offsetX = random.Next(-100000, 100000) + offset.x;
                var offsetY = random.Next(-100000, 100000) - offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);

                maxPossibleHeight += amplitude;
                amplitude *= persistance;
            }

            if (scale == 0)
            {
                scale = 0.001f;
            }

            var minNoiseHeight = float.MaxValue;
            var maxNoiseHeight = float.MinValue;

            for (var y = 0; y < mapHeight; y++)
            {
                for (var x = 0; x < mapWidth; x++)
                {
                    var noiseHeight = GetNoiseValue(x, y, mapWidth, mapHeight, scale, persistance, lacunarity, octaveOffsets);

                    if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }

                    noiseMap[x, y] = noiseHeight;
                }
            }

            NormalizeNoiseMap(noiseMap, minNoiseHeight, maxNoiseHeight, maxPossibleHeight, normalizeMode);

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

        private static void NormalizeNoiseMap(
            float[,] noiseMap,
            float minNoiseHeight,
            float maxNoiseHeight,
            float maxPossibleHeight,
            NormalizeMode normalizeMode)
        {
            for (var y = 0; y < noiseMap.GetLength(1); y++)
            {
                for (var x = 0; x < noiseMap.GetLength(0); x++)
                {
                    if (normalizeMode == NormalizeMode.Local)
                    {
                        noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                    }
                    else
                    {
                        var normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight;
                        noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                    }
                }
            }
        }
    }
}

