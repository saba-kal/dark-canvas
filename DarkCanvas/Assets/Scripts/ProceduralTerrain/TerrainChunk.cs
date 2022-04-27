using UnityEngine;

namespace DarkCanvas.Assets.Scripts.ProceduralTerrain
{
    public class TerrainChunk
    {
        private readonly GameObject _meshObject;
        private readonly Vector2 _position;
        private readonly Bounds _bounds;
        private readonly MapGenerator _mapGenerator;


        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;
        private readonly LevelOfDetailInfo[] _detailLevels;
        private readonly LevelOfDetailMesh[] _levelOfDetailMeshes;

        private MapData _mapData;
        private bool _mapDataRecieved;
        private int _previousLevelOfDetailIndex = -1;

        public TerrainChunk(
            Vector2 coord,
            int size,
            LevelOfDetailInfo[] detailLevels,
            Transform parent,
            MapGenerator mapGenerator,
            Material material)
        {
            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            _detailLevels = detailLevels;
            _mapGenerator = mapGenerator;

            _meshObject = new GameObject("Terrain Chunk");
            _meshObject.transform.position = new Vector3(_position.x, 0, _position.y) * EndlessTerrain.SCALE;
            _meshObject.transform.parent = parent;
            _meshObject.transform.localScale = Vector3.one * EndlessTerrain.SCALE;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _meshCollider = _meshObject.AddComponent<MeshCollider>();

            SetVisible(false);

            _levelOfDetailMeshes = new LevelOfDetailMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                _levelOfDetailMeshes[i] = new LevelOfDetailMesh(detailLevels[i].LevelOfDetail, _mapGenerator, Update);
            }

            _mapGenerator.RequestMapData(_position, OnMapDatReceived);
        }

        public void Update()
        {
            if (!_mapDataRecieved)
            {
                return;
            }

            var viewerDstFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(EndlessTerrain.ViewerPosition));
            var visible = viewerDstFromNearestEdge <= EndlessTerrain.MaxViewDistance;

            if (visible)
            {
                var levelOfDetailIndex = 0;
                for (var i = 0; i < _detailLevels.Length - 1; i++)
                {
                    if (viewerDstFromNearestEdge > _detailLevels[i].VisibileDistanceThreshold)
                    {
                        levelOfDetailIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (levelOfDetailIndex != _previousLevelOfDetailIndex)
                {
                    var levelOfDetailMesh = _levelOfDetailMeshes[levelOfDetailIndex];
                    if (levelOfDetailMesh.HasMesh)
                    {
                        _previousLevelOfDetailIndex = levelOfDetailIndex;
                        _meshFilter.mesh = levelOfDetailMesh.Mesh;
                        _meshCollider.sharedMesh = levelOfDetailMesh.Mesh;
                    }
                    else if (!levelOfDetailMesh.HasRequestedMesh)
                    {
                        levelOfDetailMesh.RequestMesh(_mapData);
                    }
                }

                EndlessTerrain.TerrainChunksVisibleLastUpdate.Add(this);
            }

            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }

        private void OnMapDatReceived(MapData mapData)
        {
            _mapData = mapData;
            _mapDataRecieved = true;

            var texture = TextureGenerator.TextureFromColourMap(
                mapData.ColorMap, MapGenerator.MAP_CHUNK_SIZE, MapGenerator.MAP_CHUNK_SIZE);
            _meshRenderer.material.mainTexture = texture;

            Update();
        }
    }
}