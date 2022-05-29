using DarkCanvas.Data.ProceduralTerrain;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Class for displaying a preview of a single terrain chunk.
    /// </summary>
    public class MapPreview : MonoBehaviour
    {
        public bool AutoUpdate => _autoUpdate;

        [SerializeField] private bool _autoUpdate = false;
        [SerializeField] private bool _drawNoiseGizmo = false;
        [SerializeField] private Renderer _textureRenderer;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private MapDrawMode _drawMode;
        [Range(0, MeshSettings.NUMBER_OF_SUPPORTED_LODS - 1)]
        [SerializeField] private int _previewLevelOfDetail;

        [SerializeField] private VoxelMeshGeneratorDebug _voxelMeshGeneratorDebug;
        [SerializeField] private MeshSettings _meshSettings;
        [SerializeField] private HeightMapSettings _heightMapSettings;
        [SerializeField] private TextureSettings _textureSettings;
        [SerializeField] private Material _terrainMaterial;

        private NoiseMap3D _noiseMap3D = null;

        /// <summary>
        /// Generates the map in the Unity editor.
        /// </summary>
        public void DrawMapInEditor()
        {
            _textureSettings.ApplyToMaterial(_terrainMaterial);
            _textureSettings.UpdateMeshHeights(
                _terrainMaterial,
                _heightMapSettings.MinHeight,
                _heightMapSettings.MaxHeight);

            var heightMap = HeightMapGenerator.GenerateHeightMap(
                _meshSettings.NumberOfVerticesPerLine,
                _meshSettings.NumberOfVerticesPerLine,
                _heightMapSettings,
                Vector2.zero);

            _noiseMap3D = NoiseMapGenerator.GenerateNoiseMap3D(
                NoiseSettings.NOISE_SIZE_3D,
                NoiseSettings.NOISE_SIZE_3D,
                NoiseSettings.NOISE_SIZE_3D,
                1,
                _heightMapSettings,
                Vector3.zero);

            switch (_drawMode)
            {
                case MapDrawMode.NoiseMap:
                    DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
                    break;
                case MapDrawMode.Mesh:
                    DrawMesh(
                        MeshGenerator.GenerateTerrainMesh(
                            heightMap.Values,
                            _meshSettings,
                            _previewLevelOfDetail));
                    break;
                case MapDrawMode.FalloffMap:
                    DrawTexture(TextureGenerator.TextureFromHeightMap(
                        new HeightMap
                        {
                            Values = FalloffGenerator.GenerateFalloffMap(_meshSettings.NumberOfVerticesPerLine),
                            MinValue = 0,
                            MaxValue = 1
                        }));
                    break;
                case MapDrawMode.NoiseMap3D:
                    DrawTexture(TextureGenerator.TextureFromNoiseMap(_noiseMap3D));
                    break;
                case MapDrawMode.VoxelMesh:
                    DrawMesh(
                        new VoxelMeshGenerator(_noiseMap3D.Values, MeshSettings.VOXEL_CHUNK_SIZE).GenerateTerrainMesh(Vector3Int.one));
                    break;
                case MapDrawMode.SimpleVoxelMesh:
                    DrawSimpleVoxelMesh();
                    break;
            }
        }

        private void DrawTexture(Texture2D texture)
        {
            _textureRenderer.sharedMaterial.mainTexture = texture;
            _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
            _textureRenderer.gameObject.SetActive(true);
            _meshFilter.gameObject.SetActive(false);
        }

        private void DrawMesh(MeshData meshData)
        {
            _meshFilter.sharedMesh = meshData.CreateMesh();
            _textureRenderer.gameObject.SetActive(false);
            _meshFilter.gameObject.SetActive(true);
        }

        private void DrawSimpleVoxelMesh()
        {
            const int chunkSize = 2;
            const int noiseSize = chunkSize + 3;
            var noiseMap3D = new float[noiseSize, noiseSize, noiseSize];
            for (var x = 0; x < noiseSize; x++)
            {
                for (var y = 0; y < noiseSize; y++)
                {
                    for (var z = 0; z < noiseSize; z++)
                    {
                        if (x <= 1)
                            noiseMap3D[x, y, z] = 127;
                        else
                            noiseMap3D[x, y, z] = -127;
                    }
                }
            }

            DrawMesh(
                new VoxelMeshGenerator(noiseMap3D, chunkSize).GenerateTerrainMesh(Vector3Int.one));
        }

        private void OnValidate()
        {
            if (_meshSettings != null)
            {
                _meshSettings.OnValuesUpdated -= OnValuesUpdated;
                _meshSettings.OnValuesUpdated += OnValuesUpdated;
            }
            if (_heightMapSettings != null)
            {
                _heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
                _heightMapSettings.OnValuesUpdated += OnValuesUpdated;
            }
            if (_textureSettings != null)
            {
                _textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
                _textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
            }
        }

        private void OnValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                DrawMapInEditor();
            }
        }

        private void OnTextureValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                _textureSettings.ApplyToMaterial(_terrainMaterial);
            }
        }

        private void OnDrawGizmos()
        {
            if (_noiseMap3D == null || !_drawNoiseGizmo)
            {
                return;
            }

            var noiseSize = _noiseMap3D.Values.GetLength(0);
            for (var x = 0; x < noiseSize; x++)
            {
                for (var y = 0; y < noiseSize; y++)
                {
                    for (var z = 0; z < noiseSize; z++)
                    {
                        var value = _noiseMap3D.Values[x, y, z] + 0b_0100_0000;
                        var alpha = (value + 128) / 256f;

                        Color color = Color.blue;
                        if (value < 0)
                        {
                            color = Color.red;
                        }
                        color.a = alpha;

                        Gizmos.color = color;
                        Gizmos.DrawSphere(transform.position + new Vector3(x, y, z), 0.2f);
                    }
                }
            }
        }
    }
}