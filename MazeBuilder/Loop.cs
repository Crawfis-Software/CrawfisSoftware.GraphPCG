using System;
using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Maze;
using CrawfisSoftware.Path;

namespace CrawfisSoftware.PCG.MazeBuilder
{
    /// <summary>
    /// Extensions for IMazeBuilder for loop and path sampling algorithm.
    /// </summary>
    // todo: .Net Standard 3.0 and .Net 8.0 support partial static classes.
    // When Unity supports 3.0 we can make these partial and have the name w/o numbers.
    public static /*partial*/ class Loop
    {
        private static LoopSampler _loopSampler;
        private static LoopSamplerCarryOverBit _loopSamplerCarryOverBit;
        private static int _columnWidth = 1;

        /// <summary>
        /// Create a loop maze using the loop sweeping algorithm and may take a while.
        /// </summary>
        /// <param name="mazeBuilder">A maze builder</param>
        /// <param name="preserveExistingCells">Boolean indicating whether to only replace maze cells that are undefined.
        /// Default is false.</param>
        /// <typeparam name="N">The type used for node labels</typeparam>
        /// <typeparam name="E">The type used for edge weights</typeparam>
        public static void CreateLoop<N,E>(this IMazeBuilder<N,E> mazeBuilder, bool preserveExistingCells = false)
        {
            CheckTable(mazeBuilder);
            if (_columnWidth == 1)
            {
                var samplerGrid = _loopSampler.Sample();
                MazeWrapperFromGridBitArrays<N,E>.CarvePath(mazeBuilder, samplerGrid.vertical, samplerGrid.horizontal);
            }
            else
            {
                var samplerGrid = _loopSamplerCarryOverBit.Sample(_columnWidth);
                MazeWrapperFromGridBitArrays<N,E>.CarvePath(mazeBuilder, samplerGrid.vertical, samplerGrid.horizontal);
            }

        }

        private static (int tableWidth, int columnWidth) DetermineOptimalTableWidth(int width)
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

        private static void CheckTable<N,E>(IMazeBuilder<N,E> mazeBuilder)
        {
            (int tableWidth, int columnWidth) = DetermineOptimalTableWidth(mazeBuilder.Width);
            if (columnWidth == 1 && tableWidth > 12)
            {
                throw new ArgumentException("Width must be Non-Prime if bigger than 12 for loop");
            }
            _columnWidth = columnWidth;
            if (columnWidth == 1 && (_loopSampler == null || mazeBuilder.Width != _loopSampler.GetWidth()))
            {
                _loopSampler = new LoopSampler(mazeBuilder.Width, mazeBuilder.Height, mazeBuilder.RandomGenerator);
            }
            else
            {
                if (_loopSamplerCarryOverBit == null || mazeBuilder.Width != _loopSamplerCarryOverBit.GetWidth())
                {
                    _loopSamplerCarryOverBit =
                        new LoopSamplerCarryOverBit(tableWidth, mazeBuilder.Height, mazeBuilder.RandomGenerator);
                }
            }
        }
    }
}