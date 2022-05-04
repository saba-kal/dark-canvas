﻿using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents a single square terrain chunk in the endless terrain.
    /// The size of the chunk is roughly 240x240 units.
    /// </summary>
    public class TerrainChunk
    {
        private readonly GameObject _meshObject;
        private readonly Vector2 _position;
        private readonly Bounds _bounds;
        private readonly MapGenerator _mapGenerator;
        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;
        private readonly LevelOfDetailInfo[] _detailLevels;
        private readonly LevelOfDetailMesh[] _levelOfDetailMeshes;
        private readonly int _colliderLODIndex;
        private readonly float _colliderGenerationDistanceThreshold;

        private HeightMap _mapData;
        private bool _mapDataRecieved;
        private int _previousLevelOfDetailIndex = -1;
        private bool _hasSetCollider = false;

        public TerrainChunk(TerrainChunkParams terrainChunkParams)
        {
            _position = terrainChunkParams.Coordinates * terrainChunkParams.Size;
            _bounds = new Bounds(_position, Vector2.one * terrainChunkParams.Size);
            _detailLevels = terrainChunkParams.DetailLevels;
            _mapGenerator = terrainChunkParams.MapGenerator;
            _colliderLODIndex = terrainChunkParams.ColliderLODIndex;
            _colliderGenerationDistanceThreshold = terrainChunkParams.ColliderGenerationDistanceThreshold;

            _meshObject = new GameObject("Terrain Chunk");
            _meshObject.transform.position = new Vector3(_position.x, 0, _position.y) * terrainChunkParams.UniformScale;
            _meshObject.transform.parent = terrainChunkParams.Parent;
            _meshObject.transform.localScale = Vector3.one * terrainChunkParams.UniformScale;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = terrainChunkParams.Material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _meshCollider = _meshObject.AddComponent<MeshCollider>();

            SetVisible(false);

            _levelOfDetailMeshes = new LevelOfDetailMesh[_detailLevels.Length];
            for (int i = 0; i < _detailLevels.Length; i++)
            {
                _levelOfDetailMeshes[i] = new LevelOfDetailMesh(_detailLevels[i].LevelOfDetail, _mapGenerator);
                _levelOfDetailMeshes[i].UpdateCallback += UpdateChunk;
                if (i == _colliderLODIndex)
                {
                    _levelOfDetailMeshes[i].UpdateCallback += UpdateCollisionMesh;
                }
            }

            _mapGenerator.RequestMapData(_position, OnMapDataReceived);
        }

        /// <summary>
        /// Updates the terrain chunk to show/hide itself and change level of detail
        /// depending on distance from viewer.
        /// </summary>
        public void UpdateChunk()
        {
            if (!_mapDataRecieved)
            {
                return;
            }

            var viewerDstFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(EndlessTerrain.ViewerPosition));
            var visible = viewerDstFromNearestEdge <= EndlessTerrain.MaxViewDistance;
            SetVisible(visible);
            if (!visible)
            {
                return;
            }

            var levelOfDetailIndex = GetLevelOfDetailIndex(viewerDstFromNearestEdge);
            UpdateMesh(levelOfDetailIndex);
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
            var sqrDistanceFromViewerToEdge = _bounds.SqrDistance(EndlessTerrain.ViewerPosition);
            if (sqrDistanceFromViewerToEdge < _detailLevels[_colliderLODIndex].SqrVisibleDstThreshold &&
                !collisionLevelOfDetailMesh.HasRequestedMesh)
            {
                collisionLevelOfDetailMesh.RequestMesh(_mapData);
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
                levelOfDetailMesh.RequestMesh(_mapData);
            }
        }

        private void OnMapDataReceived(HeightMap mapData)
        {
            _mapData = mapData;
            _mapDataRecieved = true;

            UpdateChunk();
        }
    }
}