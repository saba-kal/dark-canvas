namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Holds the height map of each terrain chunk.
    /// </summary>
    public class HeightMap
    {
        /// <summary>
        /// 2D array of values that represent the height map.
        /// </summary>
        public float[,] Values { get; init; }

        /// <summary>
        /// Minimum height map value in the 2D array.
        /// </summary>
        public float MinValue { get; init; }

        /// <summary>
        /// Maximum height map value in the 2D array.
        /// </summary>
        public float MaxValue { get; init; }
    }
}