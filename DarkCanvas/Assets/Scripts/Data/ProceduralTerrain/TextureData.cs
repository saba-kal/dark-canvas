using UnityEngine;

namespace DarkCanvas.Data.ProceduralTerrain
{
    /// <summary>
    /// Holds parameters for generating procedural terrain texture.
    /// </summary>
    [CreateAssetMenu(fileName = "New TextureData", menuName = "ScriptableObjects/TextureData")]
    public class TextureData : UpdatableData
    {
        [SerializeField] private Gradient _baseGradient;
        [Range(1, 256)]
        [SerializeField] private int _textureResolution;

        private float _savedMinHeight;
        private float _savedMaxHeight;

        public void ApplyToMaterial(Material material)
        {
            material.SetTexture("_baseColors", GetTextureFromGradient());
            UpdateMeshHeights(material, _savedMinHeight, _savedMaxHeight);
        }

        /// <summary>
        /// Lets the terrain material shader know the minimum and maximum heights of the terrain.
        /// </summary>
        /// <param name="material">Material to pass the heights to.</param>
        /// <param name="minHeight">Minimum height of the terrain.</param>
        /// <param name="maxHeight">Maximum height of the terrain.</param>
        public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
        {
            _savedMinHeight = minHeight;
            _savedMaxHeight = maxHeight;
            material.SetFloat("_minHeight", minHeight);
            material.SetFloat("_maxHeight", maxHeight);
        }

        private Texture2D GetTextureFromGradient()
        {
            var texture = new Texture2D(1, _textureResolution);

            for (var i = 0; i < _textureResolution; i++)
            {
                texture.SetPixel(0, i, _baseGradient.Evaluate(i / (float)_textureResolution));
            }
            texture.Apply();

            return texture;
        }
    }
}