using DarkCanvas.Data.ProceduralTerrain;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents a single cube terrain chunk in the endless terrain.
    /// The default size of the chunk is 16x16 units.
    /// </summary>
    public class VoxelTerrainChunk
    {
        public Bounds Bounds { get; private set; }

        private readonly Vector3 _sampleCenter;
        private readonly int _scale = 1;
        private readonly HeightMapSettings _heightMapSettings;
        private readonly GameObject _meshObject;
        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly Octree _octree;

        private NoiseMap3D _noiseMap;
        private MeshData _meshData;

        public VoxelTerrainChunk(
            TerrainChunkParams terrainChunkParams,
            Bounds bounds,
            Octree octree)
        {
            var meshWorldSize = terrainChunkParams.MeshSettings.MeshWorldSize;
            var position = (bounds.center - (bounds.size / 2f));

            Bounds = bounds;
            _scale = Mathf.FloorToInt(Bounds.size.x / meshWorldSize);

            _sampleCenter = position / _scale;

            _heightMapSettings = terrainChunkParams.HeightMapSettings;

            _meshObject = new GameObject("Terrain chunk");
            _meshObject.transform.position = position;
            _meshObject.transform.parent = terrainChunkParams.Parent;
            _meshObject.transform.localScale = Vector3.one * _scale;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = terrainChunkParams.Material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _octree = octree;
        }

        /// <summary>
        /// Generates data required to build the terrain chunk.
        /// </summary>
        public void GenerateChunk()
        {
            var noiseMap3D = NoiseMapGenerator.GenerateNoiseMap3D(
                NoiseSettings.NOISE_SIZE_3D,
                NoiseSettings.NOISE_SIZE_3D,
                NoiseSettings.NOISE_SIZE_3D,
                _scale,
                _heightMapSettings,
                _sampleCenter);

            _meshData = new VoxelMeshGenerator(
                noiseMap3D.Values,
                MeshSettings.VOXEL_CHUNK_SIZE,
                GetNeighborChunksWithLowerLod(),
                Vector3Int.one).GenerateTerrainMesh();
        }

        /// <summary>
        /// Builds the terrain chunk.
        /// </summary>
        public void BuildChunk()
        {
            _meshFilter.mesh = _meshData.CreateMesh();
        }

        /// <summary>
        /// Shows or hides this terrain chunk.
        /// </summary>
        /// <param name="visible">Whether or not the terrain chunk is visible.</param>
        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        private CubeFaceDirection GetNeighborChunksWithLowerLod()
        {
            var neighborChunksWithLowerLod = CubeFaceDirection.None;
            var translationAmount = Bounds.size.x;

            var rightBound = _octree.GetBound(Bounds.center + new Vector3(translationAmount, 0, 0));
            if (rightBound.HasValue && rightBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveX;
            }

            var leftBound = _octree.GetBound(Bounds.center + new Vector3(-translationAmount, 0, 0));
            if (leftBound.HasValue && leftBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeX;
            }

            var topBound = _octree.GetBound(Bounds.center + new Vector3(0, translationAmount, 0));
            if (topBound.HasValue && topBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveY;
            }

            var bottomBound = _octree.GetBound(Bounds.center + new Vector3(0, -translationAmount, 0));
            if (bottomBound.HasValue && bottomBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeY;
            }

            var forwardBound = _octree.GetBound(Bounds.center + new Vector3(0, 0, translationAmount));
            if (forwardBound.HasValue && forwardBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveZ;
            }

            var backwardBound = _octree.GetBound(Bounds.center + new Vector3(0, 0, -translationAmount));
            if (backwardBound.HasValue && backwardBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeZ;
            }

            return neighborChunksWithLowerLod;
        }
    }
}