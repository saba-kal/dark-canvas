namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Holds the height and color maps for each terrain chunk.
    /// </summary>
    public class HeightMap
    {
        /// <summary>
        /// Height of each vertex in the terrain chunk mesh.
        /// </summary>
        public float[,] Values { get; init; }
    }
}