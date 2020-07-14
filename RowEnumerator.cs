using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace CrawfisSoftware.PCG
{
    public static class RowEnumerator
    {
        public static IEnumerable<int> ValidRows(int width, int inFlows, IList<int> components)
        {
            // Find all possible merges.
            // Specify all possible states for each merge configuration.
            // Use ValidRows with states to enumerate all resulting rows.
            var inFlowPositions = InflowsFromBits(width, inFlows);
            var possibleOutFlows = DeterminePathTurns(width, inFlowPositions);
            //foreach (int row in ValidRows(width, inFlowPositions, possibleOutFlows))
            //{
            //    yield return row;
            //}
            foreach (var validOutFlows in MergeComponents(0, possibleOutFlows.Count - 1, inFlowPositions, components, possibleOutFlows))
            {
                foreach (int row in ValidRows(width, inFlowPositions, validOutFlows))
                {
                    yield return row;
                }
            }
        }

        private static List<int> InflowsFromBits(int width, int row)
        {
            var inFlows = new List<int>();
            for (int i = 0; i < width; i++)
            {
                int mask = 1 << (width - i - 1);
                if ((row & mask) == mask)
                {
                    inFlows.Add(i);
                }
            }
            return inFlows;
        }

        private static List<OutflowState>  DeterminePathTurns(int width, List<int> inFlows)
        {
            var outFlowStates = new List<OutflowState>(inFlows.Count);
            int lastPosition = -1;
            for(int i = 0; i < inFlows.Count; i++)
            {
                OutflowState validStates = OutflowState.Up;
                int position = inFlows[i];
                int nextPosition = (i == inFlows.Count-1) ? width : inFlows[i + 1];
                if (lastPosition + 1 < position)
                    validStates |= OutflowState.Left;
                if (position + 1 < nextPosition)
                    validStates |= OutflowState.Right;
                outFlowStates.Add(validStates);
                lastPosition = position;
            }
            return outFlowStates;
        }
        private static IEnumerable<List<OutflowState>> MergeComponents(int startIndex, int endIndex, IList<int> inFlows, IList<int> components, List<OutflowState> rowState)
        {
            if ((endIndex - startIndex) < 2)
                yield break;
            var newStates = new List<OutflowState>(rowState);
            int width = components.Count;
            yield return newStates;
            for(int i = startIndex; i < (endIndex-1); i++)
            {
                int pos1 = width - 1 - inFlows[i];
                int pos2 = width - 1 - inFlows[i + 1];
                if (components[pos1] == components[pos2])
                    continue;
                // Merge i and i+1 and recurse
                newStates[i] = OutflowState.DeadLeft;
                newStates[i + 1] = OutflowState.DeadRight;
                yield return newStates;
                foreach (var state in MergeComponents(startIndex, i - 1, inFlows, components, newStates))
                {
                    yield return state;
                }
                foreach (var state in MergeComponents(i+2, endIndex, inFlows, components, newStates))
                {
                    yield return state;
                }
            }
        }

        /// <summary>
        /// Enumerate all rows that have the desired inflows and outflow states (left, up, right). All merges
        /// are marked with states dead-goes-right and dead-goes-left.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="positions"></param>
        /// <param name="possibleOutflowStates"></param>
        /// <returns></returns>
        public static IEnumerable<int> ValidRows(int width, List<int> positions, List<OutflowState> possibleOutflowStates)
        {
            // Another set cross product problem {a,b,c,d} U {e,f} -> ae, af, be, bf, ... de. 
            // Note, this is NP as well O(4^N), where N is the number of inflows < 31
            var stateSets = new List<IEnumerable<OutflowState>>(possibleOutflowStates.Count);
            foreach(var flags in possibleOutflowStates)
            {
                var enumValues = EnumerableExtensions.EnumFlagSubsets<OutflowState>(flags);
                stateSets.Add(enumValues);
            }
            var crossProduct = EnumerableExtensions.CartesianProduct<OutflowState>(stateSets);
            foreach (var outflowState in crossProduct)
            {
                if (CheckForValidRightLeftCombo(width, positions, outflowState))
                {
                    foreach (int row in ValidRowsFixedFlowStates(width, positions, outflowState))
                    {
                        yield return row;
                    }
                }
            }
            yield break;
        }

        private static bool CheckForValidRightLeftCombo(int width, List<int> positions, IEnumerable<OutflowState> outflowState)
        {
            // Check case where R->L and width == 2. Not enough room for both.
            int index = 0;
            var lastState = OutflowState.Up;
            int lastPosition = -3;
            foreach(var state in outflowState)
            {
                if (positions[index] == 0 && (state & OutflowState.Left) == OutflowState.Left)
                    return false;
                if (positions[index] == (width-1) && (state & OutflowState.Right) == OutflowState.Right)
                    return false;
                if ((positions[index] - lastPosition) == 2)
                {
                    if ((lastState & OutflowState.Right) == OutflowState.Right &&
                        (state & OutflowState.Left) == OutflowState.Left)
                        return false;
                }
                lastState = state;
                lastPosition = positions[index];
                index++;
            }
            return true;
        }

        public static IEnumerable<int> ValidRowsFixedFlowStates(int width, List<int> positions, IEnumerable<OutflowState> desiredOutflowState)
        {
            // This is basically a depth-first tree traversal of the possible spans
            // going from left-to-right (down the tree). Foreach first span there
            // are a set of children associated with the set of second spans, for each
            // of these there is a set of children with the third span, etc.
            var inFlows = new List<int>(positions.Count + 1);
            foreach (int pos in positions)
                inFlows.Add(pos);
            var flowStates = new List<OutflowState>(positions.Count + 1);
            foreach (var state in desiredOutflowState)
                flowStates.Add(state);
            if (positions[positions.Count - 1] != width-1)
            {
                inFlows.Add(width);
                flowStates.Add(OutflowState.DeadLeft);
            }
            width++;
            //int mask = (2 << (width-1)) -1;
            int currentIndex = 0;
            var spanEnumerator = SpanCombiner(width, 0, 0, OutflowState.DeadRight, inFlows[0], flowStates[0]).GetEnumerator();
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
                        yield return (currentRow >> 1);
                    }
                    else
                    {
                        stack.Push(spanEnumerator);
                        indexStack.Push(currentIndex);
                        shiftStack.Push(currentShiftAmount);
                        currentIndex++; // next span
                        spanEnumerator = SpanCombiner(width, currentRow, inFlows[currentIndex-1]+1, flowStates[currentIndex-1], inFlows[currentIndex], flowStates[currentIndex]).GetEnumerator();
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
        public static IEnumerable<int> SpanCombiner(int width, int currentRow, int start, OutflowState startState, int end, OutflowState endState)
        {
            int rowPattern = currentRow;
            int shiftAmount = width - end - 1;
            foreach(int spanPattern in new SpanEnumeration(0, startState, end-start, endState))
            {
                yield return (spanPattern << shiftAmount) | rowPattern;
            }
        }
    }
}
