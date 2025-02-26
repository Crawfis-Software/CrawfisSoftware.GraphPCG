using CrawfisSoftware.Maze;

namespace CrawfisSoftware.PCG.MazeBuilder
{
    public class PathSampler<N, E>
    {
        private PathSamplerBottomToTop _pathSampler;
        private IMazeBuilder<N, E> _mazeBuilder;

        public PathSampler(IMazeBuilder<N, E> mazeBuilder)
        {
            _mazeBuilder = mazeBuilder;
            _pathSampler = new PathSamplerBottomToTop(mazeBuilder.Width, mazeBuilder.Height, new System.Random(mazeBuilder.RandomGenerator.Next()));
        }

        public void Sample(bool preserveExistingCells = false)
        {
            var samplerGrid = _pathSampler.Sample(_mazeBuilder.StartCell, _mazeBuilder.EndCell);
            MazeWrapperFromGridBitArrays<N, E>.CarvePath(_mazeBuilder, samplerGrid.vertical, samplerGrid.horizontal);
        }
    }
}