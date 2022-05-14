using UnityEngine;

namespace DarkCanvas.Data.ProceduralTerrain
{
    /// <summary>
    /// Holds parameters for generating procedural terrains.
    /// </summary>
    [CreateAssetMenu(fileName = "MeshSettings", menuName = "ScriptableObjects/Mesh Settings")]
    public class MeshSettings : UpdatableData
    {
        [SerializeField] private bool _useFlatShading;
        [SerializeField] private bool _useVoxels;
        [SerializeField] private float _meshScale = 1.5f;
        [Range(0, NUMBER_OF_SUPPORTED_CHUNK_SIZES - 1)]
        [SerializeField] private int _chunkSizeIndex;
        [Range(0, NUMBER_OF_SUPPORTED_FLAT_SHADED_CHUNK_SIZES - 1)]
        [SerializeField] private int _flatShadedChunkSizeIndex;
        [Range(0, 40)]
        [SerializeField] private int _chunkSize3D;

        /// <summary>
        /// Total number of mesh details levels that are supported.
        /// This is limited based on how often we can divide the number of vertices in a mesh by 2.
        /// </summary>
        public const int NUMBER_OF_SUPPORTED_LODS = 5;

        /// <summary>
        /// Total number of chunk supported chunk sizes.
        /// This is limited based on being able to divide the number of vertices in the mesh by even numbers.
        /// and the fact that unity only supports meshes up to 255^2 (65025) vertices.
        /// </summary>
        public const int NUMBER_OF_SUPPORTED_CHUNK_SIZES = 9;

        /// <summary>
        /// Total number of chunk supported chunk sizes for flat shaded meshes. This is a lot less than
        /// to the normal supported chunk sizes because flat shaded meshes cannot share vertices between
        /// triangles. As a result, we have a lot more vertices and reach unity's 255^2 (65025) vertex
        /// limit much faster.
        /// </summary>
        public const int NUMBER_OF_SUPPORTED_FLAT_SHADED_CHUNK_SIZES = 3;

        /// <summary>
        /// List of supported chunk widths. Values are decided with the following considerations:
        /// - The max number of vertices that a mesh can have in Unity is 255^2 (65025).
        /// - Number of vertices on each edge must fit the following formula: vertices = width / lod + 1. 
        ///   lod controls the mesh level of detail. This value follows sequence of even numbers excluding 1 (no simplification).
        ///   For example, if we have a width of 240, it gives us the following: 
        ///   lod_1 = 241, lod_2 = 121, lod_4 = 61, lod_6 = 41, etc.
        /// </summary>
        public static int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

        /// <summary>
        /// Voxel chunk size is 16x16x16 because it makes level of details calculation easier.
        /// </summary>
        public const int VOXEL_CHUNK_SIZE = 16;

        /// <summary>
        /// Whether or not the terrain is shaded flat or smooth.
        /// </summary>
        public bool UseFlatShading => _useFlatShading;

        /// <summary>
        /// Generate mesh using 3D noise instead of a simple height map.
        /// </summary>
        public bool UseVoxels => _useVoxels;

        /// <summary>
        /// Scale multiplier for each terrain chunk game object.
        /// </summary>
        public float MeshScale => _meshScale;

        /// <summary>
        /// Chunk size to use when generating a mesh. This index maps to the SupportedChunkSizes array.
        /// </summary>
        public int CunkSizeIndex => _chunkSizeIndex;

        /// <summary>
        /// Chunk size to use when generating a flat-shaded mesh. This index maps to the SupportedFlatShadedChunkSizes array.
        /// </summary>
        public int FlatShadedChunkSizeIndex => _flatShadedChunkSizeIndex;

        /// <summary>
        /// The number of mesh vertices on each edge of a terrain chunk rendered at the highest level of detail (LOD = 0).
        /// 1 is added to account for the extra vertex on the start/end of each edge.
        /// </summary>
        public int NumberOfVerticesPerLine => SupportedChunkSizes[UseFlatShading ? _flatShadedChunkSizeIndex : _chunkSizeIndex] + 1;

        /// <summary>
        /// Represents width of the mesh in unity. Does not account for game object transform scale.
        /// By default, this would equal (vertices - 1) * scale because of the extra vertex on the start/end of each edge.
        /// However, we subtract 2 from this value to account for "bleed" edges when calculating normals.
        /// Basically, the generated meshes overlap each other by 1 vertex so that we can smoothly
        /// stitch them together. Without this, there would be noticeable seams between terrain chunks.
        /// </summary>
        public float MeshWorldSize
        {
            get
            {
                if (_useVoxels)
                {
                    return VOXEL_CHUNK_SIZE;
                }
                else
                {
                    return (NumberOfVerticesPerLine - 3) * _meshScale;
                }
            }
        }

        /// <summary>
        /// If using 3D noise to generate terrain, there is a separate chunk size.
        /// The reason is because adding a third dimension makes the noise generation algorithm
        /// orders of magnitude slower. Therefore, we need to limit the size when in 3D mode.
        /// </summary>
        public int ChunkSize3D => _chunkSize3D;
    }
}