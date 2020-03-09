using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    public class PathEnumeration : MazeBuilderAbstract<int, int>
    {
        public PathEnumeration(int width, int height, int start, int end) : base(width, height, NodeValues, EdgeValues)
        {
            // Can optimize enumeration by building tables for a fixed width.
            // This can be accomplished by calling FindSpans and storing the results.
        }

        public override void CreateMaze(bool preserveExistingCells)
        {
            throw new NotImplementedException();
        }
        private IEnumerable<int> FindSpans(int width, int inBitLocation)
        {
            if (width == 1)
                yield return 1;

            foreach (int pattern in SplitOdd0Even(width, inBitLocation))
            {
                yield return pattern;
            }
            foreach (int pattern in SplitEven0Odd(width, inBitLocation))
            {
                yield return pattern;
            }
            foreach (int pattern in SplitEven1Even(width, inBitLocation))
            {
                yield return pattern;
            }

        }

        private IEnumerable<int> SplitEven1Even(int width, int inBitLocation)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<int> SplitEven0Odd(int width, int inBitLocation)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<int> SplitOdd0Even(int width, int inBitLocation)
        {
            // If inBitLocation is the first bit, then nothing should be returned
            // as an odd number of bits is not possible before this location.
            if (inBitLocation > 0)
            {
                if (inBitLocation == width - 1)
                {
                    foreach (int pattern in AllEven(width - 1))
                    {
                        yield return pattern << 1;
                    }
                }
                else
                {
                    foreach (int oddPattern in AllOdd(inBitLocation))
                    {
                        int oddPatternShifted = oddPattern << inBitLocation; 
                        foreach (int evenPattern in AllEven(width - inBitLocation - 1))
                        {
                            int pattern = oddPatternShifted + evenPattern;
                            yield return pattern;
                        }
                    }
                }
            }
        }

        private IEnumerable<int> AllOdd(int inBitLocation)
        {
            throw new NotImplementedException();
        }
        // Tables store values with an odd or even number of bits. Entry zero is ignored.
        private int[][] oddTables = new int[][] { new int[] { 0 }, new int[] { 1 }, new int[] { 1, 2 }, new int[] { 1, 2, 4, 7 } };
        private int[][] evenTables = new int[][] { new int[] { 0 }, new int[] { 0 }, new int[] { 0, 3 }, new int[] { 0, 3, 5, 6 } };
        private const int maxTableSize = 4;
        private IEnumerable<int> AllEven(int width)
        {
            if(width < maxTableSize)
            {
                // return table values;
                return evenTables[width];
            }

        }

        private static int NodeValues(int i, int j)
        {
            return 1;
        }
        private static int EdgeValues(int i, int j, Direction dir)
        {
            return 1;
        }
    }
}
