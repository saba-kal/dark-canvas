using DarkCanvas.Data.ProceduralTerrain;
using System.Text;
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
        private readonly float _colliderGenerationDistanceThreshold;
        private readonly MeshSettings _meshSettings;
        private readonly HeightMapSettings _heightMapSettings;
        private readonly Transform _viewer;
        private readonly GameObject _meshObject;
        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;

        private NoiseMap3D _noiseMap;
        private MeshData _meshData;
        private bool _noiseMapRecieved;
        private bool _meshRecieved;
        private bool _hasSetCollider = false;

        public VoxelTerrainChunk(
            TerrainChunkParams terrainChunkParams,
            Bounds bounds)
        {
            var meshWorldSize = terrainChunkParams.MeshSettings.MeshWorldSize;
            var position = (bounds.center - (bounds.size / 2f));

            Bounds = bounds;
            _scale = Mathf.FloorToInt(Bounds.size.x / meshWorldSize);

            _sampleCenter = position / _scale;

            _colliderGenerationDistanceThreshold = terrainChunkParams.ColliderGenerationDistanceThreshold;
            _viewer = terrainChunkParams.Viewer;
            _meshSettings = terrainChunkParams.MeshSettings;
            _heightMapSettings = terrainChunkParams.HeightMapSettings;

            _meshObject = new GameObject("Terrain chunk");
            _meshObject.transform.position = position;
            _meshObject.transform.parent = terrainChunkParams.Parent;
            _meshObject.transform.localScale = Vector3.one * _scale;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = terrainChunkParams.Material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            //_meshFilter.mesh = CreateCube();
            //_meshCollider = _meshObject.AddComponent<MeshCollider>();
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
            _noiseMapRecieved = true;

            _meshData = VoxelMeshGenerator.GenerateTerrainMesh(
                noiseMap3D.Values,
                MeshSettings.VOXEL_CHUNK_SIZE,
                Vector3Int.one);
        }

        /// <summary>
        /// Builds the terrain chunk.
        /// </summary>
        public void BuildChunk()
        {
            _meshFilter.mesh = _meshData.CreateMesh();
            _meshRecieved = true;
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
        /// Bakes a collision mesh for this terrain chunk.
        /// </summary>
        public void UpdateCollisionMesh()
        {
            return;
            if (_hasSetCollider)
            {
                //This chunk already has a mesh collider.
                return;
            }

            //if (_meshRecieved)
            //{
            //    _meshCollider.sharedMesh = _mesh;
            //    _hasSetCollider = true;
            //}
        }

        private string BuildTerrainChunkName()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Terrain Chunk (scale:");
            stringBuilder.Append(_scale);
            stringBuilder.Append(", sample:");
            stringBuilder.Append(_sampleCenter);
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }

        //This is test code.
        private Mesh CreateCube()
        {
            Vector3[] vertices = {
                new Vector3 (0, 0, 0),
                new Vector3 (1, 0, 0),
                new Vector3 (1, 1, 0),
                new Vector3 (0, 1, 0),
                new Vector3 (0, 1, 1),
                new Vector3 (1, 1, 1),
                new Vector3 (1, 0, 1),
                new Vector3 (0, 0, 1),
            };

            int[] triangles = {
                0, 2, 1, //face front
                0, 3, 2,
                2, 3, 4, //face top
                2, 4, 5,
                1, 2, 5, //face right
                1, 5, 6,
                0, 7, 4, //face left
                0, 4, 3,
                5, 4, 7, //face back
                5, 7, 6,
                0, 6, 7, //face bottom
                0, 1, 6
            };

            var mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.Optimize();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}