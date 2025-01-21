using System.Collections;
using System.Collections.Generic;

namespace CrawfisSoftware.Path.BitPattern
{
    /// <summary>
    /// Enumerate all valid path (or loop) fragments, given the desired outflow state of the
    /// last bit and the outflow start of the previous span (the start bit location).
    /// </summary>
    /// <remarks>Bit order for a span goes from right to left. N...0</remarks>
    public class SpanEnumeration : IEnumerable<int>
    {
        private readonly int start;
        private readonly OutflowState startStates;
        private readonly int end;
        private readonly OutflowState endStates;
        private readonly int width;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">The starting bit location (not included in the span).</param>
        /// <param name="startState">The OutflowState state of the start location (or previous span).</param>
        /// <param name="end">The ending bit location.</param>
        /// <param name="endState">The end bit's OutflowState.</param>
        internal SpanEnumeration(int start, OutflowState startState, int end, OutflowState endState)
        {
            this.start = start;
            this.startStates = startState;
            this.end = end;
            this.endStates = endState;
            //width = end - start + 1;
            width = end - start;  // All patterns add the final bit.
        }

        /// <inheritdoc/>
        public IEnumerator<int> GetEnumerator()
        {
            // Bug: All patterns should be shifted by start.
            if(width == 0)
            {
                // Only valid case is start has to be true for LeftUpOrDead and end can be Up (1)
                // or RightOrDead (0). Error checking is performed else where.
                if ((endStates & OutflowState.Up) == OutflowState.Up)
                    yield return 1 << start;
                else
                    yield return 0;
                yield break;
            }
            if((startStates & OutflowState.DeadGoesLeft) == OutflowState.DeadGoesLeft &&
                (endStates & OutflowState.DeadGoesRight) == OutflowState.DeadGoesRight) // clause not really needed.
            {
                yield return 0;
                yield break;
            }
            //bool startGoesLeftUpOrIsDead = (startStates & OutflowState.Left) == OutflowState.Left
            //    || (startStates & OutflowState.Up) == OutflowState.Up
            //    || (startStates & OutflowState.DeadLeft) == OutflowState.DeadLeft
            //    || (startStates & OutflowState.DeadRight) == OutflowState.DeadRight;
            bool startGoesRightUpOrIsDead = !((startStates & OutflowState.Left) == OutflowState.Left);
            bool startGoesLeft = (startStates & OutflowState.Left) == OutflowState.Left;
            // There are 6 cases, but R-R and L-(L|D) are the same - 0-Odd.
            if (startGoesRightUpOrIsDead)
            {
                if ((endStates & OutflowState.Right) == OutflowState.Right)
                {
                    // All 0 + Odd
                    foreach (int oddPattern in BitEnumerators.AllOdd(width))
                    {
                        yield return oddPattern << start;
                    }
                }
                if ((endStates & OutflowState.Up) == OutflowState.Up)
                {
                    // All 1 + Even
                    foreach (int evenPattern in BitEnumerators.AllEven(width))
                    {
                        yield return (evenPattern + (1<<width)) << start;
                    }
                }
                if ((endStates & OutflowState.Left) == OutflowState.Left 
                    || (endStates & OutflowState.DeadGoesLeft) == OutflowState.DeadGoesLeft)
                {
                    // All 0 + Even;
                    foreach (int evenPattern in BitEnumerators.AllEven(width))
                    {
                        yield return evenPattern << start;
                    }
                }
            }
            if (startGoesLeft)
            {
                if ((endStates & OutflowState.Right) == OutflowState.Right)
                {
                    // All Even with at least 2 bits != 0;
                    foreach (int evenPattern in BitEnumerators.AllEven(width))
                    {
                        if (evenPattern != 0)
                        {
                            yield return evenPattern << start;
                        }
                    }
                }
                if ((endStates & OutflowState.Up) == OutflowState.Up)
                {
                    // All 1 + Odd
                    foreach (int oddPattern in BitEnumerators.AllOdd(width))
                    {
                        yield return (oddPattern + (1<<width)) << start;
                    }
                }
                if ((endStates & OutflowState.Left) == OutflowState.Left
                    || (endStates & OutflowState.DeadGoesLeft) == OutflowState.DeadGoesLeft)
                {
                    // All 0 + Odd
                    foreach (int oddPattern in BitEnumerators.AllOdd(width))
                    {
                        yield return oddPattern << start;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
