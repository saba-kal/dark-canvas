using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    public class VoxelMeshGenerator
    {
        private readonly float[,,] _voxelData;
        private readonly int _size;
        private readonly CubeFaceDirection _chunkFacesWithLowerLod;
        private readonly Vector3Int _min;

        public VoxelMeshGenerator(
            float[,,] voxelData,
            int size,
            CubeFaceDirection chunkFacesWithLowerLod,
            Vector3Int min)
        {
            _voxelData = voxelData;
            _size = size;
            _chunkFacesWithLowerLod = chunkFacesWithLowerLod;
            _min = min;
        }

        /// <summary>
        /// Generates a terrain mesh using marching cubes.
        /// </summary>
        /// <returns>Object holding all data needed to create the mesh in Unity.</returns>
        public MeshData GenerateTerrainMesh()
        {
            var meshData = new MeshData(_size, false);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var vertices2Indices = new Dictionary<Vector3, int>();

            for (var x = 0; x < _size; x++)
            {
                for (var y = 0; y < _size; y++)
                {
                    for (var z = 0; z < _size; z++)
                    {
                        var pos = new Vector3Int(x, y, z);
                        var offsetPos = pos + _min;
                        GenerateRegularCell(pos, offsetPos, vertices, normals, triangles, vertices2Indices);
                    }
                }
            }

            for (var i = 0; i < 6; i++)
            {
                var direction = (CubeFaceDirection)(1 << i);
                if (!_chunkFacesWithLowerLod.HasFlag(direction))
                {
                    continue;
                }

                for (var x = 0; x < _size; x += 2)
                {
                    for (var y = 0; y < _size; y += 2)
                    {
                        var pos = ConvertChunkFaceCoordToVoxelCoord(x, y, direction);
                        var offsetPos = pos + _min;
                        GenerateTransitionCell(pos, offsetPos, vertices, normals, triangles, vertices2Indices, direction);
                    }
                }
            }

            meshData.SetVertices(vertices.ToArray());
            meshData.SetNormals(normals.ToArray());
            meshData.SetTriangles(triangles.ToArray());

            return meshData;
        }

        private Vector3Int ConvertChunkFaceCoordToVoxelCoord(
            int fx, int fy, CubeFaceDirection direction)
        {
            switch (direction)
            {
                case CubeFaceDirection.None:
                    return new Vector3Int(0, 0, 0);
                case CubeFaceDirection.NegativeX:
                    return new Vector3Int(0, fy, fx);
                case CubeFaceDirection.PositiveX:
                    return new Vector3Int(_size, fy, fx);
                case CubeFaceDirection.NegativeY:
                    return new Vector3Int(fx, 0, fy);
                case CubeFaceDirection.PositiveY:
                    return new Vector3Int(fx, _size, fy);
                case CubeFaceDirection.NegativeZ:
                    return new Vector3Int(fx, fy, 0);
                case CubeFaceDirection.PositiveZ:
                    return new Vector3Int(fx, fy, _size);
            }

            return new Vector3Int(fx, fy, 0);
        }

        private void GenerateRegularCell(
            Vector3Int pos,
            Vector3Int offsetPos,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> triangles,
            Dictionary<Vector3, int> vertices2Indices)
        {
            var density = GetCellCorners(pos.x, pos.y, pos.z);
            var caseCode = GetCaseCode(density);
            if (caseCode == 0 || caseCode == 255)
            {
                //Cell with case codes 0 and 255 contains no triangles.
                return;
            }

            var cornerNormals = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var p = offsetPos + cornerIndex[i];
                float nx = (this._voxelData[p.x + 1, p.y, p.z] - _voxelData[p.x - 1, p.y, p.z]) * 0.5f;
                float ny = (this._voxelData[p.x, p.y + 1, p.z] - _voxelData[p.x, p.y - 1, p.z]) * 0.5f;
                float nz = (_voxelData[p.x, p.y, p.z + 1] - _voxelData[p.x, p.y, p.z - 1]) * 0.5f;
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

                //TODO: figure out how the transvoxel vertex re-use algorithm works.
                //From what I have read, it depends on sequential data. Unity's z and y
                //coordinate invert is causing issues.

                //The edge is an 8-bit code where the left 4 bits indicates the reuse index
                //and the right 4 bits indicates the reuse direction.
                //byte vertexReuseData = (byte)(vertexLocation >> 8);
                //byte reuseIndex = (byte)(vertexReuseData & 0b_0000_1111); //Vertex id which should be created or reused 1,2 or 3.
                //byte reuseDir = (byte)(vertexReuseData >> 4); //the direction to go to reach a previous cell for reusing.

                //The corner indexes are stored in the low and high nibbles (4-bit values) of the edge code byte.
                byte cornerIndexes = (byte)(vertexLocation & 0b_0000_0000_1111_1111);
                byte v1 = (byte)(cornerIndexes & 0b_0000_1111); //Second Corner Index
                byte v0 = (byte)(cornerIndexes >> 4); //First Corner Index

                //Get the noise values at the corresponding cell corners.
                var d0 = density[v0];
                var d1 = density[v1];

                //Interpolate between the values.
                float t = d1 / (d1 - d0);
                float t0 = t;
                float t1 = 1f - t;

                var p0i = pos + cornerIndex[v0];
                var p0 = new Vector3(p0i.x, p0i.y, p0i.z);
                var p1i = pos + cornerIndex[v1];
                var p1 = new Vector3(p1i.x, p1i.y, p1i.z);
                var vertex = p0 * t0 + p1 * t1;
                var normal = cornerNormals[v0] * t0 + cornerNormals[v1] * t1;

                if (PositionShouldUseBePushedBack(p0i) || PositionShouldUseBePushedBack(p1i))
                {
                    vertex = GetSecondaryVertexPosition(vertex, normal, 0, 1);
                }

                int index;
                if (vertices2Indices.TryGetValue(vertex, out var cachedIndex))
                {
                    index = cachedIndex;
                }
                else
                {
                    normals.Add(normal);

                    vertices.Add(vertex);

                    index = vertices.Count - 1;
                    vertices2Indices.Add(vertex, index);
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
        }

        private void GenerateTransitionCell(
            Vector3Int pos,
            Vector3Int offsetPos,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> triangles,
            Dictionary<Vector3, int> vertices2Indices,
            CubeFaceDirection direction)
        {
            var cellSamples = GetTransitionCellSamples(pos.x, pos.y, pos.z, direction);
            var caseCode = GetTransitionCaseCode(cellSamples);

            if (caseCode == 0 || caseCode == 511)
            {
                //Transition cell with case codes 0 and 511 contains no triangles.
                return;
            }

            if (offsetPos.x == 15 && offsetPos.y == 15)
            {
                Debug.Log(cellSamples[12]);
            }

            var cellNormals = new Vector3[13];
            for (int i = 0; i < 9; i++)
            {
                var p = offsetPos + GetTransitionVertexIndices(direction)[i];
                //var p = offsetPos;
                float nx = (_voxelData[p.x + 1, p.y, p.z] - _voxelData[p.x - 1, p.y, p.z]) * 0.5f;
                float ny = (_voxelData[p.x, p.y + 1, p.z] - _voxelData[p.x, p.y - 1, p.z]) * 0.5f;
                float nz = (_voxelData[p.x, p.y, p.z + 1] - _voxelData[p.x, p.y, p.z - 1]) * 0.5f;
                cellNormals[i].x = nx;
                cellNormals[i].y = ny;
                cellNormals[i].z = nz;
                cellNormals[i].Normalize();
            }
            cellNormals[0x9] = cellNormals[0];
            cellNormals[0xA] = cellNormals[2];
            cellNormals[0xB] = cellNormals[6];
            cellNormals[0xC] = cellNormals[8];

            //High bit in cell class indicates whether we should flip the triangles.
            var cellClass = Transvoxel.transitionCellClass[caseCode];
            bool flipTriangles;
            if (direction == CubeFaceDirection.NegativeZ ||
                direction == CubeFaceDirection.PositiveX ||
                direction == CubeFaceDirection.PositiveY)
            {
                //No idea why I need to do this. If I don't, triangle will be inverted on certain faces.
                flipTriangles = (cellClass & 0b_1000_0000) == 0;
            }
            else
            {
                flipTriangles = (cellClass & 0b_1000_0000) != 0;
            }

            var cellData = Transvoxel.transitionCellData[cellClass & 0b_0111_1111];
            var vertexLocations = Transvoxel.transitionVertexData[caseCode];
            var vertexCount = cellData.GetVertexCount();
            var triangleCount = cellData.GetTriangleCount();
            var indexOffset = cellData.Indizes(); //index offsets for current cell
            var mappedIndizes = new ushort[indexOffset.Length]; //array with real indizes for current cell

            for (var i = 0; i < vertexCount; i++)
            {
                // The low byte contains the indexes for the two endpoints of the edge on which the vertex lies,
                // as numbered in Figure 3.7. The high byte contains the vertex reuse data shown in Figure 3.8.
                int vertexLocation = vertexLocations[i];

                //TODO: figure out how the transvoxel vertex re-use algorithm works.
                //From what I have read, it depends on sequential data. Unity's z and y
                //coordinate invert is causing issues.

                //The edge is an 8-bit code where the left 4 bits indicates the reuse index
                //and the right 4 bits indicates the reuse direction.
                //byte vertexReuseData = (byte)(vertexLocation >> 8);
                //byte reuseIndex = (byte)(vertexReuseData & 0b_0000_1111); //Vertex id which should be created or reused 1,2 or 3.
                //byte reuseDir = (byte)(vertexReuseData >> 4); //the direction to go to reach a previous cell for reusing.

                //The corner indexes are stored in the low and high nibbles (4-bit values) of the edge code byte.
                byte cornerIndexes = (byte)(vertexLocation & 0b_0000_0000_1111_1111);
                byte v1 = (byte)(cornerIndexes & 0b_0000_1111); //Second Corner Index
                byte v0 = (byte)(cornerIndexes >> 4); //First Corner Index

                //Get the noise values at the corresponding cell.
                var d0 = cellSamples[v0];
                var d1 = cellSamples[v1];

                //Interpolate between the values.
                float t = d1 / (d1 - d0);
                float t0 = t;
                float t1 = 1f - t;

                var isFullResSide = false;
                if (t > 0 && t < 1)
                {
                    //Vertex lies in the interior edge
                    isFullResSide = v1 < 9 || v0 < 9;
                }
                else
                {
                    // The vertex is exactly on one of the edge endpoints.
                    var cellIndex = t == 0 ? v1 : v0;
                    isFullResSide = cellIndex < 9;
                }

                var normal = cellNormals[v0] * t0 + cellNormals[v1] * t1;
                //var vertexPosition = GetTransitionVertexPosition(pos, t, v0, v1);

                var p0i = pos + GetTransitionVertexIndices(direction)[v0];
                var p0 = new Vector3(p0i.x, p0i.y, p0i.z);
                var p1i = pos + GetTransitionVertexIndices(direction)[v1];
                var p1 = new Vector3(p1i.x, p1i.y, p1i.z);
                var vertexPosition = p0 * t0 + p1 * t1;

                if (direction == CubeFaceDirection.NegativeZ)
                {
                    pos = new Vector3Int(pos.x, pos.y, pos.z);
                }

                if (isFullResSide && (PositionShouldUseBePushedBack(p0i) || PositionShouldUseBePushedBack(p1i)))
                {
                    vertexPosition = GetSecondaryVertexPosition(vertexPosition, normal, 0, 1);
                }

                int index;
                if (vertices2Indices.TryGetValue(vertexPosition, out var cachedIndex))
                {
                    index = cachedIndex;
                }
                else
                {
                    normals.Add(normal);
                    vertices.Add(vertexPosition);

                    index = vertices.Count - 1;
                    //vertices2Indices.Add(vertexPosition, index);
                }

                mappedIndizes[i] = (ushort)index;
            }

            for (int t = 0; t < triangleCount; t++)
            {
                if (flipTriangles)
                {
                    triangles.Add(mappedIndizes[cellData.Indizes()[t * 3 + 2]]);
                    triangles.Add(mappedIndizes[cellData.Indizes()[t * 3 + 1]]);
                    triangles.Add(mappedIndizes[cellData.Indizes()[t * 3]]);
                }
                else
                {
                    triangles.Add(mappedIndizes[cellData.Indizes()[t * 3]]);
                    triangles.Add(mappedIndizes[cellData.Indizes()[t * 3 + 1]]);
                    triangles.Add(mappedIndizes[cellData.Indizes()[t * 3 + 2]]);
                }
            }
        }

        private bool PositionShouldUseBePushedBack(Vector3Int pos)
        {
            var positionFaceMask = ConvertPositionToFaceMask(pos);
            if (positionFaceMask == 0 || !FaceTransitionsToLowerLod(positionFaceMask))
            {
                return false;
            }

            //Figure 4.13 in the paper shows cases where lod transitions should not cause
            //edge positions to be pushed back.
            if (FaceMaskIsEdge(positionFaceMask))
            {
                return ((int)_chunkFacesWithLowerLod & positionFaceMask) == positionFaceMask;
            }

            return true;
        }

        private int ConvertPositionToFaceMask(Vector3Int pos)
        {
            var faceMask = 0;
            faceMask |= (pos.x == 0 ? 1 : 0) << 0;
            faceMask |= (pos.x == _size ? 1 : 0) << 1;
            faceMask |= (pos.y == 0 ? 1 : 0) << 2;
            faceMask |= (pos.y == _size ? 1 : 0) << 3;
            faceMask |= (pos.z == 0 ? 1 : 0) << 4;
            faceMask |= (pos.z == _size ? 1 : 0) << 5;
            return faceMask;
        }

        private bool FaceTransitionsToLowerLod(int faceMask)
        {
            var transitionMask = (int)_chunkFacesWithLowerLod;
            if (transitionMask == 0)
            {
                return false;
            }

            return (transitionMask & faceMask) > 0;
        }

        private bool FaceMaskIsEdge(int positionFaceMask)
        {
            //Checks if the mask is not a power of 2. This tells us whether
            //or not 2 or more bits in the mask are set, which indicates that
            //the coordinates used to create the mask or on the edge of
            //the terrain chunk cube.
            return (positionFaceMask & (positionFaceMask - 1)) != 0;
        }


        //   6--------7
        //  /|       /|
        // / |      / |
        //4--------5  |
        //|  2-----|--3
        //| /      | /
        //|/       |/
        //0--------1
        private float[] GetCellCorners(int x, int y, int z)
        {
            //Figure 3.7 in https://www.transvoxel.org/Lengyel-VoxelTerrain.pdf
            return new float[8]
            {
                _voxelData[x, y, z],
                _voxelData[x + 1, y, z],
                _voxelData[x, y + 1, z],
                _voxelData[x + 1, y + 1, z],
                _voxelData[x, y, z + 1],
                _voxelData[x + 1, y, z + 1],
                _voxelData[x, y + 1, z + 1],
                _voxelData[x + 1, y + 1, z + 1],
            };
        }

        private static long GetCaseCode(float[] cellSamples)
        {
            //The sign bits of the eight corner sample values are concatenated to form the case index for a cell.
            //Listing 3.1
            return ((GetSignBit(cellSamples[0]) >> 7) & 0x01)
                | ((GetSignBit(cellSamples[1]) >> 6) & 0x02)
                | ((GetSignBit(cellSamples[2]) >> 5) & 0x04)
                | ((GetSignBit(cellSamples[3]) >> 4) & 0x08)
                | ((GetSignBit(cellSamples[4]) >> 3) & 0x10)
                | ((GetSignBit(cellSamples[5]) >> 2) & 0x20)
                | ((GetSignBit(cellSamples[6]) >> 1) & 0x40)
                | (GetSignBit(cellSamples[7]) & 0x80);
        }

        private static long GetTransitionCaseCode(float[] cellSamples)
        {
            //The sign bits of the eight corner sample values are concatenated to form the case index for a cell.
            //Figure 4.17
            return ((GetSignBit(cellSamples[0]) >> 8) & 0x01)
                | ((GetSignBit(cellSamples[1]) >> 7) & 0x02)
                | ((GetSignBit(cellSamples[2]) >> 6) & 0x04)
                | ((GetSignBit(cellSamples[3]) >> 5) & 0x80)
                | ((GetSignBit(cellSamples[4]) >> 4) & 0x100)
                | ((GetSignBit(cellSamples[5]) >> 3) & 0x08)
                | ((GetSignBit(cellSamples[6]) >> 2) & 0x40)
                | ((GetSignBit(cellSamples[7]) >> 1) & 0x20)
                | (GetSignBit(cellSamples[8]) & 0x10);
        }

        private static int GetSignBit(float value)
        {
            return value < 0 ? -1 : 0;
        }

        private Vector3 GetSecondaryVertexPosition(Vector3 primaryPosition, Vector3 normal, int lodIndex, int sign = 1)
        {
            var delta = GetBorderOffset(primaryPosition, lodIndex);
            delta = ProjectBorderOffset(delta, normal);
            return primaryPosition + delta * sign;
        }

        /// <summary>
        /// Implementation of equation 4.2 in the article.
        /// </summary>
        private Vector3 GetBorderOffset(Vector3 pos, int lodIndex)
        {
            var delta = new Vector3();

            var p2k = 1 << lodIndex; //2 ^ lod
            var p2mk = 1f / p2k; //2 ^ (-lod)

            const float transitionCellScale = 0.25f;
            var wk = transitionCellScale * p2k;

            for (var i = 0; i < 3; i++)
            {
                var p = pos[i];
                var s = _size - 1;

                if (p < p2k)
                {
                    delta[i] = (1f - p2mk * p) * wk;
                    //delta[i] = 0;
                }
                else if (p > (p2k * (s - 1)))
                {
                    delta[i] = (s - 1 - p2mk * p) * wk;
                    //delta[i] = ((p2k * s) - 1.0f - p) * wk;

                    //delta[i] = 0;
                }
                else
                {
                    delta[i] = 0;
                }
            }

            return delta;
        }

        /// <summary>
        /// Implementation of equation 4.3 in the article
        /// </summary>
        private static Vector3 ProjectBorderOffset(Vector3 delta, Vector3 normal)
        {
            var defaultProjection = new Vector3(
                (1 - normal.x * normal.x) * delta.x - normal.y * normal.x * delta.y - normal.z * normal.x * delta.z,
                -normal.x * normal.y * delta.x + (1 - normal.y * normal.y) * delta.y - normal.z * normal.y * delta.z,
                -normal.x * normal.z * delta.x - normal.y * normal.z * delta.y + (1 - normal.z * normal.z) * delta.z);

            var newProjection = new Vector3();
            newProjection.x = defaultProjection.z;
            newProjection.y = defaultProjection.y;
            newProjection.z = defaultProjection.x;

            return defaultProjection;
        }


        //   6--------7
        //  /|       /|
        // / |      / |
        //4--------5  |
        //|  2-----|--3
        //| /      | /
        //|/       |/
        //0--------1
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


        //  6---7---8  B-------C
        //  |   |   |  |       |
        //  3---4---5  |       |
        //  |   |   |  |       |
        //  0---1---2  9-------A
        private float[] GetTransitionCellSamples(int x, int y, int z, CubeFaceDirection direction)
        {
            var cellData = new float[13];
            var cellIndices = GetTransitionVertexIndices(direction);
            for (int i = 0; i < 13; i++)
            {
                var offset = cellIndices[i];
                cellData[i] = _voxelData[x + offset.x, y + offset.y, z + offset.z];
            }

            return cellData;
        }

        //  6---7---8  B-------C
        //  |   |   |  |       |
        //  3---4---5  |       |
        //  |   |   |  |       |
        //  0---1---2  9-------A
        private static Vector3Int[] GetTransitionVertexIndices(
            CubeFaceDirection direction)
        {
            return new Vector3Int[13] {
                ConvertTransitionCellCoordToVoxelCoord(0, 0, 0, direction), //0
                ConvertTransitionCellCoordToVoxelCoord(1, 0, 0, direction), //1
                ConvertTransitionCellCoordToVoxelCoord(2, 0, 0, direction), //2
                ConvertTransitionCellCoordToVoxelCoord(0, 1, 0, direction), //3
                ConvertTransitionCellCoordToVoxelCoord(1, 1, 0, direction), //4
                ConvertTransitionCellCoordToVoxelCoord(2, 1, 0, direction), //5
                ConvertTransitionCellCoordToVoxelCoord(0, 2, 0, direction), //6
                ConvertTransitionCellCoordToVoxelCoord(1, 2, 0, direction), //7
                ConvertTransitionCellCoordToVoxelCoord(2, 2, 0, direction), //8
                ConvertTransitionCellCoordToVoxelCoord(0, 0, 0, direction), //9
                ConvertTransitionCellCoordToVoxelCoord(2, 0, 0, direction), //A
                ConvertTransitionCellCoordToVoxelCoord(0, 2, 0, direction), //B
                ConvertTransitionCellCoordToVoxelCoord(2, 2, 0, direction) //C
            };
        }

        private static Vector3Int ConvertTransitionCellCoordToVoxelCoord(
            int tx, int ty, int tz, CubeFaceDirection direction)
        {
            //Transition cell could be oriented in one of three different ways depending on
            //the chunk face it is located on.
            switch (direction)
            {
                case CubeFaceDirection.NegativeX:
                case CubeFaceDirection.PositiveX:
                    return new Vector3Int(tz, ty, tx);
                case CubeFaceDirection.NegativeY:
                case CubeFaceDirection.PositiveY:
                    return new Vector3Int(tx, tz, ty);
                case CubeFaceDirection.NegativeZ:
                case CubeFaceDirection.PositiveZ:
                default:
                    return new Vector3Int(tx, ty, tz);
            }
        }
    }
}