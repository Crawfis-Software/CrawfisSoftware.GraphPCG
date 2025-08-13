using CrawfisSoftware.Maze;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Static class used to carve a path
    /// </summary>
    public static class MazeWrapperFromGridBitArrays
    {
        /// <summary>
        /// Carve openings based on the list of compressed vertical and horizontal edge flags for each row
        /// </summary>
        /// <param name="mazeBuilder">An existing maze builder to use in the carving process</param>
        /// <param name="verticalPaths">A list of rows, where each row has a bitpattern. 1's in the bit pattern 
        /// indicate a passage should be carved to the next row (i,j)->(i,j+1). Bits are read right to left as the grid goes left to right.</param>
        /// <param name="horizontalPaths">A list of rows, where each row has a bitpattern. 1's in the bit pattern 
        /// indicate a passage should be carved to the next cell (i,j)->(i+1,j). Bits are read right to left as the grid goes left to right.</param>
        // Todo: Change to CarveFromBitPatterns and add this to make it an extension method.
        public static void CarveFromBitPatterns<N, E>(this IMazeBuilder<N, E> mazeBuilder, IList<int> verticalPaths,
            IList<int> horizontalPaths)

        {
            int edges = -1;
            foreach (int passages in verticalPaths)
            {
                edges++;
                if (edges == 0)
                {
                    continue;
                }
                int verticalBits = passages;
                for (int i = 0; i < mazeBuilder.Width; i++)
                {
                    if ((verticalBits & 1) == 1)
                    {
                        mazeBuilder.CarvePassage(i, edges, i,
                            edges - 1);
                    }

                    verticalBits >>= 1;
                }
            }

            int row = 0;
            foreach (int passages in horizontalPaths)
            {
                int horizontalBits = passages;
                for (int i = 0; i < mazeBuilder.Width; i++)
                {
                    if ((horizontalBits & 1) == 1)
                    {
                        mazeBuilder.CarvePassage(i, row, i + 1, row);
                    }

                    horizontalBits >>= 1;
                }

                row++;
            }
        }

        /// <summary>
        /// Carve openings based on the list of compressed vertical and horizontal edge flags for each row
        /// </summary>
        /// <param name="mazeBuilder">An existing maze builder to use in the carving process</param>
        /// <param name="verticalPaths">A list of rows, where each row has a bitpattern. 1's in the bit pattern 
        /// indicate a passage should be carved to the next row (i,j)->(i,j+1). Bits are read right to left as the grid goes left to right.</param>
        /// <param name="horizontalPaths">A list of rows, where each row has a bitpattern. 1's in the bit pattern 
        /// indicate a passage should be carved to the next cell (i,j)->(i+1,j). Bits are read right to left as the grid goes left to right.</param>
        public static void CarveFromBitPatterns<N, E>(IMazeBuilder<N, E> mazeBuilder, IList<BigInteger> verticalPaths,
            IList<BigInteger> horizontalPaths)

        {
            BigInteger bigOne = new BigInteger(1);
            int edges = -1;
            foreach (BigInteger passages in verticalPaths)
            {
                edges++;
                if (edges == 0)
                {
                    continue;
                }
                BigInteger verticalBits = passages;
                for (int i = 0; i < mazeBuilder.Width; i++)
                {
                    if ((verticalBits & bigOne) == bigOne)
                    {
                        mazeBuilder.CarvePassage(i, edges, i,
                            edges - 1);
                    }

                    verticalBits >>= 1;
                }
            }

            int row = 0;
            foreach (BigInteger passages in horizontalPaths)
            {
                BigInteger horizontalBits = passages;
                for (int i = 0; i < mazeBuilder.Width; i++)
                {
                    if ((horizontalBits & bigOne) == bigOne)
                    {
                        mazeBuilder.CarvePassage(i, row, i + 1, row);
                    }

                    horizontalBits >>= 1;
                }

                row++;
            }
        }
    }
}
