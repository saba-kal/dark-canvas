using System;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents the level of detail of a terrain chunk mesh.
    /// </summary>
    [Serializable]
    public class LevelOfDetailInfo
    {
        /// <summary>
        /// The level of detail.
        /// </summary>
        [Range(0, MeshGenerator.NUMBER_OF_SUPPORTED_LODS - 1)]
        public int LevelOfDetail;

        /// <summary>
        /// How far the terrain chunk must be from the viewer to view this level of detail.
        /// </summary>
        public float VisibleDistanceThreshold;

        /// <summary>
        /// The square of VisibleDistanceThreshold
        /// </summary>
        public float SqrVisibleDstThreshold => VisibleDistanceThreshold * VisibleDistanceThreshold;
    }
}