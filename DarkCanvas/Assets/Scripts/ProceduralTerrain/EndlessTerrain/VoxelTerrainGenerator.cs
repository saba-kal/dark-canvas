using DarkCanvas.Data.ProceduralTerrain;
using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public class VoxelTerrainGenerator : MonoBehaviour
    {
        [SerializeField] private Transform _viewer;
        [SerializeField] private float _viewerMoveThresholdForChunkUpdate = 25f;
        [SerializeField] private float _colliderGenerationDistanceThreshold = 5f;
        [SerializeField] private float _terrainRenderDistance = 100f;
        [SerializeField] private MeshSettings _meshSettings;
        [SerializeField] private HeightMapSettings _heightMapSettings;
        [SerializeField] private TextureSettings _textureSettings;
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private int _colliderLODIndex;
        [SerializeField] private LevelOfDetailInfo[] _detailLevels;

        private float _meshWorldSize;
        private int _chunksVisibleInViewDistance;
        private List<VoxelTerrainChunk> _visibleTerrainChunks = new List<VoxelTerrainChunk>();
        private Dictionary<Vector3, VoxelTerrainChunk> _terrainChunkDictionary =
            new Dictionary<Vector3, VoxelTerrainChunk>();
        private float _sqrViewerMoveThresholdForChunkUpdate;
        private Vector3 _viewerPositionOld;


        private void Start()
        {
            _sqrViewerMoveThresholdForChunkUpdate = Mathf.Pow(_viewerMoveThresholdForChunkUpdate, 2);
            _meshWorldSize = _meshSettings.MeshWorldSize;
            _chunksVisibleInViewDistance = Mathf.RoundToInt(_terrainRenderDistance / _meshWorldSize);
            _textureSettings.ApplyToMaterial(_terrainMaterial);
            _textureSettings.UpdateMeshHeights(_terrainMaterial, _heightMapSettings.MinHeight, _heightMapSettings.MaxHeight);
            UpdateVisibleChunks();
        }

        private void Update()
        {
            if (_viewer.position != _viewerPositionOld)
            {
                foreach (var terrainChunk in _visibleTerrainChunks)
                {
                    terrainChunk.UpdateCollisionMesh();
                }
            }

            if ((_viewerPositionOld - _viewer.position).sqrMagnitude >
                _sqrViewerMoveThresholdForChunkUpdate)
            {
                _viewerPositionOld = _viewer.position;
                UpdateVisibleChunks();
            }
        }

        private void UpdateVisibleChunks()
        {
            var alreadyUpdatedChunkCoords = new HashSet<Vector3>();
            //Decrement backwards in case list size changes.
            for (var i = _visibleTerrainChunks.Count - 1; i >= 0; i--)
            {
                alreadyUpdatedChunkCoords.Add(_visibleTerrainChunks[i].Coordinates);
                _visibleTerrainChunks[i].UpdateChunk();
            }

            var currentChunkCoordX = Mathf.RoundToInt(_viewer.position.x / _meshWorldSize);
            var currentChunkCoordY = Mathf.RoundToInt(_viewer.position.y / _meshWorldSize);
            var currentChunkCoordZ = Mathf.RoundToInt(_viewer.position.z / _meshWorldSize);

            for (var zOffset = -_chunksVisibleInViewDistance; zOffset <= _chunksVisibleInViewDistance; zOffset++)
            {
                for (var yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++)
                {
                    for (var xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++)
                    {
                        var viewedChunkCoord = new Vector3(
                            currentChunkCoordX + xOffset,
                            currentChunkCoordY + yOffset,
                            currentChunkCoordZ + zOffset);

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
        }

        private void OnTerrainChunkVisiblityChanged(VoxelTerrainChunk terrainChunk, bool visible)
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

        private VoxelTerrainChunk BuildTerrainChunk(Vector3 coordinates)
        {
            return new VoxelTerrainChunk(
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
                },
                _terrainRenderDistance);
        }
    }
}