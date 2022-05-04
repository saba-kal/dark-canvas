using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public class MapDisplay : MonoBehaviour
    {
        [SerializeField] private Renderer _textureRenderer;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private MapGenerator _mapGenerator;

        public void DrawTexture(Texture2D texture)
        {
            _textureRenderer.sharedMaterial.mainTexture = texture;
            _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
        }

        public void DrawMesh(MeshData meshData)
        {
            _meshFilter.sharedMesh = meshData.CreateMesh();
            _meshFilter.transform.localScale = Vector3.one * _mapGenerator.UniformScale;
        }
    }
}