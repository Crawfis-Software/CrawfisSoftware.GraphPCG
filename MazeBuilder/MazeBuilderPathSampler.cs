using System;
using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using CrawfisSoftware.PCG;

namespace CrawfisSoftware.PCG.MazeBuilder
{
    public class MazeBuilderPathSampler<N, E> : MazeBuilderAbstract<N, E>
    {
        private PathSamplerBottomToTop _pathSampler;
        public MazeBuilderPathSampler(int width, int height, GetGridLabel<N> nodeAccessor = null, GetEdgeLabel<E> edgeAccessor = null) : base(width, height, nodeAccessor, edgeAccessor)
        {
            _pathSampler = new PathSamplerBottomToTop(width, height, RandomGenerator);
        }

        public MazeBuilderPathSampler(MazeBuilderAbstract<N, E> mazeBuilder) : base(mazeBuilder)
        {
            _pathSampler = new PathSamplerBottomToTop(Width, Height, RandomGenerator);
        }

        public override void CreateMaze(bool preserveExistingCells = false)
        { 
            this.Clear();
            var samplerGrid = _pathSampler.Sample(StartCell, EndCell);
            MazeWrapperFromGridBitArrays<N,E>.CarvePath(this, samplerGrid.vertical, samplerGrid.horizontal);
        }
    }
}