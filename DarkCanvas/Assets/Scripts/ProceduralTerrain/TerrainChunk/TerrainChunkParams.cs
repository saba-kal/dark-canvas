using DarkCanvas.Data.ProceduralTerrain;
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
        /// Index of the terrain level of detail to use for generating the collider.
        /// </summary>
        public int ColliderLODIndex { get; init; }

        /// <summary>
        /// Distance a player must be from a terrain chunk before a collision mesh is generated.
        /// </summary>
        public float ColliderGenerationDistanceThreshold { get; init; }

        /// <summary>
        /// Location of the terrain viewer. Used to display various terrain mesh levels of details.
        /// </summary>
        public Transform Viewer { get; init; }

        /// <summary>
        /// Holds settings for generating a height map.
        /// </summary>
        public HeightMapSettings HeightMapSettings { get; init; }

        /// <summary>
        /// Holds settings for generating a mesh.
        /// </summary>
        public MeshSettings MeshSettings { get; init; }
    }
}