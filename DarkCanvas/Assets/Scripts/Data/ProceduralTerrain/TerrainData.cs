using UnityEngine;

namespace DarkCanvas.Data.ProceduralTerrain
{
    /// <summary>
    /// Holds parameters for generating procedural terrains.
    /// </summary>
    [CreateAssetMenu(fileName = "New TerrainData", menuName = "ScriptableObjects/TerrainData")]
    public class TerrainData : UpdatableData
    {
        [SerializeField] private bool _useFlatShading;
        [SerializeField] private bool _useFalloff;
        [SerializeField] private float _uniformScale = 1.5f;
        [SerializeField] private float _meshHeightMultiplier;
        [SerializeField] private AnimationCurve _meshHeightCurve;

        /// <summary>
        /// Whether or not the terrain is shaded flat or smooth.
        /// </summary>
        public bool UseFlatShading => _useFlatShading;

        /// <summary>
        /// If true, noise map will gradually become 0 towards the edges of the terrain.
        /// </summary>
        public bool UseFalloff => _useFalloff;

        /// <summary>
        /// Scale multiplier for each terrain chunk game object.
        /// </summary>
        public float UniformScale => _uniformScale;

        /// <summary>
        /// Multiplies the Y value of each terrain mesh vertex.
        /// </summary>
        public float MeshHeightMultiplier => _meshHeightMultiplier;

        /// <summary>
        /// Applies a curve function to the Y value of each terrain mesh vertex.
        /// </summary>
        public AnimationCurve MeshHeightCurve => _meshHeightCurve;

        /// <summary>
        /// Minimum possible height of the terrain.
        /// </summary>
        public float MinHeight => _uniformScale * _meshHeightMultiplier * _meshHeightCurve.Evaluate(0);


        /// <summary>
        /// Maximum possible height of the terrain.
        /// </summary>
        public float MaxHeight => _uniformScale * _meshHeightMultiplier * _meshHeightCurve.Evaluate(1f);
    }
}