using DarkCanvas.Data.ProceduralTerrain;
using System;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents a single square terrain chunk in the endless terrain.
    /// The size of the chunk is roughly 240x240 units.
    /// </summary>
    public class TerrainChunk
    {
        public event Action<TerrainChunk, bool> OnVisibilityChanged;
        public Vector2 Coordinates { get; private set; }

        private readonly GameObject _meshObject;
        private readonly Vector2 _sampleCenter;
        private readonly Bounds _bounds;
        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;
        private readonly LevelOfDetailInfo[] _detailLevels;
        private readonly LevelOfDetailMesh[] _levelOfDetailMeshes;
        private readonly int _colliderLODIndex;
        private readonly float _colliderGenerationDistanceThreshold;
        private readonly MeshSettings _meshSettings;
        private readonly HeightMapSettings _heightMapSettings;
        private readonly Transform _viewer;
        private readonly float _maxViewDistance;

        private HeightMap _heightMap;
        private bool _heightMapRecieved;
        private int _previousLevelOfDetailIndex = -1;
        private bool _hasSetCollider = false;

        private Vector2 _viewerPosition => new Vector2(_viewer.position.x, _viewer.position.z);

        public TerrainChunk(TerrainChunkParams terrainChunkParams)
        {
            Coordinates = terrainChunkParams.Coordinates;

            var meshWorldSize = terrainChunkParams.MeshSettings.MeshWorldSize;
            var meshScale = terrainChunkParams.MeshSettings.MeshScale;

            _sampleCenter = Coordinates * meshWorldSize / meshScale;
            var position = Coordinates * meshWorldSize;

            _bounds = new Bounds(position, Vector2.one * meshWorldSize);
            _detailLevels = terrainChunkParams.DetailLevels;
            _colliderLODIndex = terrainChunkParams.ColliderLODIndex;
            _colliderGenerationDistanceThreshold = terrainChunkParams.ColliderGenerationDistanceThreshold;
            _viewer = terrainChunkParams.Viewer;
            _meshSettings = terrainChunkParams.MeshSettings;
            _heightMapSettings = terrainChunkParams.HeightMapSettings;

            _meshObject = new GameObject("Terrain Chunk");
            _meshObject.transform.position = new Vector3(position.x, 0, position.y);
            _meshObject.transform.parent = terrainChunkParams.Parent;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = terrainChunkParams.Material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _meshCollider = _meshObject.AddComponent<MeshCollider>();

            SetVisible(false);

            _levelOfDetailMeshes = new LevelOfDetailMesh[_detailLevels.Length];
            for (int i = 0; i < _detailLevels.Length; i++)
            {
                _levelOfDetailMeshes[i] = new LevelOfDetailMesh(_detailLevels[i].LevelOfDetail);
                _levelOfDetailMeshes[i].UpdateCallback += UpdateChunk;
                if (i == _colliderLODIndex)
                {
                    _levelOfDetailMeshes[i].UpdateCallback += UpdateCollisionMesh;
                }
            }

            _maxViewDistance = _detailLevels[_detailLevels.Length - 1].VisibleDistanceThreshold;
        }

        /// <summary>
        /// Gets data dependencies for this terrain chunk that must be retrieved asynchronously.
        /// </summary>
        public void Load()
        {
            ThreadedDataRequester.RequestData(GenerateHeightMap, OnHeightMapReceived);
        }

        /// <summary>
        /// Updates the terrain chunk to show/hide itself and change level of detail
        /// depending on distance from viewer.
        /// </summary>
        public void UpdateChunk()
        {
            if (!_heightMapRecieved)
            {
                return;
            }

            var viewerDstFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(_viewerPosition));
            var wasVisible = IsVisible();
            var visible = viewerDstFromNearestEdge <= _maxViewDistance;
            if (visible)
            {
                var levelOfDetailIndex = GetLevelOfDetailIndex(viewerDstFromNearestEdge);
                UpdateMesh(levelOfDetailIndex);
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                OnVisibilityChanged?.Invoke(this, visible);
            }
        }

        /// <summary>
        /// Shows or hides this terrain chunk.
        /// </summary>
        /// <param name="visible">Whether or not the terrain chunk is visible.</param>
        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        /// <summary>
        /// Gets whether or not this terrain chunk is visible to the viewer.
        /// </summary>
        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }

        /// <summary>
        /// Bakes a collision mesh for this terrain chunk.
        /// </summary>
        public void UpdateCollisionMesh()
        {
            if (_hasSetCollider)
            {
                //This chunk already has a mesh collider.
                return;
            }

            if (_colliderLODIndex < 0 || _colliderLODIndex >= _levelOfDetailMeshes.Length)
            {
                Debug.LogError($"Collider LOD index of {_colliderLODIndex} is out of range.");
                return;
            }

            var collisionLevelOfDetailMesh = _levelOfDetailMeshes[_colliderLODIndex];
            var sqrDistanceFromViewerToEdge = _bounds.SqrDistance(_viewerPosition);
            if (sqrDistanceFromViewerToEdge < _detailLevels[_colliderLODIndex].SqrVisibleDstThreshold &&
                !collisionLevelOfDetailMesh.HasRequestedMesh)
            {
                collisionLevelOfDetailMesh.RequestMesh(_heightMap, _meshSettings);
            }

            if (sqrDistanceFromViewerToEdge < _colliderGenerationDistanceThreshold * _colliderGenerationDistanceThreshold &&
                collisionLevelOfDetailMesh.HasMesh)
            {
                _meshCollider.sharedMesh = collisionLevelOfDetailMesh.Mesh;
                _hasSetCollider = true;
            }
        }

        private int GetLevelOfDetailIndex(float viewerDstFromNearestEdge)
        {
            var levelOfDetailIndex = 0;
            for (var i = 0; i < _detailLevels.Length - 1; i++)
            {
                if (viewerDstFromNearestEdge > _detailLevels[i].VisibleDistanceThreshold)
                {
                    levelOfDetailIndex = i + 1;
                }
                else
                {
                    break;
                }
            }

            return levelOfDetailIndex;
        }

        private void UpdateMesh(int levelOfDetailIndex)
        {
            if (levelOfDetailIndex == _previousLevelOfDetailIndex)
            {
                //Level of detail did not change since last time.
                return;
            }

            var levelOfDetailMesh = _levelOfDetailMeshes[levelOfDetailIndex];
            if (levelOfDetailMesh.HasMesh)
            {
                _previousLevelOfDetailIndex = levelOfDetailIndex;
                _meshFilter.mesh = levelOfDetailMesh.Mesh;
            }
            else if (!levelOfDetailMesh.HasRequestedMesh)
            {
                levelOfDetailMesh.RequestMesh(_heightMap, _meshSettings);
            }
        }

        private HeightMap GenerateHeightMap()
        {
            return HeightMapGenerator.GenerateHeightMap(
                _meshSettings.NumberOfVerticesPerLine,
                _meshSettings.NumberOfVerticesPerLine,
                _heightMapSettings,
                _sampleCenter);
        }

        private void OnHeightMapReceived(object heightMap)
        {
            _heightMap = (HeightMap)heightMap;
            _heightMapRecieved = true;

            UpdateChunk();
        }
    }
}