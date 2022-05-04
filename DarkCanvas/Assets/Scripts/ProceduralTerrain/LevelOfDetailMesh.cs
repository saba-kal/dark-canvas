using System;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public class LevelOfDetailMesh
    {
        public event Action UpdateCallback;
        public Mesh Mesh { get; private set; }
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh { get; private set; }

        private readonly int _levelOfDetail;
        private readonly MapGenerator _mapGenerator;

        public LevelOfDetailMesh(
            int levelOfDetail,
            MapGenerator mapGenerator)
        {
            _levelOfDetail = levelOfDetail;
            _mapGenerator = mapGenerator;
        }

        public void RequestMesh(HeightMap mapData)
        {
            HasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _levelOfDetail, OnMeshDatReceived);
        }

        private void OnMeshDatReceived(MeshData meshData)
        {
            Mesh = meshData.CreateMesh();
            HasMesh = true;
            UpdateCallback();
        }
    }
}