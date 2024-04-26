using System;

using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Generate a random loop on a grid that consists of two paths that merge at
    /// the bottom row and top row.
    /// </summary>
    public class LoopGeneratorSideWinder<N, E> : MazeBuilderAbstract<N, E>
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

        private int _lastRow = -99;
        private (int, int) DefaultPickNextColumnsFunc(int row, int previousLeftColumn, int previousRightColumn, System.Random randomGenerator = null)
        {
            if (row < _lastRow + MinVerticalSpan) return (previousLeftColumn, previousRightColumn);
            _lastRow = row;
            //return (0, Width - 1);
            int delta = RandomGenerator.Next(MaxSpanWidth + 1);
            int sign = RandomGenerator.Next(2) == 1 ? 1 : -1;
            int newLeftColumn = previousLeftColumn + sign * delta;
            if (newLeftColumn > Width - 1 - MinLeftToRightSpacing) newLeftColumn = Width - 1 - MinLeftToRightSpacing - RandomGenerator.Next(5);
            if (newLeftColumn > previousRightColumn - MinLeftToRightSpacing) newLeftColumn = previousRightColumn - MinLeftToRightSpacing;
            if (newLeftColumn < 0) newLeftColumn = previousLeftColumn - sign * delta;
            if (newLeftColumn > previousRightColumn - MinLeftToRightSpacing) newLeftColumn = previousRightColumn - MinLeftToRightSpacing;
            delta = RandomGenerator.Next(MaxSpanWidth + 1);
            sign = RandomGenerator.Next(2) == 1 ? 1 : -1;
            int newRightColumn = previousRightColumn + sign * delta;
            if (newRightColumn < newLeftColumn + MinLeftToRightSpacing) newRightColumn = newLeftColumn + MinLeftToRightSpacing;
            if (newRightColumn < previousLeftColumn + MinLeftToRightSpacing) newRightColumn = previousLeftColumn + MinLeftToRightSpacing;
            if (newRightColumn >= Width) newRightColumn = Width - 1;
            return (newLeftColumn, newRightColumn);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the desired maze</param>
        /// <param name="height">The height of the desired maze</param>
        /// <param name="nodeAccessor">A function to retrieve any node labels</param>
        /// <param name="edgeAccessor">A function to retrieve any edge weights</param>
        public LoopGeneratorSideWinder(int width, int height, GetGridLabel<N> nodeAccessor = null, GetEdgeLabel<E> edgeAccessor = null) : base(width, height, nodeAccessor, edgeAccessor)
        {
            this.PickNextColumns = DefaultPickNextColumnsFunc;
            MaxSpanWidth = Width;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mazeBuilder">Previous MazeBuilderAbstract on which to build upon.</param>
        public LoopGeneratorSideWinder(MazeBuilderAbstract<N, E> mazeBuilder) : base(mazeBuilder)
        {
            this.PickNextColumns = DefaultPickNextColumnsFunc;
            MaxSpanWidth = Width;
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells = true)
        {
            (int lastLeftColumn, int lastRightColumn) = PickNextColumns(0, 0, Width - 1, RandomGenerator);
            lastLeftColumn = (lastLeftColumn < 0) ? 0 : lastLeftColumn;
            lastLeftColumn = (lastLeftColumn >= lastRightColumn) ? lastRightColumn - 1 : lastLeftColumn;
            lastRightColumn = (lastRightColumn < lastLeftColumn) ? lastLeftColumn + 1 : lastRightColumn;
            lastRightColumn = (lastRightColumn >= Width) ? Width - 1 : lastRightColumn;
            CarveHorizontalSpan(0, lastLeftColumn, lastRightColumn, preserveExistingCells);
            StartCell = lastLeftColumn;
            EndCell = lastLeftColumn + 1;
            for (int row = 1; row < Height - 1; row++)
            {
                CarveVerticalSpan(lastLeftColumn, row - 1, row, preserveExistingCells);
                CarveVerticalSpan(lastRightColumn, row - 1, row, preserveExistingCells);
                (int leftColumn, int rightColumn) = PickNextColumns(row, lastLeftColumn, lastRightColumn, RandomGenerator);
                leftColumn = (leftColumn < 0) ? 0 : leftColumn;
                leftColumn = (leftColumn >= lastRightColumn) ? lastRightColumn - 1 : leftColumn;
                CarveHorizontalSpan(row, lastLeftColumn, leftColumn, preserveExistingCells);
                lastLeftColumn = leftColumn;

                rightColumn = (rightColumn < leftColumn) ? leftColumn + 1 : rightColumn;
                rightColumn = (rightColumn >= Width) ? Width - 1 : rightColumn;
                CarveHorizontalSpan(row, lastRightColumn, rightColumn, preserveExistingCells);
                lastRightColumn = rightColumn;
            }
            int exitColumn = EndCell % Width;
            CarveVerticalSpan(lastLeftColumn, Height - 1 - 1, Height - 1, preserveExistingCells);
            CarveVerticalSpan(lastRightColumn, Height - 1 - 1, Height - 1, preserveExistingCells);
            CarveHorizontalSpan(Height - 1, lastLeftColumn, lastRightColumn, preserveExistingCells);
        }
    }
}