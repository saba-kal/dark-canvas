using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace DarkCanvas.Assets.Scripts.ProceduralTerrain
{
    /// <summary>
    /// Class for generating a single 240x240 terrain chunk.
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        /// <summary>
        /// Type of map to generate.
        /// </summary>
        public enum DrawMode
        {
            NoiseMap, //Show a simple black and white texture of our noise function.
            ColorMap, //Show a color texture of our various terrain elevations.
            Mesh, //Generate the mesh associated with our noise function.
            FalloffMap //Generates a black and white texture with pixels closest to the edge being black.
        }

        /// <summary>
        /// Map chunk size will contain 239 vertices on each side.
        /// Was originally 241 vertices so that each side was 240 units long.
        /// 240 is easy to work with when generating different levels of details of the mesh
        /// because it is divisible by many numbers (2, 4, 6, 8, 10, and 12).
        /// Was shrunk to 239 vertices so that we could smoothly stitch together
        /// multiple map chunks by predicting the next maps normals.
        /// </summary>
        public const int MAP_CHUNK_SIZE = 239;

        public bool AutoUpdate = false;

        [SerializeField] private DrawMode _drawMode;
        [SerializeField] private NormalizeMode _normalizeMode;
        [Range(0, 6)]
        [SerializeField] private int _previewLevelOfDetail;
        [SerializeField] private float _noiseScale;
        [SerializeField] private int _octaves;
        [Range(0, 1)]
        [SerializeField] private float _persistance;
        [SerializeField] private float _lacunarity;
        [SerializeField] private int _seed;
        [SerializeField] private Vector2 _offset;
        [SerializeField] private bool _useFalloff;
        [SerializeField] private float _meshHeightMultiplier;
        [SerializeField] private AnimationCurve _meshHeightCurve;
        [SerializeField] private TerrainType[] _regions;
        [SerializeField] private MapDisplay _mapDisplay;

        private float[,] _fallOffMap;
        private ConcurrentQueue<MapThreadInfo<MapData>> _mapThreadInfoQueue =
            new ConcurrentQueue<MapThreadInfo<MapData>>();
        private ConcurrentQueue<MapThreadInfo<MeshData>> _meshThreadInfoQueue =
            new ConcurrentQueue<MapThreadInfo<MeshData>>();

        private void OnValidate()
        {
            if (_lacunarity < 1)
            {
                _lacunarity = 1;
            }
            if (_octaves < 0)
            {
                _octaves = 0;
            }

            _fallOffMap = FalloffGenerator.GenerateFalloffMap(MAP_CHUNK_SIZE);
        }

        private void Start()
        {
            _fallOffMap = FalloffGenerator.GenerateFalloffMap(MAP_CHUNK_SIZE);
        }

        private void Update()
        {
            if (_mapThreadInfoQueue.Count > 0)
            {
                for (var i = 0; i < _mapThreadInfoQueue.Count; i++)
                {
                    if (_mapThreadInfoQueue.TryDequeue(out var mapThreadInfo))
                    {
                        mapThreadInfo.Callback(mapThreadInfo.Parameter);
                    }
                }
            }

            if (_meshThreadInfoQueue.Count > 0)
            {
                for (var i = 0; i < _meshThreadInfoQueue.Count; i++)
                {
                    if (_meshThreadInfoQueue.TryDequeue(out var mapThreadInfo))
                    {
                        mapThreadInfo.Callback(mapThreadInfo.Parameter);
                    }
                }
            }
        }

        public void DrawMapInEditor()
        {
            var mapData = GenerateMapData(Vector2.zero);

            switch (_drawMode)
            {
                case DrawMode.NoiseMap:
                    _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
                    break;
                case DrawMode.ColorMap:
                    _mapDisplay.DrawTexture(
                        TextureGenerator.TextureFromColourMap(mapData.ColorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
                    break;
                case DrawMode.Mesh:
                    _mapDisplay.DrawMesh(
                        MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, _meshHeightMultiplier, _meshHeightCurve, _previewLevelOfDetail),
                        TextureGenerator.TextureFromColourMap(mapData.ColorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
                    break;
                case DrawMode.FalloffMap:
                    _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(
                        FalloffGenerator.GenerateFalloffMap(MAP_CHUNK_SIZE)));
                    break;
            }
        }

        public void RequestMapData(Vector2 center, Action<MapData> callback)
        {
            ThreadStart threadStart = delegate
            {
                MapDataThread(center, callback);
            };

            new Thread(threadStart).Start();
        }

        public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback)
        {
            ThreadStart threadStart = delegate
            {
                MeshDataThread(mapData, levelOfDetail, callback);
            };

            new Thread(threadStart).Start();
        }

        private void MapDataThread(Vector2 center, Action<MapData> callback)
        {
            var mapData = GenerateMapData(center);
            _mapThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }

        private void MeshDataThread(MapData mapData, int levelOfDetail, Action<MeshData> callback)
        {
            var meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, _meshHeightMultiplier, _meshHeightCurve, levelOfDetail);
            _meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }

        private MapData GenerateMapData(Vector2 center)
        {
            var noiseMap = Noise.GenerateMap(
                MAP_CHUNK_SIZE + 2,
                MAP_CHUNK_SIZE + 2,
                _seed,
                _noiseScale,
                _octaves,
                _persistance,
                _lacunarity,
                center +
                _offset,
                _normalizeMode);

            var colorMap = new Color[MAP_CHUNK_SIZE * MAP_CHUNK_SIZE];
            for (var y = 0; y < MAP_CHUNK_SIZE; y++)
            {
                for (var x = 0; x < MAP_CHUNK_SIZE; x++)
                {
                    if (_useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _fallOffMap[x, y]);
                    }

                    var currentHeight = noiseMap[x, y];
                    for (var i = 0; i < _regions.Length; i++)
                    {
                        if (currentHeight >= _regions[i].Height)
                        {
                            colorMap[y * MAP_CHUNK_SIZE + x] = _regions[i].Color;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return new MapData(noiseMap, colorMap);
        }
    }

    public struct MapData
    {
        public float[,] HeightMap { get; private set; }
        public Color[] ColorMap { get; private set; }

        public MapData(float[,] heightMap, Color[] colorMap)
        {
            HeightMap = heightMap;
            ColorMap = colorMap;
        }
    }
}