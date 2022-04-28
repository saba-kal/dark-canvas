using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Holds information for building a new terrain mesh.
    /// </summary>
    public class MeshGeneratorParams
    {
        /// <summary>
        /// Height of each mesh vertex.
        /// </summary>
        public float[,] HeightMap { get; init; }

        /// <summary>
        /// Multiplier for the height of each vertex.
        /// </summary>
        public float HeightMultiplier { get; init; }

        /// <summary>
        /// Evaluation function for vertex height.
        /// Allows for variable height changes depending on the initial height of the vertex.
        /// </summary>
        public AnimationCurve HeightCurve { get; init; }

        /// <summary>
        /// Indicates the complexity level of the mesh. Highest complexity is 0, and
        /// subsequent numbers decrease mesh detail.
        /// </summary>
        public int LevelOfDetail { get; init; }

        /// <summary>
        /// Determines whether the mesh is shaded smooth or flat.
        /// </summary>
        public bool UseFlatShading { get; init; }
    }
}