using CrawfisSoftware.Collections.Graph;

using System;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Framework for creating false/true (0/1) grids.
    /// </summary>
    public class CellularAutomata
    {
        /// <summary>
        /// An <c>OccupancyGrid</c> that is used as the front buffer.
        /// </summary>
        protected OccupancyGrid _occupancyGridFrontBuffer;
        /// <summary>
        /// An <c>OccupancyGrid</c> that is used as the back buffer.
        /// </summary>
        protected OccupancyGrid _occupancyGridBackBuffer;
        /// <summary>
        /// A random number generator.
        /// </summary>
        protected readonly Random randomGenerator;

        /// <summary>
        /// Get the number of columns in the cellular automata
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Get the number of rows in the cellular automata
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Get or set the neighborhood radius. Default is one.
        /// </summary>
        public int NeighborhoodSize { get; set; } = 1;

        /// <summary>
        /// Get or set the function that takes the number of neighbors and
        /// the number of those that are true (1) and return true or false
        /// </summary>
        /// <remarks>Called if the current cell is true.</remarks>
        public Func<int, int, int, int, int, bool> AutomataForTrueCells { get; set; } = DefaultAutomataIfTrue;

        /// <summary>
        /// Get or set the function that takes the number of neighbors and
        /// the number of those that are true (1) and return true or false
        /// </summary>
        /// <remarks>Called if the current cell is false.</remarks>
        public Func<int, int, int, int, int, bool> AutomataForFalseCells { get; set; } = DefaultAutomataIfFalse;

        /// <summary>
        /// Get or set the function called before each iteration
        /// </summary>
        public Action<int> PreIterationFunc { get; set; } = NoOpFunc;

        /// <summary>
        /// Get or set the function called after each iteration
        /// </summary>
        public Action<int> PostIterationFunc { get; set; } = NoOpFunc;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width">The number of columns.</param>
        /// <param name="height">The number of rows.</param>
        /// <param name="random">A random number generator. If null, a new one will be created.</param>
        public CellularAutomata(int width, int height, Random random = null)
        {
            Width = width;
            Height = height;
            _occupancyGridFrontBuffer = new OccupancyGrid(width, height);
            _occupancyGridBackBuffer = new OccupancyGrid(width, height);
            randomGenerator = random;
            if (random == null)
                randomGenerator = new System.Random();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="occupancyGrid">An initial occupancy grid to use.</param>
        /// <param name="random">A random number generator. If null, a new one will be created.</param>
        public CellularAutomata(OccupancyGrid occupancyGrid, Random random = null)
        {
            _occupancyGridFrontBuffer = occupancyGrid;
            Width = occupancyGrid.Width;
            Height = occupancyGrid.Height;
            _occupancyGridBackBuffer = new OccupancyGrid(Width, Height);
            randomGenerator = random;
            if (random == null)
                randomGenerator = new System.Random();
        }

        /// <summary>
        /// Utility function to initialize or add noise.
        /// </summary>
        /// <param name="threshold">Values above this threshold will be set to true.</param>
        public void AddTrueNoise(float threshold = 0.5f)
        {
            for (int column = 0; column < Width; column++)
            {
                for (int row = 0; row < Height; row++)
                {
                    float randomValue = (float)randomGenerator.NextDouble();
                    if (randomValue > threshold)
                    {
                        _occupancyGridFrontBuffer.MarkCell(column, row, true);
                    }
                }
            }
        }

        /// <summary>
        /// Utility function to initialize or add noise.
        /// </summary>
        /// <param name="threshold">Values above this threshold will be set to false.</param>
        public void AddFalseNoise(float threshold = 0.5f)
        {
            for (int column = 0; column < Width; column++)
            {
                for (int row = 0; row < Height; row++)
                {
                    float randomValue = (float)randomGenerator.NextDouble();
                    if (randomValue > threshold)
                    {
                        _occupancyGridFrontBuffer.MarkCell(column, row, false);
                    }
                }
            }
        }

        /// <summary>
        /// Apply the automata rules repeatedly.
        /// </summary>
        /// <param name="numberOfIterations">The number of times to apply the automata.</param>
        public void IterateAutomata(int numberOfIterations = 1)
        {
            for (int iteration = 0; iteration < numberOfIterations; iteration++)
            {
                PreIterationFunc(iteration);
                for (int column = 0; column < Width; column++)
                {
                    for (int row = 0; row < Height; row++)
                    {
                        int numberOfNeighbors = GetNeighborsAndCount(column, row, out int numberTrue);
                        if (_occupancyGridFrontBuffer.GetNodeLabel(column, row))
                        {
                            bool newValue = AutomataForTrueCells(column, row, iteration, numberTrue, numberOfNeighbors);
                            _occupancyGridBackBuffer.MarkCell(column, row, newValue);
                        }
                        else
                        {
                            bool newValue = AutomataForFalseCells(row, column, iteration, numberTrue, numberOfNeighbors);
                            _occupancyGridBackBuffer.MarkCell(column, row, newValue);
                        }
                    }
                }
                PostIterationFunc(iteration);
                var tmp = _occupancyGridFrontBuffer;
                _occupancyGridFrontBuffer = _occupancyGridBackBuffer;
                _occupancyGridBackBuffer = tmp;
            }
        }

        /// <summary>
        /// Return the OccupancyGrid of the cellular automata
        /// </summary>
        /// <returns></returns>
        public OccupancyGrid GetOccupancyGrid()
        {
            return _occupancyGridFrontBuffer;
        }

        /// <summary>
        /// Count the total number of neighbors under the cell with a box of NeighborhoodSize. Also count the number of those cells that are true.
        /// </summary>
        /// <param name="column">The center column.</param>
        /// <param name="row">The center row.</param>
        /// <param name="numberTrue">Outputs the number of true values under the kernel.</param>
        /// <returns>Outputs the total number of cells under the kernel. This will be the kernel size except near boundaries.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws an exception if the filter size or column / row are wrong.</exception>
        protected int GetNeighborsAndCount(int column, int row, out int numberTrue)
        {
            int rowMin = Math.Max(0, row - NeighborhoodSize);
            int rowMax = Math.Min(Height - 1, row + NeighborhoodSize);
            int ColumnMin = Math.Max(0, column - NeighborhoodSize);
            int ColumnMax = Math.Min(Width - 1, column + NeighborhoodSize);
            int totalCells = (rowMax - rowMin + 1) * (ColumnMax - ColumnMin + 1);
            if (totalCells <= 0)
                throw new ArgumentOutOfRangeException("row, column of NeighborhoodSize is wrong");

            numberTrue = 0;
            for (int rowIndex = rowMin; rowIndex <= rowMax; rowIndex++)
            {
                for (int columnIndex = ColumnMin; columnIndex <= ColumnMax; columnIndex++)
                {
                    if (columnIndex == column && rowIndex == row)
                        continue;
                    if (_occupancyGridFrontBuffer.GetNodeLabel(columnIndex, rowIndex))
                        numberTrue++;
                }
            }
            return totalCells;
        }

        /// <summary>
        /// Converts the Cellular Automata to an asci string representation
        /// </summary>
        /// <returns>A string</returns>
        public override string ToString()
        {
            return _occupancyGridFrontBuffer.ToString();
        }

        private static int defaultKeepWallThreshold = 3;
        private static int defaultRemoveWallThreshold = 4;
        private static int neighborsInKernel = 8;
        private static bool DefaultAutomataIfTrue(int column, int row, int iteration, int numberTrue, int numberOfNeighbors)
        {
            if (numberOfNeighbors < neighborsInKernel) // Valid for neighborhood size of 1
                return true; // Wall at all edges
            if (numberTrue >= defaultKeepWallThreshold)
                return true;
            if (numberTrue < defaultRemoveWallThreshold)
                return false;
            return true; // Keep cell as is
        }

        private static int defaultLeaveAsOpeningThreshold = 4;
        private static int defaultTurnIntoWallThreshold = 3;
        private static bool DefaultAutomataIfFalse(int column, int row, int iteration, int numberTrue, int numberOfNeighbors)
        {
            if (numberOfNeighbors < neighborsInKernel) // Valid for neighborhood size of 1
                return false; // keep edges
            if (numberOfNeighbors < defaultLeaveAsOpeningThreshold)
                return false;
            if (numberTrue >= defaultTurnIntoWallThreshold)
                return true;
            return false;
        }

        private static void NoOpFunc(int iteration)
        {
        }
    }
}