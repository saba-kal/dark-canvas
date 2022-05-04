using DarkCanvas.Data.ProceduralTerrain;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Generates a height map for a single terrain chunk.
    /// </summary>
    public static class HeightMapGenerator
    {
        /// <summary>
        /// Generates a height map for a single terrain chunk.
        /// </summary>
        /// <param name="width">Width of the map.</param>
        /// <param name="height">Height of the map.</param>
        /// <param name="settings">Holds parameters for generating height maps.</param>
        /// <param name="sampleCenter">The world position of the height map. Does not account for uniform scale.</param>
        /// <returns>2D array of values that represent the height map.</returns>
        public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
        {
            //Animation curves do not work well if accessed from multiple threads. Creating a copy avoids this issue.
            var threadSafeHeightCurve = new AnimationCurve(settings.HeightCurve.keys);
            var values = Noise.GenerateMap(width, height, settings.NoiseSettings, sampleCenter);

            var minValue = float.MaxValue;
            var maxValue = float.MinValue;
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < width; j++)
                {
                    values[i, j] *= threadSafeHeightCurve.Evaluate(values[i, j]) * settings.HeightMultiplier;
                    if (values[i, j] > maxValue)
                    {
                        maxValue = values[i, j];
                    }
                    if (values[i, j] < minValue)
                    {
                        minValue = values[i, j];
                    }
                }
            }

            return new HeightMap
            {
                Values = values,
                MinValue = minValue,
                MaxValue = maxValue
            };
        }
    }
}