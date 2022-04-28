using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Holds information for building a new terrain chunk.
    /// </summary>
    public class TerrainChunkParams
    {
        /// <summary>
        /// Position of the terrain chunk relative to other terrain chunk. For example, 
        /// if terrain chunk A is directly to the right of chunk B with coordinates (0,0), 
        /// chunk A would have coordinates (0,1).
        /// </summary>
        public Vector2 Coordinates { get; init; }

        /// <summary>
        /// Size of the terrain chunk.
        /// </summary>
        public int Size { get; init; }

        /// <summary>
        /// Array containing information for what mesh level of detail to use at various 
        /// distances from the viewer.
        /// </summary>
        public LevelOfDetailInfo[] DetailLevels { get; init; }

        /// <summary>
        /// The parent transform to use when instantiating the terrain chunk game object.
        /// </summary>
        public Transform Parent { get; init; }

        /// <summary>
        /// Material of the terrain chunk.
        /// </summary>
        public Material Material { get; init; }

        /// <summary>
        /// Class for generating a single chunk of terrain.
        /// </summary>
        public MapGenerator MapGenerator { get; init; }

        /// <summary>
        /// Scale of the entire endless terrain.
        /// </summary>
        public float GlobalTerrainScale { get; init; }
    }
}