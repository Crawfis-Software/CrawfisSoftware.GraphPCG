using CrawfisSoftware.Maze;

using System;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Generate a path from the start column on the bottom row to the end column
    /// on the top row.
    /// </summary>
    public class PathGeneratorSideWinder<N, E>
    {
        IMazeBuilder<N, E> _mazeBuilder;
        /// <summary>
        /// Get or set the maximum horizontal passage length used in the default
        /// PickNextColumn function.
        /// </summary>
        public int MaxSpanWidth { get; set; } = 5;

        /// <summary>
        /// Get or set the a function to determine on a per row basis the exact column
        /// the curve should shift over to. Defaults to a random column to the left or
        /// right of the previous column at most MaxSpanWidth away.
        /// </summary>
        public Func<int, int, System.Random, int> PickNextColumn { get; set; }

        /// <summary>
        /// Get or set the a function to determine on a per row basis the exact row
        /// the curve should move to after the span for that row is completed. Defaults
        /// to the next row (returns row+1).
        /// </summary>
        public Func<int, int, System.Random, int> PickNextRow { get; set; }

        private int DefaultPickNextColumnFunc(int row, int previousColumn, System.Random randomGenerator = null)
        {
            int delta = randomGenerator.Next(_mazeBuilder.Width) - previousColumn;
            int sign = 1;
            if (delta < 0) sign = -1;
            delta = ((sign * delta) > MaxSpanWidth) ? sign * MaxSpanWidth : delta;
            return previousColumn + delta;
        }

        private bool first = true;
        private int DefaultPickNextRowFunc(int row, int previousColumn, System.Random randomGenerator = null)
        {
            // An example of a "reset". Once it hits 12 it loops back to row 2, creating a long vertical and possibly loops
            //if(first && row == 12)
            //{
            //    first = false;
            //    return 2;
            //}
            return row + 3;
        }

        /// <summary>
        /// Constructor. Takes an existing maze builder (derived from MazeBuilderAbstract) and copies the state over.
        /// </summary>
        /// <param name="mazeBuilder">Previous MazeBuilderAbstract on which to build upon.</param>
        public PathGeneratorSideWinder(IMazeBuilder<N, E> mazeBuilder)
        {
            _mazeBuilder = mazeBuilder;
            this.PickNextColumn = DefaultPickNextColumnFunc;
            this.PickNextRow = DefaultPickNextRowFunc;
        }

        /// <inheritdoc/>
        public void CarvePath(IMazeBuilder<N, E> mazeBuilder, bool preserveExistingCells = false)
        {
            int lastColumn = mazeBuilder.StartCell % mazeBuilder.Width;
            int row = mazeBuilder.StartCell / mazeBuilder.Width;
            while (row < (mazeBuilder.Height - 1))
            {
                //int column = RandomGenerator.Next(Width);
                int column = PickNextColumn(row, lastColumn, mazeBuilder.RandomGenerator);
                column = (column < 0) ? 0 : column;
                column = (column >= mazeBuilder.Width) ? mazeBuilder.Width - 1 : column;
                //CarveDirectionally(column, row, Direction.N, preserveExistingCells);
                mazeBuilder.CarveHorizontalSpan(row, lastColumn, column, preserveExistingCells);
                lastColumn = column;
                int nextRow = PickNextRow(row, column, mazeBuilder.RandomGenerator);
                nextRow = (nextRow < 0) ? 0 : nextRow;
                nextRow = (nextRow >= mazeBuilder.Height) ? mazeBuilder.Height - 1 : nextRow;
                mazeBuilder.CarveVerticalSpan(column, row, nextRow, preserveExistingCells);
                row = nextRow;
            }
            int exitColumn = mazeBuilder.EndCell % mazeBuilder.Width;
            mazeBuilder.CarveHorizontalSpan(mazeBuilder.Height - 1, lastColumn, exitColumn, preserveExistingCells);
        }
    }
}