using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Static class used to enumerate rows with an inFlow constraint.
    /// </summary>
    public static class ValidPathRowEnumerator
    {
        private static IList<IList<short>> preComputedRowTables;
        private static IList<IList<short>> preComputedOddRowTables;
        private static IList<IList<short>> preComputedEvenRowTables;
        private static int tableWidth;

        /// <summary>
        /// Get the valid vertical outputs given a vertical input. Valid outputs must have an odd number of bits.
        /// </summary>
        /// <param name="width">The width of the row</param>
        /// <param name="row">The inflow bits.</param>
        /// <returns>A list of possible outputs.</returns>
        public static IList<short> OddRowList(int width, int row)
        {
            if (width == tableWidth)
                return preComputedOddRowTables[row];
            return null;
        }

        /// <summary>
        /// Get the valid vertical outputs given a vertical input. Valid outputs must have an even number of bits.
        /// </summary>
        /// <param name="width">The width of the row</param>
        /// <param name="row">The inflow bits.</param>
        /// <returns>A list of possible outputs.</returns>
        public static IList<short> EvenRowList(int width, int row)
        {
            if (width == tableWidth)
                return preComputedEvenRowTables[row];
            return null;
        }

        /// <summary>
        /// Create pre-computed tables for paths, which have an odd number of inflows.
        /// </summary>
        /// <param name="width">The width of the row</param>
        public static void BuildOddTables(int width = 12)
        {
            if (width > 16)
                throw new ArgumentOutOfRangeException("row widths greater than 16 would take too much memory");
            tableWidth = width;
            int tableSize = (1 << width);
            preComputedRowTables = new IList<short>[tableSize];
            preComputedOddRowTables = preComputedRowTables;
            var taskList = new List<Task>();
            foreach (int row in BitEnumerators.AllOdd(width))
            {
                taskList.Add( //BuildTableEntryAsync(width, row));
                Task.Run(() =>
                {
                    BuildTableEntry(width, row);
                }
                ));
            
            }
            Task.WaitAll(Task.WhenAll(taskList));
        }

        private static void BuildTableEntry(int width, int row)
        {
            var validRows = new List<short>();
            //var inFlows = InflowsFromBits(width, row);
            foreach (int child in ValidPathRowEnumerator.ValidRows(width, row))
            {
                validRows.Add((short)child);
            }
            preComputedRowTables[row] = validRows;
        }

        /// <summary>
        /// Create pre-computed tables for loops, which have an even number of inflows.
        /// </summary>
        /// <param name="width">The width of the row</param>
        public static void BuildEvenTables(int width = 12)
        {
            if (width > 16)
                throw new ArgumentOutOfRangeException("row widths greater than 16 would take too much memory");
            tableWidth = width;
            int tableSize = (1 << width);
            preComputedRowTables = new IList<short>[tableSize];
            preComputedEvenRowTables = preComputedRowTables;
            var taskList = new List<Task>();
            foreach (int row in BitEnumerators.AllEven(width))
            {
                taskList.Add( //BuildTableEntryAsync(width, row));
                Task.Run(() =>
                {
                    BuildTableEntry(width, row);
                }
                ));

            }
            Task.WaitAll(Task.WhenAll(taskList));
        }

        /// <summary>
        /// Create pre-computed tables for paths, which have an odd number of inflows.
        /// </summary>
        /// <param name="width">The width of the row</param>
        /// <param name="oracle">Function that returns true if this outflow configuration is desireable.</param>
        public static void BuildOddTablesWithConstraints(int width, Func<int, bool> oracle)
        {
            if (width > 16)
                throw new ArgumentOutOfRangeException("row widths greater than 16 would take too much memory");
            tableWidth = width;
            int tableSize = (1 << width);
            preComputedRowTables = new IList<short>[tableSize];
            preComputedOddRowTables = preComputedRowTables;
            var taskList = new List<Task>();
            foreach (int row in BitEnumerators.AllOdd(width))
            {
                if (oracle(row))
                {
                    taskList.Add( //BuildTableEntryAsync(width, row));
                    Task.Run(() =>
                    {
                        BuildTableEntryWithConstraints(width, row, oracle);
                    }
                    ));
                }
            }
            Task.WaitAll(Task.WhenAll(taskList));
        }

        /// <summary>
        /// Create pre-computed tables for loops or paths which do not have a start and end on opposite grid edges, which have an even number of inflows.
        /// </summary>
        /// <param name="width">The width of the row</param>
        /// <param name="oracle">Function that returns true if this outflow configuration is desireable.</param>
        public static void BuildEvenTablesWithConstraints(int width, Func<int, bool> oracle)
        {
            if (width > 16)
                throw new ArgumentOutOfRangeException("row widths greater than 16 would take too much memory");
            tableWidth = width;
            int tableSize = (1 << width);
            preComputedRowTables = new IList<short>[tableSize];
            preComputedEvenRowTables = preComputedRowTables;
            var taskList = new List<Task>();
            foreach (int row in BitEnumerators.AllEven(width))
            {
                if (oracle(row))
                {
                    taskList.Add( //BuildTableEntryAsync(width, row));
                    Task.Run(() =>
                    {
                        BuildTableEntryWithConstraints(width, row, oracle);
                    }
                    ));
                }
            }
            Task.WaitAll(Task.WhenAll(taskList));
        }

        private static void BuildTableEntryWithConstraints(int width, int row, Func<int, bool> oracle)
        {
            var validRows = new List<short>();
            //var inFlows = InflowsFromBits(width, row);
            foreach (int child in ValidPathRowEnumerator.ValidRows(width, row))
            {
                if (oracle(child))
                {
                    validRows.Add((short)child);
                }
            }
            preComputedRowTables[row] = validRows;
        }

        /// <summary>
        /// Get a random row that is a valid result with the given inflows.
        /// </summary>
        /// <param name="width">The width of the row</param>
        /// <param name="inFlows">A bit pattern of the inflows in the row</param>
        /// <param name="random">A random number generator</param>
        /// <returns></returns>
        public static int GetRandomRow(int width, int inFlows, System.Random random)
        {
            if (preComputedRowTables == null)
                throw new InvalidOperationException("Tables must be built first. Odd tables for paths, even for loops");
            var rowList = OddRowList(width, inFlows);
            if (rowList == null)
            {
                throw new ArgumentException("The number of inFlows (bits in the inFlows variable) is not correct, or width is too large");
            }
            int count = rowList.Count;
            int randomIndex = random.Next(0, count);
            return rowList[randomIndex];
        }
        
        internal static IList<short> CandidateRows(int width, int inFlows)
        {
            if (preComputedRowTables == null)
                throw new InvalidOperationException("Tables must be built first. Odd tables for paths, even for loops");
            var rowList = OddRowList(width, inFlows);
            if (rowList == null)
            {
                throw new ArgumentException("The number of inFlows (bits in the inFlows variable) is not odd, or width is too large");
            }
            return rowList;
        }

        /// <summary>
        /// Iterate over all valid rows given a row's inflows
        /// </summary>
        /// <param name="width">The width of the row</param>
        /// <param name="inFlows">A bit pattern of the inflows in the row</param>
        /// <param name="leftEdgeState">The Outflow state of the first outflow.</param>
        /// <param name="rightEdgeState">The outflow state for the last outflow.</param>
        /// <returns>An enumerable of integers that represent the state of the row.</returns>
        private static IEnumerable<int> ValidRows(int width, int inFlows,
            OutflowState leftEdgeState = OutflowState.DeadGoesRight, OutflowState rightEdgeState = OutflowState.DeadGoesLeft)
        {
            // Find all possible merges.
            // Specify all possible states for each merge configuration.
            // Use ValidRows with states to enumerate all resulting rows.
            if (inFlows == 0)
                yield break;
            var inFlowPositions = InflowsFromBits(width, inFlows);
            var components = new List<int>(width);
            // any set of unique numbers will work. Just trying to avoid any merge rejects
            for (int i = 0; i < width; i++)
                components.Add( i + 1);
            var possibleOutFlows = OutflowStates.DetermineOutflowStates(width, inFlowPositions, components);
            foreach (var outflowState in possibleOutFlows)
            {
                foreach (int row in ValidRowsFixedFlowStates(width, inFlowPositions, outflowState, leftEdgeState, rightEdgeState))
                {
                    yield return row;
                }
            }
        }

        /// <summary>
        /// Returns the column indices of the inflows of a row
        /// </summary>
        /// <param name="width">The width of the row</param>
        /// <param name="row">The inflow bit pattern</param>
        /// <returns></returns>
        public static List<int> InflowsFromBits(int width, int row)
        {
            var inFlows = new List<int>();
            int mask = 1;
            for (int i = 0; i < width; i++)
            {
                //mask = 1 << (width - i - 1);
                if ((row & mask) == mask)
                {
                    inFlows.Add(i);
                }
                mask <<= 1;
            }
            return inFlows;
        }

        internal static IEnumerable<List<OutflowState>> MergeComponents(int startIndex, int endIndex, IList<int> inFlows, List<OutflowState> rowState)
        {
            if ((endIndex - startIndex) < 2)
                yield break;
            int numberOfInFlows = inFlows.Count;
            var newStates = new List<OutflowState>(rowState);
            //yield return newStates;
            for (int i = startIndex; i < endIndex; i++)
            {
                for(int j = 0; j < numberOfInFlows; j++)
                    newStates[j] = rowState[j];
                // Merge i and i+1 and recurse
                newStates[i] = OutflowState.DeadGoesLeft;
                newStates[i + 1] = OutflowState.DeadGoesRight;
                yield return newStates;
                foreach (var state in MergeComponents(startIndex, i - 1, inFlows, newStates))
                {
                    yield return state;
                }
                foreach (var state in MergeComponents(i + 2, endIndex, inFlows, newStates))
                {
                    yield return state;
                }
            }
        }

        internal static IEnumerable<int> ValidRowsFixedFlowStates(int width, List<int> positions, IEnumerable<OutflowState> desiredOutflowState,
            OutflowState leftEdgeState = OutflowState.DeadGoesRight, OutflowState rightEdgeState = OutflowState.DeadGoesLeft)
        {
            // This is basically a depth-first tree traversal of the possible spans
            // going from left-to-right (down the tree). Foreach first span there
            // are a set of children associated with the set of second spans, for each
            // of these there is a set of children with the third span, etc.
            if (positions.Count == 0) yield break;
            var inFlows = new List<int>(positions.Count + 1);
            foreach (int pos in positions)
                inFlows.Add(pos);
            var flowStates = new List<OutflowState>(positions.Count + 1);
            foreach (var state in desiredOutflowState)
                flowStates.Add(state);
            if (positions[positions.Count - 1] != width-1)
            {
                inFlows.Add(width);
                flowStates.Add(OutflowState.DeadGoesLeft);
                flowStates.Add(rightEdgeState);
            }
            int currentIndex = 0;
            var spanEnumerator = SpanCombiner(0, 0, leftEdgeState, inFlows[0], flowStates[0]).GetEnumerator();
            var stack = new Stack<IEnumerator<int>>();
            var indexStack = new Stack<int>();
            var shiftStack = new Stack<int>();
            int currentShiftAmount = 0;
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
                        shiftStack.Push(currentShiftAmount);
                        currentIndex++; // next span
                        spanEnumerator = SpanCombiner(currentRow, inFlows[currentIndex-1]+1, flowStates[currentIndex-1], inFlows[currentIndex], flowStates[currentIndex]).GetEnumerator();
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
        internal static IEnumerable<int> SpanCombiner(int currentRow, int start, OutflowState startState, int end, OutflowState endState)
        {
            int rowPattern = currentRow;
            //foreach(int spanPattern in new SpanEnumeration(0, startState, end-start, endState))
            foreach (int spanPattern in new SpanEnumeration(start, startState, end, endState))
            {
                //yield return (spanPattern << start) | rowPattern;
                yield return spanPattern | rowPattern;
            }
        }
    }
}
