using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public static class TextureGenerator
    {
        public static Texture2D TextureFromColourMap(Color[] colorMap, int width, int height)
        {
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colorMap);
            texture.Apply();
            return texture;
        }

        public static Texture2D TextureFromHeightMap(HeightMap heightMap)
        {
            var width = heightMap.Values.GetLength(0);
            var height = heightMap.Values.GetLength(1);

            var colorMap = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    colorMap[y * width + x] = Color.Lerp(
                        Color.black,
                        Color.white,
                        Mathf.InverseLerp(heightMap.MinValue, heightMap.MaxValue, heightMap.Values[x, y]));
                }
            }

            return TextureFromColourMap(colorMap, width, height);
        }

        public static Texture2D TextureFromNoiseMap(NoiseMap3D noiseMap)
        {
            var width = noiseMap.Values.GetLength(0);
            var height = noiseMap.Values.GetLength(1);
            var depth = noiseMap.Values.GetLength(2);

            var colorMap = new Color[width * depth];
            for (var z = 0; z < depth; z++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        colorMap[y * width + x] = Color.Lerp(
                            Color.black,
                            Color.white,
                            Mathf.InverseLerp(noiseMap.MinValue, noiseMap.MaxValue, noiseMap.Values[x, y, z]));
                    }
                }
            }

            return TextureFromColourMap(colorMap, width, height);
        }
    }
}