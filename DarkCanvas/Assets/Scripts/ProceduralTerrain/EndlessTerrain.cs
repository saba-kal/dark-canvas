using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Generates endless number of terrain chunks based on viewer's position.
    /// </summary>
    public class EndlessTerrain : MonoBehaviour
    {
        public static Vector2 ViewerPosition;
        public static float MaxViewDistance = 500;

        [SerializeField] private float _scale = 1.5f;
        [SerializeField] private Transform _viewer;
        [SerializeField] private float _viewerMoveThresholdForChunkUpdate = 25f;
        [SerializeField] private MapGenerator _mapGenerator;
        [SerializeField] private Material _mapMaterial;
        [SerializeField] private LevelOfDetailInfo[] _detailLevels;

        private int _chunkSize;
        private int _chunksVisibleInViewDistance;
        private List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
        private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary =
            new Dictionary<Vector2, TerrainChunk>();
        private float _sqrViewerMoveThresholdForChunkUpdate;
        private Vector2 _viewerPositionOld;

        private void Start()
        {
            _sqrViewerMoveThresholdForChunkUpdate = Mathf.Pow(_viewerMoveThresholdForChunkUpdate, 2);
            MaxViewDistance = _detailLevels[_detailLevels.Length - 1].VisibileDistanceThreshold;
            _chunkSize = MapGenerator.MapChunkSize - 1;
            _chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance / _chunkSize);
            UpdateVisibleChunks();
        }

        private void Update()
        {
            ViewerPosition = new Vector2(_viewer.position.x, _viewer.position.z) / _scale;

            if ((_viewerPositionOld - ViewerPosition).sqrMagnitude >
                _sqrViewerMoveThresholdForChunkUpdate)
            {
                _viewerPositionOld = ViewerPosition;
                UpdateVisibleChunks();
            }
        }

        private void UpdateVisibleChunks()
        {
            HideAllVisibleTerrainChunks();

            var currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / _chunkSize);
            var currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / _chunkSize);

            for (var yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++)
            {
                for (var xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++)
                {
                    var viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                    if (_terrainChunkDictionary.TryGetValue(viewedChunkCoord, out var terrainChunk))
                    {
                        //Terrain chunk was previously visited.
                        terrainChunk.UpdateChunk();
                    }
                    else
                    {
                        //Terrain chunk does not exist. Generate a new one.
                        terrainChunk = BuildTerrainChunk(viewedChunkCoord);
                        _terrainChunkDictionary.Add(viewedChunkCoord, terrainChunk);
                    }

                    _terrainChunksVisibleLastUpdate.Add(terrainChunk);
                }
            }
        }

        private void HideAllVisibleTerrainChunks()
        {
            foreach (var terrainChunk in _terrainChunksVisibleLastUpdate)
            {
                terrainChunk.SetVisible(false);
            }
            _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
        }

        private TerrainChunk BuildTerrainChunk(Vector2 coordinates)
        {
            return new TerrainChunk(
                new TerrainChunkParams
                {
                    Coordinates = coordinates,
                    Size = _chunkSize,
                    DetailLevels = _detailLevels,
                    Parent = transform,
                    MapGenerator = _mapGenerator,
                    Material = _mapMaterial,
                    GlobalTerrainScale = _scale
                });
        }
    }

    [Serializable]
    public struct LevelOfDetailInfo
    {
        public int LevelOfDetail;
        public float VisibileDistanceThreshold;
        public bool UseForCollider;
    }
}