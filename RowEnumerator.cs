using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    public static class RowEnumerator
    {
        private static IList<IList<short>> preComputedRowTables;
        private static int tableWidth;
        private static IList<short> RowList(int width, int row)
        {
            if (width == tableWidth)
                return preComputedRowTables[row];
            return null;
        }
        public static void BuildOddTables(int width = 12)
        {
            if (width > 16)
                throw new ArgumentOutOfRangeException("row widths greater than 16 would take too much memory");
            tableWidth = width;
            int tableSize = (1 << width);
            preComputedRowTables = new IList<short>[tableSize];
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
            foreach (int child in RowEnumerator.ValidRows(width, row))
            {
                validRows.Add((short)child);
            }
            preComputedRowTables[row] = validRows;
        }

        public static void BuildEvenTables(int width = 12)
        {
            if (width > 16)
                throw new ArgumentOutOfRangeException("row widths greater than 16 would take too much memory");
            tableWidth = width;
            int tableSize = (1 << width);
            preComputedRowTables = new IList<short>[tableSize];
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

        public static int GetRandomRow(int width, int inFlows, System.Random random)
        {
            if (preComputedRowTables == null)
                throw new InvalidOperationException("Tables must be built first. Odd tables for paths, even for loops");
            var rowList = RowList(width, inFlows);
            if (rowList == null)
            {
                throw new ArgumentException("The number of inFlows (bits in the inFlows variable) is not odd, or width is too large");
            }
            int count = rowList.Count;
            int randomIndex = random.Next(0, count);
            return rowList[randomIndex];
        }
        
        internal static IList<short> CandidateRows(int width, int inFlows)
        {
            if (preComputedRowTables == null)
                throw new InvalidOperationException("Tables must be built first. Odd tables for paths, even for loops");
            var rowList = RowList(width, inFlows);
            if (rowList == null)
            {
                throw new ArgumentException("The number of inFlows (bits in the inFlows variable) is not odd, or width is too large");
            }
            return rowList;
        }
        public static IEnumerable<int> ValidRows(int width, int inFlows)
        {
            // Find all possible merges.
            // Specify all possible states for each merge configuration.
            // Use ValidRows with states to enumerate all resulting rows.
            if (inFlows == 0)
                yield break;
            var rowList = RowList(width, inFlows);
            if (rowList != null)
            {
                foreach (int row in rowList)
                    yield return row;
                yield break;
            }
            var inFlowPositions = InflowsFromBits(width, inFlows);
            var components = new List<int>(width);
            // any set of unique numbers will work. Just trying to avoid any merge rejects
            for (int i = 0; i < width; i++)
                components.Add( i + 1);
            var possibleOutFlows = OutflowStates.DetermineOutflowStates(width, inFlowPositions, components);
            foreach (var state in possibleOutFlows)
            {
                foreach (int row in ValidRowsFixedFlowStates(width, inFlowPositions, state))
                {
                    yield return row;
                }
            }
        }

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

        public static IEnumerable<List<OutflowState>> MergeComponents(int startIndex, int endIndex, IList<int> inFlows, List<OutflowState> rowState)
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

        //public static IEnumerable<List<OutflowState>> MergeComponents(int startIndex, int endIndex, IList<int> inFlows, IList<int> components, List<OutflowState> rowState)
        //{
        //    if ((endIndex - startIndex) < 2)
        //        yield break;
        //    var newStates = new List<OutflowState>(rowState);
        //    int width = components.Count;
        //    //yield return newStates;
        //    for (int i = startIndex; i < endIndex; i++)
        //    {
        //        int pos1 = inFlows[i];
        //        int pos2 = inFlows[i + 1];
        //        if (components[pos1] == components[pos2])
        //            continue;
        //        // Merge i and i+1 and recurse
        //        newStates[i] = OutflowState.DeadLeft;
        //        newStates[i + 1] = OutflowState.DeadRight;
        //        yield return newStates;
        //        foreach (var state in MergeComponents(startIndex, i - 1, inFlows, components, newStates))
        //        {
        //            yield return state;
        //        }
        //        foreach (var state in MergeComponents(i + 2, endIndex, inFlows, components, newStates))
        //        {
        //            yield return state;
        //        }
        //    }
        //}

        /// <summary>
        /// Enumerate all rows that have the desired inflows and outflow states (left, up, right). All merges
        /// are marked with states dead-goes-right and dead-goes-left.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="positions"></param>
        /// <param name="possibleOutflowStates"></param>
        /// <returns></returns>
        //public static IEnumerable<int> ValidRows(int width, List<int> positions, List<OutflowState> possibleOutflowStates)
        //{
        //    // Another set cross product problem {a,b,c,d} U {e,f} -> ae, af, be, bf, ... de. 
        //    // Note, this is NP as well O(4^N), where N is the number of inflows < 31
        //    var stateSets = new List<IEnumerable<OutflowState>>(possibleOutflowStates.Count);
        //    foreach(var flags in possibleOutflowStates)
        //    {
        //        var enumValues = EnumerableExtensions.EnumFlagSubsets<OutflowState>(flags);
        //        stateSets.Add(enumValues);
        //    }
        //    var crossProduct = EnumerableExtensions.CartesianProduct<OutflowState>(stateSets);
        //    foreach (var outflowState in crossProduct)
        //    {
        //        if (CheckForValidRightLeftCombo(width, positions, outflowState))
        //        {
        //            foreach (int row in ValidRowsFixedFlowStates(width, positions, outflowState))
        //            {
        //                yield return row;
        //            }
        //        }
        //    }
        //    yield break;
        //}

        public static IEnumerable<int> ValidRowsFixedFlowStates(int width, List<int> positions, IEnumerable<OutflowState> desiredOutflowState)
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
            }
            int currentIndex = 0;
            var spanEnumerator = SpanCombiner(0, 0, OutflowState.DeadGoesRight, inFlows[0], flowStates[0]).GetEnumerator();
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
        public static IEnumerable<int> SpanCombiner(int currentRow, int start, OutflowState startState, int end, OutflowState endState)
        {
            int rowPattern = currentRow;
            foreach(int spanPattern in new SpanEnumeration(0, startState, end-start, endState))
            {
                yield return (spanPattern << start) | rowPattern;
            }
        }
    }
}
