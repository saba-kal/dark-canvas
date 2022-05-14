using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    //Copyright 2019 BinaryConstruct
    //Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
    //associated documentation files (the "Software"), to deal in the Software without restriction,
    //including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
    //and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
    //subject to the following conditions:
    //The above copyright notice and this permission notice shall be included in all copies or substantial
    //portions of the Software.
    //Referenc: https://github.com/BinaryConstruct/Transvoxel-XNA
    public class Cache
    {
        internal class ReuseCell
        {
            public readonly int[] Verts;

            public ReuseCell(int size)
            {
                Verts = new int[size];

                for (int i = 0; i < size; i++)
                    Verts[i] = -1;
            }
        }
        internal class RegularCellCache
        {
            private readonly ReuseCell[][] _cache;
            private int chunkSize;

            public RegularCellCache(int chunksize)
            {
                this.chunkSize = chunksize;
                _cache = new ReuseCell[2][];

                _cache[0] = new ReuseCell[chunkSize * chunkSize];
                _cache[1] = new ReuseCell[chunkSize * chunkSize];

                for (int i = 0; i < chunkSize * chunkSize; i++)
                {
                    _cache[0][i] = new ReuseCell(4);
                    _cache[1][i] = new ReuseCell(4);
                }
            }

            public ReuseCell GetReusedIndex(Vector3Int pos, byte rDir)
            {
                int rx = rDir & 0x01;
                int rz = (rDir >> 1) & 0x01;
                int ry = (rDir >> 2) & 0x01;

                int dx = pos.x - rx;
                int dy = pos.y - ry;
                int dz = pos.z - rz;

                return _cache[dx & 1][dy * chunkSize + dz];
            }


            public ReuseCell this[int x, int y, int z]
            {
                set
                {
                    _cache[x & 1][y * chunkSize + z] = value;
                }
            }

            public ReuseCell this[Vector3Int v]
            {
                set { this[v.x, v.y, v.z] = value; }
            }


            internal void SetReusableIndex(Vector3Int pos, byte reuseIndex, ushort p)
            {
                _cache[pos.x & 1][pos.y * chunkSize + pos.z].Verts[reuseIndex] = p;
            }
        }

        internal class TransitionCache
        {
            private readonly ReuseCell[] _cache;

            public TransitionCache()
            {
                const int cacheSize = 0;// 2 * TransvoxelExtractor.BlockWidth * TransvoxelExtractor.BlockWidth;
                _cache = new ReuseCell[cacheSize];

                for (int i = 0; i < cacheSize; i++)
                {
                    _cache[i] = new ReuseCell(12);
                }
            }

            public ReuseCell this[int x, int y]
            {
                get
                {
                    return null;//_cache[x + (y & 1) * TransvoxelExtractor.BlockWidth];
                }
                set
                {
                    //_cache[x + (y & 1) * TransvoxelExtractor.BlockWidth] = value;
                }
            }
        }
    }
}