using CrawfisSoftware.Collections.Graph;

using System;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// CellularAutomata that allows a Kernel to be passed in. This allows for the N-neighbors, weights, etc. where the original algorithm is a box filter / counter.
    /// </summary>
    public class CellularAutomataExtended : CellularAutomata
    {
        /// <summary>
        /// Get or set the kernel weights. These values will be added together for those cells that are true.
        /// </summary>
        public float[,] KernelWeights { get; set; }
        /// <summary>
        /// The Predicate Function that determines whether to change the cell. It takes in parameters as in the example:
        /// <c>private static bool AlwaysTrue(int column, int row, int iteration, int numberTrue, int numberOfNeighbors)</c>
        /// </summary>
        public Func<int, int, int, bool, float, int, int, bool> ComputeCell { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="occupancyGrid">An initial state for the cellular automata.</param>
        /// <param name="random">An optional random number generator.</param>
        public CellularAutomataExtended(OccupancyGrid occupancyGrid, Random random = null) : base(occupancyGrid, random)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width">The number of columns for the new occupancy grid.</param>
        /// <param name="height">The number of rows for the new occupancy grid.</param>
        /// <param name="random">An optional random number generator.</param>
        public CellularAutomataExtended(int width, int height, Random random = null) : base(width, height, random)
        {
        }

        /// <summary>
        /// Iterate over the grid the specified number of times, computing the convolution of the kernel with the occupancy grid and calling the ComputeCell function.
        /// </summary>
        /// <param name="numberOfIterations">The number of times to apply the decision process for each cell.</param>
        public void IterateFiltered(int numberOfIterations = 1)
        {
            for (int iteration = 0; iteration < numberOfIterations; iteration++)
            {
                PreIterationFunc(iteration);
                for (int column = 0; column < Width; column++)
                {
                    for (int row = 0; row < Height; row++)
                    {
                        float filterValue = GetFilteredValue(column, row, out int numberTrue, out int numberOfNeighbors);
                        bool newValue = ComputeCell(column, row, iteration, _occupancyGridFrontBuffer.GetNodeLabel(column, row), filterValue, numberTrue, numberOfNeighbors);
                        _occupancyGridBackBuffer.MarkCell(column, row, newValue);
                    }
                }
                PostIterationFunc(iteration);
                var tmp = _occupancyGridFrontBuffer;
                _occupancyGridFrontBuffer = _occupancyGridBackBuffer;
                _occupancyGridBackBuffer = tmp;
            }
        }

        /// <summary>
        /// Applies the convolution.
        /// </summary>
        /// <param name="column">The center column.</param>
        /// <param name="row">The center row.</param>
        /// <param name="numberTrue">Outputs the number of true values under the kernel.</param>
        /// <param name="totalCells">Outputs the total number of cells under the kernel. This will be the kernel size except near boundaries.</param>
        /// <returns>The summation of the kernel values where the cells are true.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws an exception if the filter size or column / row are wrong.</exception>
        protected float GetFilteredValue(int column, int row, out int numberTrue, out int totalCells)
        {
            int neighborhoodWidth = KernelWeights.GetLength(0) / 2;
            int neighborhoodHeight = KernelWeights.GetLength(1) / 2;
            int rowMin = Math.Max(0, row - neighborhoodHeight);
            int rowMax = Math.Min(Height - 1, row + neighborhoodHeight);
            int ColumnMin = Math.Max(0, column - neighborhoodWidth);
            int ColumnMax = Math.Min(Width - 1, column + neighborhoodWidth);
            totalCells = (rowMax - rowMin + 1) * (ColumnMax - ColumnMin + 1);
            float filteredValue = 0;
            if (totalCells <= 0)
                throw new ArgumentOutOfRangeException("row, column of NeighborhoodSize is wrong");

            numberTrue = 0;
            int filterRow = 0;
            for (int rowIndex = rowMin; rowIndex <= rowMax; rowIndex++)
            {
                int filterColumn = 0;
                for (int columnIndex = ColumnMin; columnIndex <= ColumnMax; columnIndex++)
                {
                    bool cellValue = _occupancyGridFrontBuffer.GetNodeLabel(columnIndex, rowIndex);
                    if (cellValue)
                        numberTrue++;
                    float cellAsFloat = (cellValue) ? 1 : 0;
                    filteredValue += cellAsFloat * KernelWeights[filterColumn, filterRow];
                    filterColumn++;
                }
                filterRow++;
            }
            return filteredValue;
        }
    }
}