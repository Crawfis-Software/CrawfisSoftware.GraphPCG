using CrawfisSoftware.Collections.Maze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    public static class MazeWrapperFromGridBitArrays
    {
        public static void CarvePath(MazeBuilderAbstract<int,int> mazeBuilder, IList<int> verticalPaths, IList<int> horizontalPaths)
        {
            int row = 0;
            foreach(int passages in verticalPaths)
            {
                int verticalBits = passages;
                for(int i=0; i < mazeBuilder.Width; i++)
                {
                    if((verticalBits & 1) == 1)
                    {
                        mazeBuilder.CarvePassage(i, row, i, row + 1);
                    }
                    verticalBits >>= 1;
                }

                int horizontalBits = horizontalPaths[row];
                for (int i = 0; i < mazeBuilder.Width; i++)
                {
                    if ((horizontalBits & 1) == 1)
                    {
                        mazeBuilder.CarvePassage(i, row+1, i+1, row+1);
                    }
                    horizontalBits >>= 1;
                }

                row++;
            }
        }
    }
}
