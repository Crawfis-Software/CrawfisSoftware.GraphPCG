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
        public Func<int, int, int> PickNextColumn { get; set; }

        private int DefaultPickNextColumnFunc(int row, int previousColumn)
        {
            int delta = RandomGenerator.Next(Width) - previousColumn;
            int sign = 1;
            if (delta < 0) sign = -1;
            delta = ((sign*delta) > MaxSpanWidth) ? sign*MaxSpanWidth : delta;
            return previousColumn + delta;
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
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells = false)
        {
            int lastColumn = StartCell % Width;
            for(int row = 0; row < (Height-1); row++)
            {
                //int column = RandomGenerator.Next(Width);
                int column = PickNextColumn(row, lastColumn);
                column = (column < 0) ? 0 : column;
                column = (column >= Width) ? Width - 1 : column;
                CarveDirectionally(column, row, Direction.N, preserveExistingCells);
                CarveHorizontalSpan(row, lastColumn, column, preserveExistingCells);
                lastColumn = column;
            }
            CarveHorizontalSpan(Height - 1, lastColumn, Width - 1, preserveExistingCells);
        }
    }
}
