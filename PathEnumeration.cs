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
        public static IEnumerable<int> FindSpans(int width, int inBitLocation)
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

        private static IEnumerable<int> SplitEven1Even(int width, int inBitLocation)
        {
            foreach (int evenPattern in BitEnumerators.AllEven(inBitLocation))
            {
                int evenPatternShifted = evenPattern << (width - inBitLocation);
                evenPatternShifted += 1 << (width - inBitLocation-1);
                foreach (int oddPattern in BitEnumerators.AllEven(width - inBitLocation - 1))
                {
                    int pattern = evenPatternShifted + oddPattern;
                    yield return pattern;
                }
            }
        }

        private static IEnumerable<int> SplitEven0Odd(int width, int inBitLocation)
        {
            // If inBitLocation is the last bit, then nothing should be returned
            // as an odd number of bits is not possible after this location.
            if (inBitLocation <= (width - 1))
            {
                if (inBitLocation == 0)
                {
                    foreach (int pattern in BitEnumerators.AllOdd(width - 1))
                    {
                        yield return pattern;
                    }
                }
                else
                {
                    foreach (int evenPattern in BitEnumerators.AllEven(inBitLocation))
                    {
                        int evenPatternShifted = evenPattern << (width - inBitLocation);
                        foreach (int oddPattern in BitEnumerators.AllOdd(width - inBitLocation - 1))
                        {
                            int pattern = evenPatternShifted + oddPattern;
                            yield return pattern;
                        }
                    }
                }
            }
        }

        private static IEnumerable<int> SplitOdd0Even(int width, int inBitLocation)
        {
            // If inBitLocation is the first bit, then nothing should be returned
            // as an odd number of bits is not possible before this location.
            if (inBitLocation > 0)
            {
                if (inBitLocation == width - 1)
                {
                    foreach (int pattern in BitEnumerators.AllEven(width - 1))
                    {
                        yield return pattern << 1;
                    }
                }
                else
                {
                    foreach (int oddPattern in BitEnumerators.AllOdd(inBitLocation))
                    {
                        int oddPatternShifted = oddPattern << (width - inBitLocation); 
                        foreach (int evenPattern in BitEnumerators.AllEven(width - inBitLocation - 1))
                        {
                            int pattern = oddPatternShifted + evenPattern;
                            yield return pattern;
                        }
                    }
                }
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
