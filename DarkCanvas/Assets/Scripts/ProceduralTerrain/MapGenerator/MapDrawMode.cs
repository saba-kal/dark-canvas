namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Type of map to generate in the Unity editor.
    /// </summary>
    public enum MapDrawMode
    {
        /// <summary>
        /// Show a simple black and white texture of our noise function.
        /// </summary>
        NoiseMap,

        /// <summary>
        /// Generate the mesh associated with our noise function.
        /// </summary>
        Mesh,

        /// <summary>
        /// Generates a black and white texture with pixels closest to the edge being black.
        /// </summary>
        FalloffMap
    }
}