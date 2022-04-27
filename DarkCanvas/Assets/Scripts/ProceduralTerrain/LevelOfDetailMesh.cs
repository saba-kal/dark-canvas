using System;
using UnityEngine;

namespace DarkCanvas.Assets.Scripts.ProceduralTerrain
{
    public class LevelOfDetailMesh
    {
        public Mesh Mesh { get; private set; }
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh { get; private set; }

        private readonly int _levelOfDetail;
        private readonly MapGenerator _mapGenerator;
        private readonly Action _updateCallback;

        public LevelOfDetailMesh(
            int levelOfDetail,
            MapGenerator mapGenerator,
            Action updateCallback)
        {
            _levelOfDetail = levelOfDetail;
            _mapGenerator = mapGenerator;
            _updateCallback = updateCallback;
        }

        public void RequestMesh(MapData mapData)
        {
            HasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _levelOfDetail, OnMeshDatReceived);
        }

        private void OnMeshDatReceived(MeshData meshData)
        {
            Mesh = meshData.CreateMesh();
            HasMesh = true;
            _updateCallback();
        }
    }
}