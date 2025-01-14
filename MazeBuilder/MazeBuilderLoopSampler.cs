using System;
using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;

namespace CrawfisSoftware.PCG.MazeBuilder
{
    public class MazeBuilderLoopSampler<N, E> : MazeBuilderAbstract<N, E>
    {
        private readonly LoopSampler _loopSampler;
        private readonly LoopSamplerCarryOverBit _loopSamplerCarryOverBit;
        private readonly int _columnWidth = 1;
        public MazeBuilderLoopSampler(int width, int height, GetGridLabel<N> nodeAccessor = null, GetEdgeLabel<E> edgeAccessor = null) : base(width, height, nodeAccessor, edgeAccessor)
        {
            (int tableWidth, int columnWidth) = DetermineOptimalTableWidth(width);
            if (columnWidth == 1 && tableWidth > 12)
            {
                throw new ArgumentException("Width must be Non-Prime if bigger than 12");
            }
            _columnWidth = columnWidth;
            if (columnWidth == 1)
            {
                _loopSampler = new LoopSampler(width, height, RandomGenerator);
            }
            else
            {
                _loopSamplerCarryOverBit = new LoopSamplerCarryOverBit(tableWidth, height, RandomGenerator);
            }
        }

        public MazeBuilderLoopSampler(MazeBuilderAbstract<N, E> mazeBuilder) : base(mazeBuilder)
        {
            (int tableWidth, int columnWidth) = DetermineOptimalTableWidth(mazeBuilder.Width);
            _columnWidth = columnWidth;
            Console.WriteLine($"table width: {tableWidth}, column width: {columnWidth}");
            if (columnWidth == 1)
            {
                _loopSampler = new LoopSampler(mazeBuilder.Width, mazeBuilder.Height, RandomGenerator);
            }
            else
            {
                _loopSamplerCarryOverBit = new LoopSamplerCarryOverBit(tableWidth, mazeBuilder.Height, RandomGenerator);
            }
        }

        public override void CreateMaze(bool preserveExistingCells = false)
        {
            this.Clear();
            var mazeBuilder = new MazeBuilderExplicit<int, int>(Width, Height, MazeBuilderUtility<int, int>.DummyNodeValues, MazeBuilderUtility<int, int>.DummyEdgeValues);
            
            if (_columnWidth == 1)
            {
                var samplerGrid = _loopSampler.Sample();
                MazeWrapperFromGridBitArrays<N,E>.CarvePath(this, samplerGrid.vertical, samplerGrid.horizontal);
            }
            else
            {
                var samplerGrid = _loopSamplerCarryOverBit.Sample(_columnWidth);
                MazeWrapperFromGridBitArrays<N,E>.CarvePath(this, samplerGrid.vertical, samplerGrid.horizontal);
            }

        }

        private (int tableWidth, int columnWidth) DetermineOptimalTableWidth(int width)
        {
            int[] tableWidths = new[] { 12, 11, 10, 9, 8, 7, 6, 5, 4, 3 };
            int finalTableWidth = width;
            int finalColumnWidth = 1;

            if (width > 12)
            {
                foreach (int tableWidth in tableWidths)
                {
                    if (width % tableWidth == 0)
                    {
                        finalTableWidth = tableWidth;
                        finalColumnWidth = width / tableWidth;
                        break;
                    }
                }
            }
            return (finalTableWidth, finalColumnWidth);
        }
    }
}