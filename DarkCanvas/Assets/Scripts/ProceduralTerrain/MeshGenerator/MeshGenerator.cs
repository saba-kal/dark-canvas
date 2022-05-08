using DarkCanvas.Data.ProceduralTerrain;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Class for generating a terrain mesh.
    /// </summary>
    public static class MeshGenerator
    {
        /// <summary>
        /// Generates a terrain mesh.
        /// </summary>
        /// <param name="heightMap">2D array of heights for each mesh vertex.</param>
        /// <param name="settings">Display settings for the mesh.</param>
        /// <param name="levelOfDetail">
        /// Mesh level of detail. Highest detail level is 0.
        /// Subsequent detail levels (1, 2, 3, etc.) simplify the mesh.
        /// </param>
        /// <returns>Object holding all data needed to create the mesh in Unity.</returns>
        public static MeshData GenerateTerrainMesh(
            float[,] heightMap, MeshSettings settings, int levelOfDetail)
        {
            var meshSimplificationIncrement = levelOfDetail <= 0 ? 1 : levelOfDetail * 2;

            var borderedSize = heightMap.GetLength(0);
            var meshSize = borderedSize - 2 * meshSimplificationIncrement;
            var meshSizeUnsimplified = borderedSize - 2;

            var topLeftX = (meshSizeUnsimplified - 1) / -2f;
            var topLeftZ = (meshSizeUnsimplified - 1) / 2f;

            var verticiesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

            var meshData = new MeshData(verticiesPerLine, settings.UseFlatShading);

            var vertexIndicesMap = new int[borderedSize, borderedSize];
            var meshVertexIndex = 0;
            var borderVertexIndex = -1;

            for (var y = 0; y < borderedSize; y += meshSimplificationIncrement)
            {
                for (var x = 0; x < borderedSize; x += meshSimplificationIncrement)
                {
                    var isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                    if (isBorderVertex)
                    {
                        vertexIndicesMap[x, y] = borderVertexIndex;
                        borderVertexIndex--;
                    }
                    else
                    {
                        vertexIndicesMap[x, y] = meshVertexIndex;
                        meshVertexIndex++;
                    }
                }
            }

            for (var y = 0; y < borderedSize; y += meshSimplificationIncrement)
            {
                for (var x = 0; x < borderedSize; x += meshSimplificationIncrement)
                {
                    var vertexIndex = vertexIndicesMap[x, y];

                    var percent = new Vector2(
                        (x - meshSimplificationIncrement) / (float)meshSize,
                        (y - meshSimplificationIncrement) / (float)meshSize);
                    var vertexPosition = new Vector3(
                        (topLeftX + percent.x * meshSizeUnsimplified) * settings.MeshScale,
                        heightMap[x, y],
                        (topLeftZ - percent.y * meshSizeUnsimplified) * settings.MeshScale);

                    meshData.AddVertex(vertexPosition, percent, vertexIndex);

                    if (x < borderedSize - 1 && y < borderedSize - 1)
                    {
                        var a = vertexIndicesMap[x, y];
                        var b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                        var c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                        var d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }

            meshData.ProcessMesh();

            return meshData;
        }
    }
}