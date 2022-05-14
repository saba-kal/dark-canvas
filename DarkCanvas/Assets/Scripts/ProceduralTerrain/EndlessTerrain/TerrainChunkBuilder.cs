using System.Collections.Generic;

namespace DarkCanvas.ProceduralTerrain
{
    public class TerrainChunkBuilder
    {
        private List<VoxelTerrainChunk> _terrainChunksToBuild;

        public TerrainChunkBuilder(List<VoxelTerrainChunk> terrainChunksToBuild)
        {
            _terrainChunksToBuild = terrainChunksToBuild;
        }

        public void BuildTerrainChunks()
        {
            ThreadedDataRequester.RequestData(GenerateChunks, OnChunksGenerated);
        }

        private object GenerateChunks()
        {
            foreach (var terrainChunk in _terrainChunksToBuild)
            {
                terrainChunk.GenerateChunk();
            }

            return null;
        }

        private void OnChunksGenerated(object _)
        {
            foreach (var terrainChunk in _terrainChunksToBuild)
            {
                terrainChunk.BuildChunk();
            }
        }
    }
}