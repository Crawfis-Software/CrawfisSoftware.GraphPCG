using System;
using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using CrawfisSoftware.PCG;

namespace CrawfisSoftware.PCG.MazeBuilder
{
    public class MazeBuilderLoopSampler<N, E> : MazeBuilderAbstract<N, E>
    {
        private LoopSampler _loopSampler;
        public MazeBuilderLoopSampler(int width, int height, GetGridLabel<N> nodeAccessor = null, GetEdgeLabel<E> edgeAccessor = null) : base(width, height, nodeAccessor, edgeAccessor)
        {
            _loopSampler = new LoopSampler(width, height, RandomGenerator);
        }

        public MazeBuilderLoopSampler(MazeBuilderAbstract<N, E> mazeBuilder) : base(mazeBuilder)
        {
            _loopSampler = new LoopSampler(Width, Height, RandomGenerator);
        }

        public override void CreateMaze(bool preserveExistingCells = false)
        {
            this.Clear();
            var mazeBuilder = new MazeBuilderExplicit<int, int>(Width, Height, MazeBuilderUtility<int, int>.DummyNodeValues, MazeBuilderUtility<int, int>.DummyEdgeValues);
            var samplerGrid = _loopSampler.Sample();
            MazeWrapperFromGridBitArrays<N,E>.CarvePath(this, samplerGrid.vertical, samplerGrid.horizontal);
        }
    }
}