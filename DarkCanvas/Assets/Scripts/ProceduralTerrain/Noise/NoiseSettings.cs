using System;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    [Serializable]
    public class NoiseSettings
    {
        /// <summary>
        /// How the noise is normalized after it is generated.
        /// </summary>
        public NormalizeMode NormalizeMode;

        /// <summary>
        /// Scale of the noise. Increasing this will increase the width of terrain features.
        /// </summary>
        public float Scale = 50;

        /// <summary>
        /// Number of noise layers to use when generating the noise map.
        /// </summary>
        public int Octaves = 6;

        /// <summary>
        /// Decrease in amplitude of each subsequent octave. Value must be between 0 and 1.
        /// amplitude = persistence ^ octave
        /// </summary>
        [Range(0, 1)]
        public float Persistence = 0.6f;

        /// <summary>
        /// Increase in frequency of each subsequent octave.
        /// frequency = lacunarity ^ octave
        /// </summary>
        public float Lacunarity = 2;

        /// <summary>
        /// Seed used for the random number generator.
        /// </summary>
        public int Seed;

        /// <summary>
        /// The location offset of the noise.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// Validates each property in this class.
        /// </summary>
        public void ValidateValues()
        {
            Scale = Mathf.Max(Scale, 0.01f);
            Octaves = Mathf.Max(Octaves, 1);
            Lacunarity = Mathf.Max(Lacunarity, 1);
            Persistence = Mathf.Clamp01(Persistence);
        }
    }
}