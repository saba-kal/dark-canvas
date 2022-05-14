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
        [SerializeField] private int _renderDistanceMultiplier = 16;
        [SerializeField] private int _terrainChunksToBuildPerThread = 10;
        [SerializeField] private int _terrainChunkBatchesToBuildPerFrame = 5;
        [SerializeField] private MeshSettings _meshSettings;
        [SerializeField] private HeightMapSettings _heightMapSettings;
        [SerializeField] private TextureSettings _textureSettings;
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private int _colliderLODIndex;
        [SerializeField] private LevelOfDetailInfo[] _detailLevels;

        private float _meshWorldSize;
        private int _chunksVisibleInViewDistance;
        private List<VoxelTerrainChunk> _visibleTerrainChunks = new List<VoxelTerrainChunk>();
        private Dictionary<Bounds, VoxelTerrainChunk> _terrainChunkDictionary =
            new Dictionary<Bounds, VoxelTerrainChunk>();
        private float _sqrViewerMoveThresholdForChunkUpdate;
        private Vector3 _viewerPositionOld;
        private Octree _octree;
        private Queue<VoxelTerrainChunk> _terrainChunksToBuild = new Queue<VoxelTerrainChunk>();

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

            BuildTerrainChunks();
        }

        private void UpdateVisibleChunks()
        {
            var currentChunkPosition = new Vector3(
                Mathf.RoundToInt(_viewer.position.x / _meshWorldSize) * _meshWorldSize,
                Mathf.RoundToInt(_viewer.position.y / _meshWorldSize) * _meshWorldSize,
                Mathf.RoundToInt(_viewer.position.z / _meshWorldSize) * _meshWorldSize);

            _octree = new Octree(
                new Bounds(currentChunkPosition, Vector3.one * _meshWorldSize * _renderDistanceMultiplier),
                _meshWorldSize);
            _octree.Insert(_viewer.position);

            var visibleChunkBounds = new HashSet<Bounds>();
            foreach (var bound in _octree.GetAllLeafBounds())
            {
                if (_terrainChunkDictionary.TryGetValue(bound, out var terrainChunk))
                {
                    //Terrain chunk was previously visited.
                    terrainChunk.SetVisible(true);
                }
                else
                {
                    //Terrain chunk does not exist. Generate a new one.
                    terrainChunk = CreateTerrainChunk(bound);
                    _terrainChunksToBuild.Enqueue(terrainChunk);
                    _terrainChunkDictionary.Add(bound, terrainChunk);
                }
                visibleChunkBounds.Add(bound);
            }

            //Hide chunks that are not part of the generated octree.
            foreach (var chunk in _terrainChunkDictionary.Values)
            {
                if (!visibleChunkBounds.Contains(chunk.Bounds))
                {
                    chunk.SetVisible(false);
                }
            }
        }

        private void BuildTerrainChunks()
        {
            if (_terrainChunksToBuild.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _terrainChunkBatchesToBuildPerFrame; i++)
            {
                var terrainChunksToBuildThisBatch = new List<VoxelTerrainChunk>();
                while (_terrainChunksToBuild.Count > 0)
                {
                    terrainChunksToBuildThisBatch.Add(_terrainChunksToBuild.Dequeue());
                    if (terrainChunksToBuildThisBatch.Count >= _terrainChunksToBuildPerThread)
                    {
                        var terrainChunkBuilder = new TerrainChunkBuilder(terrainChunksToBuildThisBatch);
                        terrainChunkBuilder.BuildTerrainChunks();
                        terrainChunksToBuildThisBatch = new List<VoxelTerrainChunk>();
                    }
                }

                if (_terrainChunksToBuild.Count == 0)
                {
                    break;
                }
            }

        }

        private VoxelTerrainChunk CreateTerrainChunk(Bounds bounds)
        {
            return new VoxelTerrainChunk(
                new TerrainChunkParams
                {
                    DetailLevels = _detailLevels,
                    Parent = transform,
                    Material = _terrainMaterial,
                    ColliderLODIndex = _colliderLODIndex,
                    ColliderGenerationDistanceThreshold = _colliderGenerationDistanceThreshold,
                    Viewer = _viewer,
                    MeshSettings = _meshSettings,
                    HeightMapSettings = _heightMapSettings
                },
                bounds);
        }

        private void OnDrawGizmos()
        {
            if (_octree == null)
            {
                return;
            }

            Gizmos.color = Color.blue;
            _octree.DrawGizmo();
        }
    }
}