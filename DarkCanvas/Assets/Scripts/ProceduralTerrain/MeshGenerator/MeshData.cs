using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Represents the mesh on a single terrain chunk.
    /// </summary>
    public class MeshData
    {
        private readonly bool _useFlatShading = false;

        private Vector3[] _vertices;
        private int[] _triangles;
        private Vector2[] _uvs;
        private Vector3[] _bakedNormals;
        private Vector3[] _borderVertices;
        private int[] _borderTriangles;
        private int _triangleIndex = 0;
        private int _borderTriangleIndex = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="verticesPerLine">Number of vertices per width or height of the square terrain chunk.</param>
        /// <param name="useFlatShading">Determines whether the mesh is shaded smooth or flat.</param>
        public MeshData(
            int verticesPerLine,
            bool useFlatShading)
        {
            _useFlatShading = useFlatShading;
            _vertices = new Vector3[verticesPerLine * verticesPerLine];
            _triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
            _uvs = new Vector2[verticesPerLine * verticesPerLine];
            _borderVertices = new Vector3[verticesPerLine * 4 + 4];
            _borderTriangles = new int[verticesPerLine * 24];
        }

        /// <summary>
        /// Adds a vertex to the terrain mesh.
        /// </summary>
        /// <param name="vertexPosition">Position of the vertex.</param>
        /// <param name="uv">UV map location of the vertex.</param>
        /// <param name="vertexIndex">Index of the vertex.</param>
        public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
        {
            if (vertexIndex < 0)
            {
                _borderVertices[-vertexIndex - 1] = vertexPosition;
            }
            else
            {
                _vertices[vertexIndex] = vertexPosition;
                _uvs[vertexIndex] = uv;
            }
        }

        /// <summary>
        /// Adds a triangle to the terrain mesh.
        /// </summary>
        /// <param name="a">Index of the mesh vertex for the first point of the triangle.</param>
        /// <param name="b">Index of the mesh vertex for the second point of the triangle.</param>
        /// <param name="c">Index of the mesh vertex for the third point of the triangle.</param>
        public void AddTriangle(int a, int b, int c)
        {
            if (a < 0 || b < 0 || c < 0)
            {
                _borderTriangles[_borderTriangleIndex] = a;
                _borderTriangles[_borderTriangleIndex + 1] = b;
                _borderTriangles[_borderTriangleIndex + 2] = c;
                _borderTriangleIndex += 3;
            }
            else
            {
                _triangles[_triangleIndex] = a;
                _triangles[_triangleIndex + 1] = b;
                _triangles[_triangleIndex + 2] = c;
                _triangleIndex += 3;
            }
        }

        /// <summary>
        /// Creates a Unity mesh object associated with this mesh data.
        /// </summary>
        public Mesh CreateMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = _vertices;
            mesh.triangles = _triangles;
            mesh.uv = _uvs;
            if (_useFlatShading)
            {
                //Flat shading normal recalculation is not that expensive.
                mesh.RecalculateNormals();
            }
            else
            {
                mesh.normals = _bakedNormals;
            }
            return mesh;
        }

        /// <summary>
        /// Calculates the normals for this mesh and stores them in memory.
        /// </summary>
        public void ProcessMesh()
        {
            if (_useFlatShading)
            {
                FlatShading();
            }
            else
            {
                BakeNormals();
            }
        }

        private void BakeNormals()
        {
            _bakedNormals = CalculateNormals();
        }

        private Vector3[] CalculateNormals()
        {
            var vertexNormals = new Vector3[_vertices.Length];
            var triangleCount = _triangles.Length / 3;

            for (var i = 0; i < triangleCount; i++)
            {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = _triangles[normalTriangleIndex];
                var vertexIndexB = _triangles[normalTriangleIndex + 1];
                var vertexIndexC = _triangles[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals[vertexIndexA] += triangleNormal;
                vertexNormals[vertexIndexB] += triangleNormal;
                vertexNormals[vertexIndexC] += triangleNormal;
            }

            var borderTriangleCount = _borderTriangles.Length / 3;
            for (var i = 0; i < borderTriangleCount; i++)
            {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = _borderTriangles[normalTriangleIndex];
                var vertexIndexB = _borderTriangles[normalTriangleIndex + 1];
                var vertexIndexC = _borderTriangles[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0)
                {
                    vertexNormals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0)
                {
                    vertexNormals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0)
                {
                    vertexNormals[vertexIndexC] += triangleNormal;
                }
            }

            for (var i = 0; i < vertexNormals.Length; i++)
            {
                vertexNormals[i].Normalize();
            }

            return vertexNormals;
        }

        private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
        {
            var pointA = (indexA < 0) ? _borderVertices[-indexA - 1] : _vertices[indexA];
            var pointB = (indexB < 0) ? _borderVertices[-indexB - 1] : _vertices[indexB];
            var pointC = (indexC < 0) ? _borderVertices[-indexC - 1] : _vertices[indexC];

            var sideAB = pointB - pointA;
            var sideAC = pointC - pointA;

            return Vector3.Cross(sideAB, sideAC).normalized;
        }

        private void FlatShading()
        {
            var flatShadedVertices = new Vector3[_triangles.Length];
            var flatShadedUvs = new Vector2[_triangles.Length];

            //For flat shading, each triangle 3 unique vertices not shared with any other triangle.
            for (var i = 0; i < _triangles.Length; i++)
            {
                flatShadedVertices[i] = _vertices[_triangles[i]];
                flatShadedUvs[i] = _uvs[_triangles[i]];
                _triangles[i] = i;
            }

            _vertices = flatShadedVertices;
            _uvs = flatShadedUvs;
        }
    }
}