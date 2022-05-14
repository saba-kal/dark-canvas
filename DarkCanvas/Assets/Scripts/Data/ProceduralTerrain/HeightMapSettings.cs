using DarkCanvas.ProceduralTerrain;
using UnityEngine;

namespace DarkCanvas.Data.ProceduralTerrain
{
    /// <summary>
    /// Holds parameters for generating height maps.
    /// </summary>
    [CreateAssetMenu(fileName = "HeightMapSettings", menuName = "ScriptableObjects/Height Map Settings")]
    public class HeightMapSettings : UpdatableData
    {
        [SerializeField] private NoiseSettings _noiseSettings;
        [SerializeField] private bool _useFalloff;
        [SerializeField] private float _heightMultiplier;
        [SerializeField] private AnimationCurve _heightCurve;

        /// <summary>
        /// Holds parameters for generating terrain noise.
        /// </summary>
        public NoiseSettings NoiseSettings => _noiseSettings;

        /// <summary>
        /// Multiplies each value in the height map.
        /// </summary>
        public float HeightMultiplier => _heightMultiplier;

        /// <summary>
        /// Applies a curve function to each value in the height map.
        /// </summary>
        public AnimationCurve HeightCurve => _heightCurve;

        /// <summary>
        /// Minimum possible height of the terrain.
        /// </summary>
        public float MinHeight => _heightMultiplier * _heightCurve.Evaluate(0);

        /// <summary>
        /// Maximum possible height of the terrain.
        /// </summary>
        public float MaxHeight => _heightMultiplier * _heightCurve.Evaluate(1f);

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            NoiseSettings.ValidateValues();
            base.OnValidate();
        }
#endif
    }
}
