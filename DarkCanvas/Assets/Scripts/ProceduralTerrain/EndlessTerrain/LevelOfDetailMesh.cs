using DarkCanvas.Data.ProceduralTerrain;
using System;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents a terrain mesh with a certain level of detail.
    /// </summary>
    public class LevelOfDetailMesh
    {
        public event Action UpdateCallback;
        public Mesh Mesh { get; private set; }
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh { get; private set; }

        private readonly int _levelOfDetail;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="levelOfDetail">
        /// Mesh level of detail. Highest detail level is 0.
        /// Subsequent detail levels (1, 2, 3, etc.) simplify the mesh.
        /// </param>
        public LevelOfDetailMesh(int levelOfDetail)
        {
            _levelOfDetail = levelOfDetail;
        }

        /// <summary>
        /// Starts a separate thread for generating this mesh.
        /// </summary>
        /// <param name="heightMap">Height map to use when creating the terrain mesh.</param>
        /// <param name="meshSettings">Display settings for this mesh.</param>
        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            HasRequestedMesh = true;
            ThreadedDataRequester.RequestData(
                () => MeshGenerator.GenerateTerrainMesh(heightMap.Values, meshSettings, _levelOfDetail),
                OnMeshDatReceived);
        }

        private void OnMeshDatReceived(object meshData)
        {
            Mesh = ((MeshData)meshData).CreateMesh();
            HasMesh = true;
            UpdateCallback();
        }
    }
}