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
        public int MaxSpanWidth { get; set; } = 5;
        public Func<int, int, int> PickNextColumn { get; set; } = DefaultPickNextColumnFunc;

        private static int DefaultPickNextColumnFunc(int row, int previousColumn)
        {
            return 5;
        }

        private int stepSize = 2;
        private int stepDirection = 1;
        private int PickNextColumnWaveFunc(int row, int previousColumn)
        {
            if (previousColumn > (Width - stepSize)) stepDirection = (stepDirection > 0) ? -stepDirection : stepDirection;
            if (previousColumn < stepSize) stepDirection = (stepDirection < 0) ? -stepDirection : stepDirection;
            int step = (RandomGenerator.Next(stepSize)) * stepDirection;
            return previousColumn + step;
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
            this.PickNextColumn = PickNextColumnWaveFunc;
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
                lastColumn = CarveSpan(row, lastColumn, column, preserveExistingCells);
            }
            CarveSpan(Height - 1, lastColumn, Width - 1, preserveExistingCells);
        }

        private int CarveSpan(int row, int lastColumn, int column, bool preserveExistingCells)
        {
            int start = column;
            int end = lastColumn;
            lastColumn = column;
            if (start > end)
            {
                start = end;
                end = column;
            }
            for (int i = start; i < end; i++)
            {
                CarveDirectionally(i, row, Direction.E, preserveExistingCells);
            }

            return lastColumn;
        }
    }
}
