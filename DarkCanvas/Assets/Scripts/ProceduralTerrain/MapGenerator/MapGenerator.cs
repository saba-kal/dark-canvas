using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Class for generating a single 240x240 terrain chunk.
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        /// <summary>
        /// Map chunk size will contain 239 vertices on each side.
        /// Was originally 241 vertices so that each side was 240 units long.
        /// 240 is easy to work with when generating different levels of details of the mesh
        /// because it is divisible by many numbers (2, 4, 6, 8, 10, and 12).
        /// Was shrunk to 239 vertices so that we could smoothly stitch together
        /// multiple map chunks by predicting the next maps normals.
        /// </summary>
        private const int SMOOTH_SHADING_MAP_CHUNK_SIZE = 239;

        /// <summary>
        /// Flat shading uses significantly more vertices.
        /// Therefore, the chunk size must be smaller so that we don't run into
        /// Unity's mesh size limit.
        /// </summary>
        private const int FLAT_SHADING_MAP_CHUNK_SIZE = 95;

        public static int MapChunkSize
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MapGenerator>();
                }

                return _instance.UseFlatShading ?
                    FLAT_SHADING_MAP_CHUNK_SIZE :
                    SMOOTH_SHADING_MAP_CHUNK_SIZE;
            }
        }

        public bool AutoUpdate { get => _autoUpdate; }
        public bool UseFlatShading { get => _useFlatShading; }

        [SerializeField] private bool _autoUpdate = false;
        [SerializeField] private MapDrawMode _drawMode;
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
        [SerializeField] private bool _useFlatShading;
        [SerializeField] private AnimationCurve _meshHeightCurve;
        [SerializeField] private TerrainType[] _regions;
        [SerializeField] private MapDisplay _mapDisplay;

        private float[,] _fallOffMap;
        private ConcurrentQueue<MapThreadInfo<MapData>> _mapThreadInfoQueue =
            new ConcurrentQueue<MapThreadInfo<MapData>>();
        private ConcurrentQueue<MapThreadInfo<MeshData>> _meshThreadInfoQueue =
            new ConcurrentQueue<MapThreadInfo<MeshData>>();
        private static MapGenerator _instance;

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

            _fallOffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
        }

        private void Start()
        {
            _fallOffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
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

        /// <summary>
        /// Generates the map in the Unity editor.
        /// </summary>
        public void DrawMapInEditor()
        {
            var mapData = GenerateMapData(Vector2.zero);

            switch (_drawMode)
            {
                case MapDrawMode.NoiseMap:
                    _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
                    break;
                case MapDrawMode.ColorMap:
                    _mapDisplay.DrawTexture(
                        TextureGenerator.TextureFromColourMap(mapData.ColorMap, MapChunkSize, MapChunkSize));
                    break;
                case MapDrawMode.Mesh:
                    _mapDisplay.DrawMesh(
                        MeshGenerator.GenerateTerrainMesh(
                            new MeshGeneratorParams
                            {
                                HeightMap = mapData.HeightMap,
                                HeightMultiplier = _meshHeightMultiplier,
                                HeightCurve = _meshHeightCurve,
                                LevelOfDetail = _previewLevelOfDetail,
                                UseFlatShading = _useFlatShading
                            }),
                        TextureGenerator.TextureFromColourMap(mapData.ColorMap, MapChunkSize, MapChunkSize));
                    break;
                case MapDrawMode.FalloffMap:
                    _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(
                        FalloffGenerator.GenerateFalloffMap(MapChunkSize)));
                    break;
            }
        }

        /// <summary>
        /// Makes an asynchronous request for new map data.
        /// </summary>
        /// <param name="center">Center of the terrain chunk this map will represent.</param>
        /// <param name="callback">Callback function for when map data is returned.</param>
        public void RequestMapData(Vector2 center, Action<MapData> callback)
        {
            ThreadStart threadStart = delegate
            {
                MapDataThread(center, callback);
            };

            new Thread(threadStart).Start();
        }

        /// <summary>
        /// Makes an asynchronous request for new mesh data.
        /// </summary>
        /// <param name="mapData">Holds the height and color maps of the mesh.</param>
        /// <param name="levelOfDetail">Complexity level of the mesh.</param>
        /// <param name="callback">Callback function for when mesh data is returned.</param>
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
            var meshData = MeshGenerator.GenerateTerrainMesh(
                new MeshGeneratorParams
                {
                    HeightMap = mapData.HeightMap,
                    HeightMultiplier = _meshHeightMultiplier,
                    HeightCurve = _meshHeightCurve,
                    LevelOfDetail = levelOfDetail,
                    UseFlatShading = _useFlatShading
                });
            _meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }

        private MapData GenerateMapData(Vector2 center)
        {
            var noiseMap = Noise.GenerateMap(
                MapChunkSize + 2,
                MapChunkSize + 2,
                _seed,
                _noiseScale,
                _octaves,
                _persistance,
                _lacunarity,
                center + _offset,
                _normalizeMode);

            var colorMap = new Color[MapChunkSize * MapChunkSize];
            for (var y = 0; y < MapChunkSize; y++)
            {
                for (var x = 0; x < MapChunkSize; x++)
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
                            colorMap[y * MapChunkSize + x] = _regions[i].Color;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return new MapData
            {
                HeightMap = noiseMap,
                ColorMap = colorMap
            };
        }
    }
}