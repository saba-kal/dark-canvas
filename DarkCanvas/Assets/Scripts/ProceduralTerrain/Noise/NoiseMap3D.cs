namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Holds the noise map for each terrain chunk.
    /// </summary>
    public class NoiseMap3D
    {
        /// <summary>
        /// 3D array of values that represent the noise function.
        /// </summary>
        public sbyte[,,] Values { get; init; }

        /// <summary>
        /// Minimum height map value in the 3D array.
        /// </summary>
        public float MinValue { get; init; }

        /// <summary>
        /// Maximum height map value in the 3D array.
        /// </summary>
        public float MaxValue { get; init; }
    }
}