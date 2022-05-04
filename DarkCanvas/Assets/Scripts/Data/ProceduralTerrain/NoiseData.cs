using DarkCanvas.ProceduralTerrain;
using UnityEngine;

namespace DarkCanvas.Data.ProceduralTerrain
{
    /// <summary>
    /// Holds parameters for generating noise maps.
    /// </summary>
    [CreateAssetMenu(fileName = "New NoiseData", menuName = "ScriptableObjects/NoiseData")]
    public class NoiseData : UpdatableData
    {
        [SerializeField] private NormalizeMode _normalizeMode;
        [SerializeField] private float _noiseScale;
        [SerializeField] private int _octaves;
        [Range(0, 1)]
        [SerializeField] private float _persistence;
        [SerializeField] private float _lacunarity;
        [SerializeField] private int _seed;
        [SerializeField] private Vector2 _offset;

        /// <summary>
        /// How the noise is normalized after it is generated.
        /// </summary>
        public NormalizeMode NormalizeMode => _normalizeMode;

        /// <summary>
        /// Scale of the noise. Increasing this will increase the width of terrain features.
        /// </summary>
        public float NoiseScale => _noiseScale;

        /// <summary>
        /// Number of noise layers to use when generating the noise map.
        /// </summary>
        public int Octaves => _octaves;

        /// <summary>
        /// Decrease in amplitude of each subsequent octave. Value must be between 0 and 1.
        /// amplitude = persistence ^ octave
        /// </summary>
        public float Persistence => _persistence;

        /// <summary>
        /// Increase in frequency of each subsequent octave.
        /// frequency = lacunarity ^ octave
        /// </summary>
        public float Lacunarity => _lacunarity;

        /// <summary>
        /// Seed used for the random number generator.
        /// </summary>
        public int Seed => _seed;

        /// <summary>
        /// The location offset of the noise.
        /// </summary>
        public Vector2 Offset => _offset;

        protected override void OnValidate()
        {
            if (_lacunarity < 1)
            {
                _lacunarity = 1;
            }
            if (_octaves < 0)
            {
                _octaves = 0;
            }

            base.OnValidate();
        }
    }
}
