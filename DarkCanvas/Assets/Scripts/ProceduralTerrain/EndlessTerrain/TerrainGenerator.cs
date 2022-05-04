using DarkCanvas.Data.ProceduralTerrain;
using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Generates endless number of terrain chunks based on viewer's position.
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] private Transform _viewer;
        [SerializeField] private float _viewerMoveThresholdForChunkUpdate = 25f;
        [SerializeField] private float _colliderGenerationDistanceThreshold = 5f;
        [SerializeField] private MeshSettings _meshSettings;
        [SerializeField] private HeightMapSettings _heightMapSettings;
        [SerializeField] private TextureSettings _textureSettings;
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private int _colliderLODIndex;
        [SerializeField] private LevelOfDetailInfo[] _detailLevels;

        private float _meshWorldSize;
        private int _chunksVisibleInViewDistance;
        private List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();
        private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary =
            new Dictionary<Vector2, TerrainChunk>();
        private float _sqrViewerMoveThresholdForChunkUpdate;
        private Vector2 _viewerPosition;
        private Vector2 _viewerPositionOld;


        private void Start()
        {
            _sqrViewerMoveThresholdForChunkUpdate = Mathf.Pow(_viewerMoveThresholdForChunkUpdate, 2);
            _meshWorldSize = _meshSettings.MeshWorldSize;
            var maxViewDistance = _detailLevels[_detailLevels.Length - 1].VisibleDistanceThreshold;
            _chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _meshWorldSize);
            _textureSettings.ApplyToMaterial(_terrainMaterial);
            _textureSettings.UpdateMeshHeights(_terrainMaterial, _heightMapSettings.MinHeight, _heightMapSettings.MaxHeight);
            UpdateVisibleChunks();
        }

        private void Update()
        {
            _viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);

            if (_viewerPosition != _viewerPositionOld)
            {
                foreach (var terrainChunk in _visibleTerrainChunks)
                {
                    terrainChunk.UpdateCollisionMesh();
                }
            }

            if ((_viewerPositionOld - _viewerPosition).sqrMagnitude >
                _sqrViewerMoveThresholdForChunkUpdate)
            {
                _viewerPositionOld = _viewerPosition;
                UpdateVisibleChunks();
            }
        }

        private void UpdateVisibleChunks()
        {
            var alreadyUpdatedChunkCoords = new HashSet<Vector2>();
            //Decrement backwards in case list size changes.
            for (var i = _visibleTerrainChunks.Count - 1; i >= 0; i--)
            {
                alreadyUpdatedChunkCoords.Add(_visibleTerrainChunks[i].Coordinates);
                _visibleTerrainChunks[i].UpdateChunk();
            }

            var currentChunkCoordX = Mathf.RoundToInt(_viewerPosition.x / _meshWorldSize);
            var currentChunkCoordY = Mathf.RoundToInt(_viewerPosition.y / _meshWorldSize);

            for (var yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++)
            {
                for (var xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++)
                {
                    var viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                    if (alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                    {
                        continue;
                    }

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
                        terrainChunk.OnVisibilityChanged += OnTerrainChunkVisiblityChanged;
                        terrainChunk.Load();
                    }
                }
            }
        }

        private void OnTerrainChunkVisiblityChanged(TerrainChunk terrainChunk, bool visible)
        {
            if (terrainChunk.IsVisible())
            {
                _visibleTerrainChunks.Add(terrainChunk);
            }
            else
            {
                _visibleTerrainChunks.Remove(terrainChunk);
            }
        }

        private TerrainChunk BuildTerrainChunk(Vector2 coordinates)
        {
            return new TerrainChunk(
                new TerrainChunkParams
                {
                    Coordinates = coordinates,
                    DetailLevels = _detailLevels,
                    Parent = transform,
                    Material = _terrainMaterial,
                    ColliderLODIndex = _colliderLODIndex,
                    ColliderGenerationDistanceThreshold = _colliderGenerationDistanceThreshold,
                    Viewer = _viewer,
                    MeshSettings = _meshSettings,
                    HeightMapSettings = _heightMapSettings
                });
        }
    }
}