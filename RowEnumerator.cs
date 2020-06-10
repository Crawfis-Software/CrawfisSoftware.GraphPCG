using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace CrawfisSoftware.PCG
{
    public static class RowEnumerator
    {
        public static IEnumerable<int> ValidRows(int width, List<int> positions, List<OutflowState> possibleOutflowStates)
        {
            var outflowState = new List<OutflowState>(possibleOutflowStates.Count);
            // Todo: Enumerate set of valid states.
            // Todo: check case where R->L and width == 2.
            // Another set cancat problem {a,b,c,d} U {e,f} -> ae, af, be, bf, ... de. 
            return ValidRowsFixedFlowStates(width, positions, outflowState);
        }
        public static IEnumerable<int> ValidRowsFixedFlowStates(int width, List<int> positions, List<OutflowState> desiredOutflowState)
        {
            // This is basically a depth-first tree traversal of the possible spans
            // going from left-to-right (down the tree). Foreach first span there
            // are a set of children associated with the set of second spans, for each
            // of these there is a set of children with the third span, etc.
            var inFlows = new List<int>(positions.Count + 1);
            foreach (int pos in positions)
                inFlows.Add(pos);
            var flowStates = new List<OutflowState>(desiredOutflowState.Count + 1);
            foreach (var state in desiredOutflowState)
                flowStates.Add(state);
            if (positions[positions.Count - 1] != width-1)
            {
                inFlows.Add(width);
                flowStates.Add(OutflowState.Dead);
            }
            width++;
            //int mask = (2 << (width-1)) -1;
            int currentIndex = 0;
            var spanEnumerator = SpanCombiner(width, 0, 0, OutflowState.Dead, inFlows[0], flowStates[0]).GetEnumerator();
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
                //Console.WriteLine();
                //Console.WriteLine(Convert.ToString(spanPattern, 2).PadLeft(10, '0'));
                //Console.WriteLine(Convert.ToString((spanPattern << shiftAmount), 2).PadLeft(10, '0'));
                //Console.WriteLine(Convert.ToString((spanPattern << shiftAmount) | rowPattern, 2).PadLeft(10, '0'));
                yield return (spanPattern << shiftAmount) | rowPattern;
            }
        }
    }
}
