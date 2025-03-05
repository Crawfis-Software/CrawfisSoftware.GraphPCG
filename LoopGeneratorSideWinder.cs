using System;

using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Maze;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Generate a random loop on a grid that consists of two paths that merge at
    /// the bottom row and top row.
    /// </summary>
    public class LoopGeneratorSideWinder<N, E>
    {
        /// <summary>
        /// Get or set the maximum horizontal passage length used in the default
        /// PickNextColumn function.
        /// </summary>
        public int MaxSpanWidth { get; set; } = 5;

        /// <summary>
        /// Get or set the number of rows that should be vertical spans before any turns. This is used in the default
        /// PickNextColumn function.
        /// </summary>
        public int MinVerticalSpan { get; private set; } = 1;
        /// <summary>
        /// Get or set the number of rows that should be vertical spans before any turns. This is used in the default
        /// PickNextColumn function.
        /// </summary>
        public int MinLeftToRightSpacing { get; private set; } = 1;

        /// <summary>
        /// Get or set the a function to determine on a per row basis the exact column
        /// the curve should shift over to. Defaults to a random column to the left or
        /// right of the previous column at most MaxSpanWidth away.
        /// </summary>
        public Func<int, int, int, System.Random, (int, int)> PickNextColumns { get; set; }

        /// <summary>
        /// Get or set whether to ignore the start and end cells when generating the maze.
        /// </summary>
        public bool IgnoreStartAndEnd { get; set; }
        /// <summary>
        /// Get or set whether to reset the start to the leftmost cell in the loop on the bottom row and the rightmost cell on the top row that are within the loop.
        /// </summary>
        public bool ResetStartAndEnd { get; set; }

        private IMazeBuilder<N, E> _mazeBuilder;
        private int _lastRow = -99;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ignoreStartEnd">Boolean indicating whether to ignore the start and end cells when generating the maze.</param>
        /// <param name="resetStartEnd">Boolean indicating whether to reset the start to the leftmost cell in the loop on the bottom row and 
        /// the end to the rightmost cell on the top row.</param>
        public LoopGeneratorSideWinder(bool ignoreStartEnd = true, bool resetStartEnd = false)
        {
            this.PickNextColumns = DefaultPickNextColumnsFunc;
            this.IgnoreStartAndEnd = ignoreStartEnd;
            this.ResetStartAndEnd = resetStartEnd;
        }

        /// <summary>
        /// Create a maze using the Sidewinder algorithm
        /// </summary>
        /// <param name="mazeBuilder">IMazeBuilder to use.</param>
        /// <param name="preserveExistingCells">Boolean indicating whether to only replace maze cells that are undefined.
        /// Default is false.</param>
        public void CarveLoop(IMazeBuilder<N, E> mazeBuilder, bool preserveExistingCells = true)
        {
            this._mazeBuilder = mazeBuilder;
            (int leftColumn, int rightColumn) = PickNextColumns(0, 0, _mazeBuilder.Width - 1, _mazeBuilder.RandomGenerator);
            leftColumn = (leftColumn < 0) ? 0 : leftColumn;
            leftColumn = (leftColumn >= rightColumn) ? rightColumn - 1 : leftColumn;
            if (!IgnoreStartAndEnd && _mazeBuilder.StartCell < leftColumn) leftColumn = _mazeBuilder.StartCell;
            rightColumn = (rightColumn < leftColumn) ? leftColumn + 1 : rightColumn;
            rightColumn = (rightColumn >= _mazeBuilder.Width) ? _mazeBuilder.Width - 1 : rightColumn;
            if (!IgnoreStartAndEnd && _mazeBuilder.StartCell > rightColumn) rightColumn = _mazeBuilder.StartCell;
            _mazeBuilder.CarveHorizontalSpan(0, leftColumn, rightColumn, preserveExistingCells);
            int lastLeftColumn = leftColumn;
            int lastRightColumn = rightColumn;
            if (ResetStartAndEnd) _mazeBuilder.StartCell = leftColumn;
            for (int row = 1; row < _mazeBuilder.Height - 2; row++)
            {
                lastLeftColumn = leftColumn;
                lastRightColumn = rightColumn;
                _mazeBuilder.CarveVerticalSpan(leftColumn, row - 1, row, preserveExistingCells);
                _mazeBuilder.CarveVerticalSpan(rightColumn, row - 1, row, preserveExistingCells);
                (leftColumn, rightColumn) = PickNextColumns(row, lastLeftColumn, lastRightColumn, _mazeBuilder.RandomGenerator);
                leftColumn = (leftColumn < 0) ? 0 : leftColumn;
                leftColumn = (leftColumn >= lastRightColumn) ? lastRightColumn - 1 : leftColumn;
                _mazeBuilder.CarveHorizontalSpan(row, lastLeftColumn, leftColumn, preserveExistingCells);

                rightColumn = (rightColumn < leftColumn) ? leftColumn + 1 : rightColumn;
                rightColumn = (rightColumn >= _mazeBuilder.Width) ? _mazeBuilder.Width - 1 : rightColumn;
                _mazeBuilder.CarveHorizontalSpan(row, lastRightColumn, rightColumn, preserveExistingCells);
            }
            int secondToLastRow = _mazeBuilder.Height - 2;
            int lastRow = _mazeBuilder.Height - 1;
            _mazeBuilder.CarveVerticalSpan(leftColumn, secondToLastRow - 1, secondToLastRow, preserveExistingCells);
            _mazeBuilder.CarveVerticalSpan(rightColumn, secondToLastRow - 1, secondToLastRow, preserveExistingCells);
            lastLeftColumn = leftColumn;
            lastRightColumn = rightColumn;
            (leftColumn, rightColumn) = PickNextColumns(secondToLastRow, leftColumn, rightColumn, _mazeBuilder.RandomGenerator);
            leftColumn = (leftColumn < 0) ? 0 : leftColumn;
            leftColumn = (leftColumn >= rightColumn) ? rightColumn - 1 : leftColumn;
            int exitColumn = _mazeBuilder.EndCell % _mazeBuilder.Width;
            if (!IgnoreStartAndEnd && exitColumn < leftColumn)
            {
                leftColumn = exitColumn;
            }
            _mazeBuilder.CarveHorizontalSpan(secondToLastRow, leftColumn, lastLeftColumn, preserveExistingCells);
            if (!IgnoreStartAndEnd && exitColumn > rightColumn)
            {
                rightColumn = exitColumn;
            }
            _mazeBuilder.CarveHorizontalSpan(secondToLastRow, lastRightColumn, rightColumn, preserveExistingCells);

            _mazeBuilder.CarveVerticalSpan(leftColumn, secondToLastRow, lastRow, preserveExistingCells);
            _mazeBuilder.CarveVerticalSpan(rightColumn, secondToLastRow, lastRow, preserveExistingCells);
            _mazeBuilder.CarveHorizontalSpan(lastRow, leftColumn, rightColumn, preserveExistingCells);
            if (ResetStartAndEnd) _mazeBuilder.EndCell = rightColumn + _mazeBuilder.Width * (_mazeBuilder.Height - 1);
        }
        private (int, int) DefaultPickNextColumnsFunc(int row, int previousLeftColumn, int previousRightColumn, System.Random randomGenerator = null)
        {
            if (row < _lastRow + MinVerticalSpan) return (previousLeftColumn, previousRightColumn);
            _lastRow = row;
            //return (0, Width - 1);
            int delta = randomGenerator.Next(MaxSpanWidth + 1);
            int sign = randomGenerator.Next(2) == 1 ? 1 : -1;
            int newLeftColumn = previousLeftColumn + sign * delta;
            if (newLeftColumn > _mazeBuilder.Width - 1 - MinLeftToRightSpacing) newLeftColumn = _mazeBuilder.Width - 1 - MinLeftToRightSpacing - randomGenerator.Next(5);
            if (newLeftColumn > previousRightColumn - MinLeftToRightSpacing) newLeftColumn = previousRightColumn - MinLeftToRightSpacing;
            if (newLeftColumn < 0) newLeftColumn = previousLeftColumn - sign * delta;
            if (newLeftColumn > previousRightColumn - MinLeftToRightSpacing) newLeftColumn = previousRightColumn - MinLeftToRightSpacing;
            delta = randomGenerator.Next(MaxSpanWidth + 1);
            sign = randomGenerator.Next(2) == 1 ? 1 : -1;
            int newRightColumn = previousRightColumn + sign * delta;
            if (newRightColumn < newLeftColumn + MinLeftToRightSpacing) newRightColumn = newLeftColumn + MinLeftToRightSpacing;
            if (newRightColumn < previousLeftColumn + MinLeftToRightSpacing) newRightColumn = previousLeftColumn + MinLeftToRightSpacing;
            if (newRightColumn >= _mazeBuilder.Width) newRightColumn = _mazeBuilder.Width - 1;
            return (newLeftColumn, newRightColumn);
        }
    }
}