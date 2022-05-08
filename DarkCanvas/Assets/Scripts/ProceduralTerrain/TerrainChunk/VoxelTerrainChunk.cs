using DarkCanvas.Data.ProceduralTerrain;
using System;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents a single cube terrain chunk in the endless terrain.
    /// The default size of the chunk is 16x16 units.
    /// </summary>
    public class VoxelTerrainChunk
    {
        public event Action<VoxelTerrainChunk, bool> OnVisibilityChanged;
        public Vector3 Coordinates { get; private set; }

        private readonly Vector3 _sampleCenter;
        private readonly float _colliderGenerationDistanceThreshold;
        private readonly MeshSettings _meshSettings;
        private readonly HeightMapSettings _heightMapSettings;
        private readonly Transform _viewer;
        private readonly GameObject _meshObject;
        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;
        private readonly LevelOfDetailInfo[] _detailLevels;
        private readonly LevelOfDetailMesh[] _levelOfDetailMeshes;
        private readonly float _maxViewDistance;

        private NoiseMap3D _noiseMap;
        private bool _noiseMapRecieved;
        private int _previousLevelOfDetailIndex = -1;
        private bool _hasSetCollider = false;

        public VoxelTerrainChunk(
            TerrainChunkParams terrainChunkParams,
            float maxViewDistance)
        {
            Coordinates = terrainChunkParams.Coordinates;

            var meshWorldSize = terrainChunkParams.MeshSettings.MeshWorldSize;
            var meshScale = terrainChunkParams.MeshSettings.MeshScale;

            _sampleCenter = Coordinates * meshWorldSize;
            var position = Coordinates * meshWorldSize;

            _detailLevels = terrainChunkParams.DetailLevels;
            _colliderGenerationDistanceThreshold = terrainChunkParams.ColliderGenerationDistanceThreshold;
            _viewer = terrainChunkParams.Viewer;
            _meshSettings = terrainChunkParams.MeshSettings;
            _heightMapSettings = terrainChunkParams.HeightMapSettings;

            _meshObject = new GameObject("Terrain Chunk");
            _meshObject.transform.position = position;
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
                if (i == 0)
                {
                    _levelOfDetailMeshes[i].UpdateCallback += UpdateCollisionMesh;
                }
            }

            _maxViewDistance = maxViewDistance;
        }

        /// <summary>
        /// Gets data dependencies for this terrain chunk that must be retrieved asynchronously.
        /// </summary>
        public void Load()
        {
            ThreadedDataRequester.RequestData(GenerateNoiseMap, OnNoiseMapRecieved);
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
        /// Updates the terrain chunk to show/hide itself and change level of detail
        /// depending on distance from viewer.
        /// </summary>
        public void UpdateChunk()
        {
            if (!_noiseMapRecieved)
            {
                return;
            }

            var distance = Vector3.Distance(_meshObject.transform.position, _viewer.position);
            var wasVisible = IsVisible();
            var visible = distance <= _maxViewDistance;
            if (visible)
            {
                var levelOfDetailIndex = GetLevelOfDetailIndex(distance);
                UpdateMesh(levelOfDetailIndex);
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                OnVisibilityChanged?.Invoke(this, visible);
            }
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

            var collisionLevelOfDetailMesh = _levelOfDetailMeshes[0];
            var distance = Vector3.Distance(_meshObject.transform.position, _viewer.position);
            if (distance <= _maxViewDistance && !collisionLevelOfDetailMesh.HasRequestedMesh)
            {
                collisionLevelOfDetailMesh.RequestVoxelMesh(_noiseMap, _meshSettings);
            }

            if (distance < _colliderGenerationDistanceThreshold &&
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
                levelOfDetailMesh.RequestVoxelMesh(_noiseMap, _meshSettings);
            }
        }

        private NoiseMap3D GenerateNoiseMap()
        {
            return NoiseMapGenerator.GenerateNoiseMap3D(
                MeshSettings.VOXEL_CHUNK_SIZE,
                MeshSettings.VOXEL_CHUNK_SIZE,
                MeshSettings.VOXEL_CHUNK_SIZE,
                _heightMapSettings,
                _sampleCenter);
        }

        private void OnNoiseMapRecieved(object noiseMap)
        {
            _noiseMap = (NoiseMap3D)(noiseMap);
            _noiseMapRecieved = true;

            UpdateChunk();
        }
    }
}