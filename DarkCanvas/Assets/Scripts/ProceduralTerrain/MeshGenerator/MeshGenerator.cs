using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public static class MeshGenerator
    {
        public const int NUMBER_OF_SUPPORTED_LODS = 5;
        public const int NUMBER_OF_SUPPORTED_CHUNK_SIZES = 9;
        public const int NUMBER_OF_SUPPORTED_FLAT_SHADED_CHUNK_SIZES = 3;

        public static int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
        public static int[] SupportedFlatShadedChunkSizes = { 48, 72, 96 };

        public static MeshData GenerateTerrainMesh(
            MeshGeneratorParams meshGeneratorParams)
        {
            var threadSafeHeightCurve = new AnimationCurve(meshGeneratorParams.HeightCurve.keys);
            var meshSimplificationIncrement = meshGeneratorParams.LevelOfDetail <= 0 ? 1 : meshGeneratorParams.LevelOfDetail * 2;

            var borderedSize = meshGeneratorParams.HeightMap.GetLength(0);
            var meshSize = borderedSize - 2 * meshSimplificationIncrement;
            var meshSizeUnsimplified = borderedSize - 2;

            var topLeftX = (meshSizeUnsimplified - 1) / -2f;
            var topLeftZ = (meshSizeUnsimplified - 1) / 2f;

            var verticiesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

            var meshData = new MeshData(verticiesPerLine, meshGeneratorParams.UseFlatShading);

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
                    var height =
                        threadSafeHeightCurve.Evaluate(meshGeneratorParams.HeightMap[x, y]) *
                        meshGeneratorParams.HeightMultiplier;
                    var vertexPosition = new Vector3(
                        topLeftX + percent.x * meshSizeUnsimplified,
                        height,
                        topLeftZ - percent.y * meshSizeUnsimplified);

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