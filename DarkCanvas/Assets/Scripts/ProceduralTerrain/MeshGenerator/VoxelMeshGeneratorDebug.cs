using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public class VoxelMeshGeneratorDebug : MonoBehaviour
    {
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;

        private Vector3Int _currentPosition;

        public void Generate(
            int[,,] voxelData,
            int size,
            Vector3Int min)
        {
            StopAllCoroutines();
            StartCoroutine(GenerateTerrainMesh(voxelData, size, min));
        }

        /// <summary>
        /// Generates a terrain mesh using marching cubes.
        /// </summary>
        /// <param name="voxelData">3D array of noise values.</param>
        /// <returns>Object holding all data needed to create the mesh in Unity.</returns>
        public IEnumerator GenerateTerrainMesh(
            int[,,] voxelData,
            int size,
            Vector3Int min)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var cache = new Cache.RegularCellCache(size * 10);

            _meshFilter.gameObject.SetActive(true);

            for (var z = 0; z < size; z++)
            {
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var pos = new Vector3Int(x, y, z);
                        _currentPosition = pos;
                        var offsetPos = pos + min;

                        byte directionMask = (byte)((pos.x > 0 ? 1 : 0) | ((pos.z > 0 ? 1 : 0) << 1) | ((pos.y > 0 ? 1 : 0) << 2));
                        var density = GetCellCorners(offsetPos.x, offsetPos.y, offsetPos.z, voxelData);
                        var caseCode = GetCaseCode(density);
                        if (caseCode == 0 || caseCode == 255)
                        {
                            //Cell with case codes 0 and 255 contains no triangles.
                            yield return new WaitForSeconds(1f);
                            continue;
                        }

                        var cornerNormals = new Vector3[8];
                        for (int i = 0; i < 8; i++)
                        {
                            var p = offsetPos + cornerIndex[i];
                            float nx = (voxelData[p.x + 1, p.y, p.z] - voxelData[p.x - 1, p.y, p.z]) * 0.5f;
                            float ny = (voxelData[p.x, p.y + 1, p.z] - voxelData[p.x, p.y - 1, p.z]) * 0.5f;
                            float nz = (voxelData[p.x, p.y, p.z + 1] - voxelData[p.x, p.y, p.z - 1]) * 0.5f;
                            cornerNormals[i].x = nx;
                            cornerNormals[i].y = ny;
                            cornerNormals[i].z = nz;
                            cornerNormals[i].Normalize();
                        }

                        var cellClass = Transvoxel.regularCellClass[caseCode];
                        var cellData = Transvoxel.regularCellData[cellClass];
                        var vertexLocations = Transvoxel.regularVertexData[caseCode];
                        var vertexCount = cellData.GetVertexCount();
                        var triangleCount = cellData.GetTriangleCount();
                        var indexOffset = cellData.Indizes(); //index offsets for current cell
                        var mappedIndizes = new ushort[indexOffset.Length]; //array with real indizes for current cell

                        for (var i = 0; i < vertexCount; i++)
                        {
                            // The low byte contains the indexes for the two endpoints of the edge on which the vertex lies,
                            // as numbered in Figure 3.7. The high byte contains the vertex reuse data shown in Figure 3.8.
                            int vertexLocation = vertexLocations[i];

                            //The edge is an 8-bit code where the left 4 bits indicates the reuse index
                            //and the right 4 bits indicates the reuse direction.
                            byte vertexReuseData = (byte)(vertexLocation >> 8);
                            byte reuseIndex = (byte)(vertexReuseData & 0b_0000_1111); //Vertex id which should be created or reused 1,2 or 3.
                            byte reuseDir = (byte)(vertexReuseData >> 4); //the direction to go to reach a previous cell for reusing.

                            //The corner indexes are stored in the low and high nibbles (4-bit values) of the edge code byte.
                            byte cornerIndexes = (byte)(vertexLocation & 0b_0000_0000_1111_1111);
                            byte v1 = (byte)(cornerIndexes & 0b_0000_1111); //Second Corner Index
                            byte v0 = (byte)(cornerIndexes >> 4); //First Corner Index

                            //Get the noise values at the corresponding cell corners.
                            var d0 = density[v0];
                            var d1 = density[v1];

                            //Interpolate between the values.
                            long t = (d1 << 8) / (d1 - d0);
                            long u = 0x0100 - t;
                            float t0 = t / 256f;
                            float t1 = u / 256f;
                            int index = -1;

                            if (v1 != 7 && (reuseDir & directionMask) == reuseDir)
                            {
                                var cell = cache.GetReusedIndex(pos, reuseDir);
                                index = cell.Verts[reuseIndex];
                                if (index != -1)
                                {
                                    Debug.Log("reuse");
                                }
                            }

                            if (index == -1)
                            {
                                var normal = cornerNormals[v0] * t0 + cornerNormals[v1] * t1;
                                normals.Add(normal);

                                var vertex = GenerateVertex(ref pos, t, ref v0, ref v1);
                                vertices.Add(vertex);

                                index = vertices.Count - 1;
                            }

                            if ((reuseDir & 8) != 0)
                            {
                                cache.SetReusableIndex(pos, reuseIndex, (ushort)(vertices.Count - 1));
                            }

                            mappedIndizes[i] = (ushort)index;
                        }

                        for (int t = 0; t < triangleCount; t++)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                triangles.Add(mappedIndizes[cellData.Indizes()[t * 3 + i]]);
                            }
                        }

                        mesh.vertices = vertices.ToArray();
                        mesh.normals = normals.ToArray();
                        mesh.triangles = triangles.ToArray();
                        _meshFilter.sharedMesh = mesh;

                        yield return new WaitForSeconds(1f);
                        //yield return null;
                    }
                }
            }

            yield return null;
        }

        //   6--------7
        //  /|       /|
        // / |      / |
        //4--------5  |
        //|  2-----|--3
        //| /      | /
        //|/       |/
        //0--------1
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

        private static Vector3 GenerateVertex(ref Vector3Int pos, long t, ref byte v0, ref byte v1)
        {
            var iP0 = pos + cornerIndex[v0];
            var P0 = new Vector3(iP0.x, iP0.y, iP0.z);

            var iP1 = pos + cornerIndex[v1];
            var P1 = new Vector3(iP1.x, iP1.y, iP1.z);

            return InterpolateVoxelVector(t, P0, P1);
        }

        private static Vector3 InterpolateVoxelVector(long t, Vector3 P0, Vector3 P1)
        {
            long u = 0x0100 - t; //256 - t
            float s = 1.0f / 256.0f;
            Vector3 Q = P0 * t + P1 * u; //Density Interpolation
            Q *= s; // shift to shader ! 
            return Q;
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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_currentPosition + Vector3.one / 2f, Vector3.one);
        }
    }
}