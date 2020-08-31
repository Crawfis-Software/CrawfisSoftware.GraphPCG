using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    public class PathGeneratorSideWinder : MazeBuilderAbstract<int, int>
    {
        public PathGeneratorSideWinder(int width, int height, GetGridLabel<int> nodeAccessor, GetEdgeLabel<int> edgeAccessor) : base(width, height, nodeAccessor, edgeAccessor)
        {
        }

        public override void CreateMaze(bool preserveExistingCells = false)
        {
            int lastColumn = 0;
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
