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
            // Path Generation can be thought of as three sections. This algorithm will sweep
            // up the grid, row by row. For faster performance, rotate your grid such that the
            // width of the row is smaller than the height of the overall grid or maze.
            // Assume start is on a lower row than end.
            //    Section 1: Find all loops from 0 until start -> an even number of in-flows.
            //    Section 2: Find all partial paths from start through end -> odd number of in-flows
            //    Section 3: Find all loops from end+1 until height.
            // Create first row
            // Sweep up adding new rows of loops until Path start
            // Make sure path start is not encased in a component.
            // Sweep up adding new rows of paths until Path end.
            // Make sure components allow start and end to be connected.
            // Sweep up adding new rows of loops until last row.
            // Make sure last row is valid and components can be connected w/o intersections.
            // (optional) Make sure path or loop touches the ends of the rows (0 and width-1).
            // While sweeping
            //    Handle connected components and reject enumerations that from disconnected closed loops.
            //    
        }
        public static IEnumerable<int> FindRows(int width, IList<int> inFlows)
        {
            // Go through the list of inFlows and use the FindMatchingRows 
            // for each subset of inFlows with possible components merged.
            // This can be accomplished by looking at the set of all pairs and
            // splitting it into those that start with even and those that start with odd.
            // All possible selections of these sets are possible. Which implies
            // all bit vectors of the size of the subsets.
            int numberOfBits = inFlows.Count / 2;
            int subsetSize = (1 << numberOfBits) - 1;
            var inFlowSets = new List<IList<int>>();
            inFlowSets.Add(inFlows);
            for(int i = 0; i < subsetSize; i++)
            {
                var inFlowEvenSubset = new List<int>();
                var inFlowOddSubset = new List<int>();
                inFlowOddSubset.Add(0);
                int bitPattern = i;
                for(int bit=0; bit < numberOfBits; bit++)
                {
                    if((bitPattern&1) == 1)
                    {
                        inFlowEvenSubset.Add(2 * bit);
                        inFlowEvenSubset.Add(2 * bit + 1);
                        inFlowOddSubset.Add(2 * bit + 1);
                        inFlowOddSubset.Add(2 * bit + 2);
                    }
                    bitPattern >>= 1;
                }
                inFlowEvenSubset.Add(inFlows.Count - 1);
                inFlowSets.Add(inFlowEvenSubset);
                inFlowSets.Add(inFlowOddSubset);
            }
            var validSets = new List<IList<int>>(inFlowSets.Count);
            // Todo; Validate each set. If valid, add it to a new set.
            // A set is invalid if it merges (closes a loop) on two inflows
            // with the same component.
            // Todo: For each set we need a mask to prevent new iterations from
            // adding a horizontal link past a newly closed loop.
            foreach (var set in inFlowSets)
            {
                //if()
                {
                    validSets.Add(set);
                }
            }
            foreach(var newInFlow in validSets)
            {
                int mask = (1 << width) - 1;
                // mask sould be computed setting to zero all bits from deleted inFlows.
                foreach (int row in FindMatchingRows(width,newInFlow))
                {
                    // Todo: Need to validate each row with a mask or something.
                    // Or this needs to be placed within FindMatchingRows.
                    // Need to probably return the componenents with each row as well.
                    if((row&mask) == row)
                        yield return row;
                }
            }
        }
        public static IEnumerable<int> FindMatchingRows(int width, IList<int> inFlows)
        {
            // This method will ensure each inFlow has an out flow. 
            // Additional new outFlow pairs are also permissible.
            // It is basically a depth-first tree traversal of the possible spans
            // going from left-to-right (down the tree). Foreach first span there
            // are a set of children associated with the set of second spans, for each
            // of these there is a set of children with the third span, etc.
            int currentIndex = 0;
            var spanEnumerator = MergeSpans(width, 0, currentIndex, inFlows).GetEnumerator();
            var stack = new Stack<IEnumerator<int>>();
            var indexStack = new Stack<int>();
            while (true)
            {
                if (spanEnumerator.MoveNext())
                {
                    int currentRow = spanEnumerator.Current;
                    if (currentIndex == inFlows.Count - 1)
                    {
                        yield return currentRow;
                    }
                    else
                    {
                        stack.Push(spanEnumerator);
                        indexStack.Push(currentIndex);
                        spanEnumerator = MergeSpans(width, currentRow, ++currentIndex,inFlows).GetEnumerator();
                    }
                }
                else if (stack.Count > 0)
                {
                    spanEnumerator.Dispose();
                    spanEnumerator = stack.Pop();
                    currentIndex = indexStack.Pop();
                }
                else
                {
                    yield break;
                }
            }
        }
        private static IEnumerable<int> MergeSpans(int width, int currentRow, int currentInFlowIndex, IList<int> inFlows)
        {
            int spanStart = -1;
            int tempRow = currentRow;
            for(int i=0; i <= width; i++)
            {
                spanStart++;
                if ((tempRow & 1) ==1) break;
                tempRow = tempRow >> 1;
            }
            spanStart = width - spanStart;
            int lastBitLoc = (currentInFlowIndex > 0) ? inFlows[currentInFlowIndex - 1]+1 : 0;
            spanStart = (spanStart > lastBitLoc) ? spanStart : lastBitLoc;
            int currentInFlow = inFlows[currentInFlowIndex];
            int spanEnd = (inFlows.Count > (currentInFlowIndex+1)) ? inFlows[currentInFlowIndex + 1] : width;
            int spanWidth = spanEnd - spanStart;
            int shiftAmount = width - spanEnd;
            int inBitLocation = currentInFlow - spanStart;
            foreach(int pattern in FindSpans(spanWidth,inBitLocation))
            {
                yield return currentRow | (pattern << shiftAmount);
            }
        }
        public static IEnumerable<int> FindSpans(int width, int inBitLocation)
        {
            if (width == 1)
            {
                yield return 1;
                yield break;
            }

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
            if (inBitLocation < (width - 1))
            {
                //if (inBitLocation == 0)
                //{
                //    foreach (int pattern in BitEnumerators.AllOdd(width - 1))
                //    {
                //        yield return pattern;
                //    }
                //}
                //else
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
                    foreach (int pattern in BitEnumerators.AllOdd(width - 1))
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
