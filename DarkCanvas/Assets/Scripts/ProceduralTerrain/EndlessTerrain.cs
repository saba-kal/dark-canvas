using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.Assets.Scripts.ProceduralTerrain
{
    public class EndlessTerrain : MonoBehaviour
    {
        public const float SCALE = 1.5f;

        public static Vector2 ViewerPosition;
        public static float MaxViewDistance = 500;
        public static List<TerrainChunk> TerrainChunksVisibleLastUpdate = new List<TerrainChunk>();

        [SerializeField] private Transform _viewer;
        [SerializeField] private float _viewerMoveThresholdForChunkUpdate = 25f;
        [SerializeField] private MapGenerator _mapGenerator;
        [SerializeField] private Material _mapMaterial;
        [SerializeField] private LevelOfDetailInfo[] _detailLevels;

        private int _chunkSize;
        private int _chunksVisibleInViewDistance;
        private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary =
            new Dictionary<Vector2, TerrainChunk>();
        private float _sqrViewerMoveThresholdForChunkUpdate;
        private Vector2 _viewerPositionOld;

        // Use this for initialization
        private void Start()
        {
            _sqrViewerMoveThresholdForChunkUpdate = Mathf.Pow(_viewerMoveThresholdForChunkUpdate, 2);
            MaxViewDistance = _detailLevels[_detailLevels.Length - 1].VisibileDistanceThreshold;
            _chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
            _chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance / _chunkSize);
            UpdateVisibleChunks();
        }

        private void Update()
        {
            ViewerPosition = new Vector2(_viewer.position.x, _viewer.position.z) / SCALE;

            if ((_viewerPositionOld - ViewerPosition).sqrMagnitude >
                _sqrViewerMoveThresholdForChunkUpdate)
            {
                _viewerPositionOld = ViewerPosition;
                UpdateVisibleChunks();
            }
        }

        private void UpdateVisibleChunks()
        {
            foreach (var terrainChunk in TerrainChunksVisibleLastUpdate)
            {
                terrainChunk.SetVisible(false);
            }
            TerrainChunksVisibleLastUpdate = new List<TerrainChunk>();

            var currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / _chunkSize);
            var currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / _chunkSize);

            for (var yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++)
            {
                for (var xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++)
                {
                    var viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                    if (_terrainChunkDictionary.TryGetValue(viewedChunkCoord, out var terrainChunk))
                    {
                        terrainChunk.Update();
                    }
                    else
                    {
                        _terrainChunkDictionary.Add(
                            viewedChunkCoord,
                            new TerrainChunk(
                                viewedChunkCoord,
                                _chunkSize,
                                _detailLevels,
                                transform,
                                _mapGenerator,
                                _mapMaterial));
                    }
                }
            }
        }
    }

    [Serializable]
    public struct LevelOfDetailInfo
    {
        public int LevelOfDetail;
        public float VisibileDistanceThreshold;
    }
}