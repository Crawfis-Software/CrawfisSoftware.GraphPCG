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
    /// 
    /// </summary>
    public class PathGeneratorSideWinder : MazeBuilderAbstract<int, int>
    {
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
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells = false)
        {
            int lastColumn = StartCell % Width;
            for(int row = 0; row < (Height-1); row++)
            {
                int column = RandomGenerator.Next(Width);
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
