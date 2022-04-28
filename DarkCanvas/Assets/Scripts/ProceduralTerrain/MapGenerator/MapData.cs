using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Holds the height and color maps for each terrain chunk.
    /// </summary>
    public class MapData
    {
        /// <summary>
        /// Height of each vertex in the terrain chunk mesh.
        /// </summary>
        public float[,] HeightMap { get; init; }

        /// <summary>
        /// Color of each square in the terrain chunk
        /// from left to right, top to bottom.
        /// </summary>
        public Color[] ColorMap { get; init; }
    }
}