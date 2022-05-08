using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public static class VoxelMeshGenerator
    {
        private const float s = 1f / 256f;

        /// <summary>
        /// Generates a terrain mesh using marching cubes.
        /// </summary>
        /// <param name="voxelData">3D array of noise values.</param>
        /// <param name="settings">Display settings for the mesh.</param>
        /// <returns>Object holding all data needed to create the mesh in Unity.</returns>
        public static MeshData GenerateTerrainMesh(
            int[,,] voxelData)
        {
            var depth = voxelData.GetLength(2);
            var height = voxelData.GetLength(1);
            var width = voxelData.GetLength(0);
            var meshData = new MeshData(depth, true);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var localVertexMapping = new ushort[15];

            //Generating the triangle mesh for one block requires access to a volume of 19x19x19 voxels, where one layer
            //of voxels precedes the negative boundaries of th e block, and two layers of voxels succeed the
            //positive boundaries of the block. This is why we start at 1 and end at length - 2.
            for (var z = 0; z < depth - 1; z++)
            {
                for (var y = 0; y < height - 1; y++)
                {
                    for (var x = 0; x < width - 1; x++)
                    {
                        var corners = GetCellCorners(x, y, z, voxelData);
                        var caseCode = GetCaseCode(corners);

                        if (caseCode == 0 || caseCode == 255)
                        {
                            //Cell with case codes 0 and 255 contains no triangles.
                            continue;
                        }

                        var cellClass = Transvoxel.regularCellClass[caseCode];
                        var cellData = Transvoxel.regularCellData[cellClass];
                        var vertexLocations = Transvoxel.regularVertexData[caseCode];
                        var vertexCount = cellData.GetVertexCount();
                        var triangleCount = cellData.GetTriangleCount();

                        for (var i = 0; i < vertexCount; i++)
                        {
                            //Edge is stored in the lower byte of a 16-bit value, hence the 255 mask.
                            var edgeCode = vertexLocations[i] & 0b_0000_0000_1111_1111;

                            //The corner indexes are stored in the low and high nibbles (4-bit values) of the edge code byte. 
                            var v0 = (edgeCode >> 4) & 0b_0000_1111; //First Corner Index
                            var v1 = edgeCode & 0b_0000_1111; //Second Corner Index

                            //Get the noise values at the corresponding cell corners.
                            var d0 = corners[v0];
                            var d1 = corners[v1];

                            //Interpolate between the values.
                            var t = (d1 << 8) / (d1 - d0);
                            var u = 0x0100 - t;

                            var p0 = new Vector3((x + cornerIndex[v0].x) * t, (y + cornerIndex[v0].y) * t, (z + cornerIndex[v0].z) * t);
                            var p1 = new Vector3((x + cornerIndex[v1].x) * u, (y + cornerIndex[v1].y) * u, (z + cornerIndex[v1].z) * u);
                            vertices.Add(new Vector3((p0.x + p1.x) * s, (p0.y + p1.y) * s, (p0.z + p1.z) * s));

                            localVertexMapping[i] = (ushort)(vertices.Count - 1);
                        }

                        for (int t = 0; t < triangleCount; t++)
                        {
                            int tm = t * 3;
                            triangles.Add(localVertexMapping[cellData.vertexIndex[tm]]);
                            triangles.Add(localVertexMapping[cellData.vertexIndex[tm + 1]]);
                            triangles.Add(localVertexMapping[cellData.vertexIndex[tm + 2]]);
                        }
                    }
                }
            }

            meshData.SetVertices(vertices.ToArray());
            meshData.SetTriangles(triangles.ToArray());

            return meshData;
        }

        private static int[] GetCellCorners(int x, int y, int z, int[,,] voxelData)
        {
            //Figure 3.7 in https://www.transvoxel.org/Lengyel-VoxelTerrain.pdf
            return new int[8]
            {
                voxelData[x, y, z],
                voxelData[x + 1, y, z],
                voxelData[x, y + 1, z],
                voxelData[x + 1, y + 1, z],
                voxelData[x, y, z + 1],
                voxelData[x + 1, y, z + 1],
                voxelData[x, y + 1, z + 1],
                voxelData[x + 1, y + 1, z + 1],
            };
        }

        private static long GetCaseCode(int[] corners)
        {
            //The sign bits of the eight corner sample values are concatenated to form the case index for a cell.
            return ((corners[0] >> 7) & 0x01)
                | ((corners[1] >> 6) & 0x02)
                | ((corners[2] >> 5) & 0x04)
                | ((corners[3] >> 4) & 0x08)
                | ((corners[4] >> 3) & 0x10)
                | ((corners[5] >> 2) & 0x20)
                | ((corners[6] >> 1) & 0x40)
                | (corners[7] & 0x80);
        }

        private static void AppendCubeMesh(int x, int y, int z, Mesh mesh, ref int vertexIndex)
        {
            Vector3[] vertices = {
                new Vector3 (x, y, z),
                new Vector3 (x + 1, y, z),
                new Vector3 (x + 1, y + 1, z),
                new Vector3 (x, y + 1, z),
                new Vector3 (x, y + 1, z + 1),
                new Vector3 (x + 1, y + 1, z + 1),
                new Vector3 (x + 1, y, z + 1),
                new Vector3 (x, y, z + 1),
            };

            int[] triangles = {
                vertexIndex, vertexIndex + 2, vertexIndex + 1, //face front
                vertexIndex, vertexIndex + 3, vertexIndex + 2,
                vertexIndex + 2, vertexIndex + 3, vertexIndex + 4, //face top
                vertexIndex + 2, vertexIndex + 4, vertexIndex + 5,
                vertexIndex + 1, vertexIndex + 2, vertexIndex + 5, //face right
                vertexIndex + 1, vertexIndex + 5, vertexIndex + 6,
                vertexIndex + 0, vertexIndex + 7, vertexIndex + 4, //face left
                vertexIndex + 0, vertexIndex + 4, vertexIndex + 3,
                vertexIndex + 5, vertexIndex + 4, vertexIndex + 7, //face back
                vertexIndex + 5, vertexIndex + 7, vertexIndex + 6,
                vertexIndex + 0, vertexIndex + 6, vertexIndex + 7, //face bottom
                vertexIndex + 0, vertexIndex + 1, vertexIndex + 6
            };

            vertexIndex += 8;

            var resultVertices = new List<Vector3>();
            resultVertices.AddRange(mesh.vertices);
            resultVertices.AddRange(vertices);
            mesh.vertices = resultVertices.ToArray();

            var resultTriangles = new List<int>();
            resultTriangles.AddRange(mesh.triangles);
            resultTriangles.AddRange(triangles);
            mesh.triangles = resultTriangles.ToArray();
        }

        private static readonly Vector3Int[] cornerIndex = new Vector3Int[8] {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 1),
            new Vector3Int(0, 1, 1),
            new Vector3Int(1, 1, 1)
        };
    }
}