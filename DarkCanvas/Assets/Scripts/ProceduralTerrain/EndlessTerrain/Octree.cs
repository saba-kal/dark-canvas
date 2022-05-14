using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Implementation of the octree algorithm.
    /// https://en.wikipedia.org/wiki/Octree
    /// </summary>
    public class Octree
    {
        private readonly Bounds _bounds;
        private readonly float _minNodeSize;

        private Octree[] _children;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bounds">Represents the cube bounds of this octree node.</param>
        /// <param name="minNodeSize">Minimum node size.</param>
        public Octree(Bounds bounds, float minNodeSize)
        {
            _bounds = bounds;
            _minNodeSize = minNodeSize;
        }

        /// <summary>
        /// Inserts a position in the octree, causing it to subdivide until a minimum size is reached.
        /// </summary>
        /// <param name="position">The insert position.</param>
        public void Insert(Vector3 position)
        {
            var dist = Vector3.Distance(_bounds.center, position) * 0.5f;
            if (dist < _bounds.size.x && _bounds.size.x > _minNodeSize)
            {
                CreateChildren(position);
            }
        }

        /// <summary>
        /// Gets a list of all the bounds of every octree leaf node.
        /// </summary>
        public List<Bounds> GetAllLeafBounds()
        {
            var bounds = new List<Bounds> { };
            if (_children == null)
            {
                //This is a leaf node.
                return new List<Bounds> { _bounds };
            }
            else
            {
                for (var i = 0; i < 8; i++)
                {
                    bounds.AddRange(_children[i].GetAllLeafBounds());
                }
            }
            return bounds;
        }

        private void CreateChildren(Vector3 position)
        {
            _children = new Octree[8];
            for (var i = 0; i < 8; i++)
            {
                _children[i] = new Octree(GetChildBoundsFromIndex(i), _minNodeSize);
                _children[i].Insert(position);
            }
        }

        //   6--------7
        //  /|       /|
        // / |      / |
        //4--------5  |
        //|  2-----|--3
        //| /      | /
        //|/       |/
        //0--------1
        private Bounds GetChildBoundsFromIndex(int i)
        {
            var childSize = _bounds.size / 2;
            var offset = childSize / 2;

            switch (i)
            {
                case 0:
                    offset *= -1;
                    break;
                case 1:
                    offset.y *= -1;
                    offset.z *= -1;
                    break;
                case 2:
                    offset.x *= -1;
                    offset.y *= -1;
                    break;
                case 3:
                    offset.y *= -1;
                    break;
                case 4:
                    offset.x *= -1;
                    offset.z *= -1;
                    break;
                case 5:
                    offset.z *= -1;
                    break;
                case 6:
                    offset.x *= -1;
                    break;
            }

            return new Bounds(_bounds.center + offset, childSize);
        }

        /// <summary>
        /// Draws wire cubes representing the entire octree.
        /// </summary>
        public void DrawGizmo()
        {
            Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            if (_children != null)
            {
                for (var i = 0; i < _children.Length; i++)
                {
                    _children[i].DrawGizmo();
                }
            }
        }
    }
}