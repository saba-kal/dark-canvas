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
        [SerializeField] private Renderer _textureRenderer;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private MapDrawMode _drawMode;
        [Range(0, MeshSettings.NUMBER_OF_SUPPORTED_LODS - 1)]
        [SerializeField] private int _previewLevelOfDetail;

        [SerializeField] private MeshSettings _meshSettings;
        [SerializeField] private HeightMapSettings _heightMapSettings;
        [SerializeField] private TextureSettings _textureSettings;
        [SerializeField] private Material _terrainMaterial;

        public void Test()
        {
            var heightMap3D = NoiseMapGenerator.GenerateNoiseMap3D(
                _meshSettings.NumberOfVerticesPerLine,
                _meshSettings.NumberOfVerticesPerLine,
                _meshSettings.NumberOfVerticesPerLine,
                _heightMapSettings,
                Vector3.zero);
        }

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

            var noiseMap3D = NoiseMapGenerator.GenerateNoiseMap3D(
                MeshSettings.VOXEL_CHUNK_SIZE,
                MeshSettings.VOXEL_CHUNK_SIZE,
                MeshSettings.VOXEL_CHUNK_SIZE,
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
                    DrawTexture(TextureGenerator.TextureFromNoiseMap(noiseMap3D));
                    break;
                case MapDrawMode.VoxelMesh:
                    DrawMesh(
                        VoxelMeshGenerator.GenerateTerrainMesh(noiseMap3D.Values));
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
    }
}