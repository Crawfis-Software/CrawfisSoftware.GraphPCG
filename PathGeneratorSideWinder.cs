using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Generate a path from the start column on the bottom row to the end column
    /// on the top row.
    /// </summary>
    public class PathGeneratorSideWinder : MazeBuilderAbstract<int, int>
    {
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
            int delta = RandomGenerator.Next(Width) - previousColumn;
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
            return row+3;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the desired maze</param>
        /// <param name="height">The height of the desired maze</param>
        /// <param name="nodeAccessor">A function to retrieve any node labels</param>
        /// <param name="edgeAccessor">A function to retrieve any edge weights</param>
        public PathGeneratorSideWinder(int width, int height, GetGridLabel<int> nodeAccessor = null, GetEdgeLabel<int> edgeAccessor = null)
            : base(width, height, nodeAccessor, edgeAccessor)
        {
            this.PickNextColumn = DefaultPickNextColumnFunc;
            this.PickNextRow = DefaultPickNextRowFunc;
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells = false)
        {
            int lastColumn = StartCell % Width;
            int row = 0;
            while ( row < (Height-1))
            {
                //int column = RandomGenerator.Next(Width);
                int column = PickNextColumn(row, lastColumn, RandomGenerator);
                column = (column < 0) ? 0 : column;
                column = (column >= Width) ? Width - 1 : column;
                //CarveDirectionally(column, row, Direction.N, preserveExistingCells);
                CarveHorizontalSpan(row, lastColumn, column, preserveExistingCells);
                lastColumn = column;
                int nextRow = PickNextRow(row, column, RandomGenerator);
                CarveVerticalSpan(column, row, nextRow, preserveExistingCells);
                row = nextRow;
            }
            int exitColumn = EndCell % Width;
            CarveHorizontalSpan(Height - 1, lastColumn, exitColumn, preserveExistingCells);
        }
    }
}
