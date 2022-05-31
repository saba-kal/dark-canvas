using System;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents one of the six sides on a cube terrain chunk. 
    /// </summary>
    [Flags]
    [Serializable]
    public enum CubeFaceDirection
    {
        None = 0,
        NegativeX = 1 << 0,
        PositiveX = 1 << 1,
        NegativeY = 1 << 2,
        PositiveY = 1 << 3,
        NegativeZ = 1 << 4,
        PositiveZ = 1 << 5,
    }
}