using CrawfisSoftware.Maze;

namespace CrawfisSoftware.PCG.MazeBuilder
{
    public class LoopSampler<N, E>
    {
        private LoopSampler _loopSampler;
        private IMazeBuilder<N, E> _mazeBuilder;

        public LoopSampler(IMazeBuilder<N, E> mazeBuilder)
        {
            _loopSampler = new LoopSampler(mazeBuilder.Width, mazeBuilder.Height, new System.Random(mazeBuilder.RandomGenerator.Next()));
            _mazeBuilder = mazeBuilder;
        }

        public void Sample(bool preserveExistingCells = false)
        {
            //this.Clear();
            //var mazeBuilder = new MazeBuilderExplicit<int, int>(Width, Height, MazeBuilderUtility<int, int>.DummyNodeValues, MazeBuilderUtility<int, int>.DummyEdgeValues);
            var samplerGrid = _loopSampler.Sample();
            MazeWrapperFromGridBitArrays<N, E>.CarvePath(_mazeBuilder, samplerGrid.vertical, samplerGrid.horizontal);
        }
    }
}