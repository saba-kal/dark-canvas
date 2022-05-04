using DarkCanvas.Data.ProceduralTerrain;
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
        public int MapChunkSize => UseFlatShading ?
            MeshGenerator.SupportedFlatShadedChunkSizes[_flatShadedChunkSizeIndex] - 1 :
            MeshGenerator.SupportedChunkSizes[_chunkSizeIndex] - 1;

        public bool AutoUpdate => _autoUpdate;
        public bool UseFlatShading => _terrainData.UseFlatShading;
        public float UniformScale => _terrainData.UniformScale;

        [SerializeField] private bool _autoUpdate = false;
        [SerializeField] private MapDrawMode _drawMode;
        [Range(0, MeshGenerator.NUMBER_OF_SUPPORTED_CHUNK_SIZES - 1)]
        [SerializeField] private int _chunkSizeIndex;
        [Range(0, MeshGenerator.NUMBER_OF_SUPPORTED_FLAT_SHADED_CHUNK_SIZES - 1)]
        [SerializeField] private int _flatShadedChunkSizeIndex;
        [Range(0, MeshGenerator.NUMBER_OF_SUPPORTED_LODS - 1)]
        [SerializeField] private int _previewLevelOfDetail;

        [SerializeField] private NoiseData _noiseData;
        [SerializeField] private Data.ProceduralTerrain.TerrainData _terrainData;
        [SerializeField] private TextureData _textureData;
        [SerializeField] private Material _terrainMaterial;

        [SerializeField] private MapDisplay _mapDisplay;

        private float[,] _fallOffMap;
        private ConcurrentQueue<MapThreadInfo<HeightMap>> _mapThreadInfoQueue =
            new ConcurrentQueue<MapThreadInfo<HeightMap>>();
        private ConcurrentQueue<MapThreadInfo<MeshData>> _meshThreadInfoQueue =
            new ConcurrentQueue<MapThreadInfo<MeshData>>();

        private void Awake()
        {
            _textureData.ApplyToMaterial(_terrainMaterial);
        }

        private void OnValidate()
        {
            if (_terrainData != null)
            {
                _terrainData.OnValuesUpdated -= OnValuesUpdated;
                _terrainData.OnValuesUpdated += OnValuesUpdated;
            }
            if (_noiseData != null)
            {
                _noiseData.OnValuesUpdated -= OnValuesUpdated;
                _noiseData.OnValuesUpdated += OnValuesUpdated;
            }
            if (_textureData != null)
            {
                _textureData.OnValuesUpdated -= OnTextureValuesUpdated;
                _textureData.OnValuesUpdated += OnTextureValuesUpdated;
            }
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
            _textureData.UpdateMeshHeights(
                _terrainMaterial,
                _terrainData.MinHeight,
                _terrainData.MaxHeight);

            var heightMap = GenerateMapData(Vector2.zero);
            switch (_drawMode)
            {
                case MapDrawMode.NoiseMap:
                    _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.Values));
                    break;
                case MapDrawMode.Mesh:
                    _mapDisplay.DrawMesh(
                        MeshGenerator.GenerateTerrainMesh(
                            new MeshGeneratorParams
                            {
                                HeightMap = heightMap.Values,
                                HeightMultiplier = _terrainData.MeshHeightMultiplier,
                                HeightCurve = _terrainData.MeshHeightCurve,
                                LevelOfDetail = _previewLevelOfDetail,
                                UseFlatShading = _terrainData.UseFlatShading
                            }));
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
        public void RequestMapData(Vector2 center, Action<HeightMap> callback)
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
        /// <param name="heightMap">Holds the height and color maps of the mesh.</param>
        /// <param name="levelOfDetail">Complexity level of the mesh.</param>
        /// <param name="callback">Callback function for when mesh data is returned.</param>
        public void RequestMeshData(HeightMap heightMap, int levelOfDetail, Action<MeshData> callback)
        {
            ThreadStart threadStart = delegate
            {
                MeshDataThread(heightMap, levelOfDetail, callback);
            };

            new Thread(threadStart).Start();
        }

        private void MapDataThread(Vector2 center, Action<HeightMap> callback)
        {
            var mapData = GenerateMapData(center);
            _mapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, mapData));
        }

        private void MeshDataThread(HeightMap mapData, int levelOfDetail, Action<MeshData> callback)
        {
            var meshData = MeshGenerator.GenerateTerrainMesh(
                new MeshGeneratorParams
                {
                    HeightMap = mapData.Values,
                    HeightMultiplier = _terrainData.MeshHeightMultiplier,
                    HeightCurve = _terrainData.MeshHeightCurve,
                    LevelOfDetail = levelOfDetail,
                    UseFlatShading = _terrainData.UseFlatShading
                });
            _meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }

        private HeightMap GenerateMapData(Vector2 center)
        {
            //Extend the size of chunks beyond the edges by 1 vertex so that
            //we can smoothly shade across multiple terrain chunks.
            var bleededMapChinkSize = MapChunkSize + 2;

            var noiseMap = Noise.GenerateMap(
                bleededMapChinkSize,
                bleededMapChinkSize,
                _noiseData.Seed,
                _noiseData.NoiseScale,
                _noiseData.Octaves,
                _noiseData.Persistence,
                _noiseData.Lacunarity,
                center + _noiseData.Offset,
                _noiseData.NormalizeMode);

            if (_terrainData.UseFalloff)
            {
                _fallOffMap ??= FalloffGenerator.GenerateFalloffMap(bleededMapChinkSize);

                for (var y = 0; y < bleededMapChinkSize; y++)
                {
                    for (var x = 0; x < bleededMapChinkSize; x++)
                    {
                        if (_terrainData.UseFalloff)
                        {
                            noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _fallOffMap[x, y]);
                        }
                    }
                }
            }

            return new HeightMap
            {
                Values = noiseMap
            };
        }

        private void OnValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                DrawMapInEditor();
            }
        }

        private void OnTextureValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                _textureData.ApplyToMaterial(_terrainMaterial);
            }
        }
    }
}