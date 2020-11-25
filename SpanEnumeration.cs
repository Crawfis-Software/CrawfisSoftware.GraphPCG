using System.Collections;
using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Enumerate all valid path fragments 
    /// </summary>
    /// <remarks>Bit order for a span goes from right to left. N...0</remarks>
    public class SpanEnumeration : IEnumerable<int>
    {
        private readonly int start;
        private readonly OutflowState startStates;
        private readonly int end;
        private readonly OutflowState endStates;
        private readonly int width;
        public SpanEnumeration(int start, OutflowState startStates, int end, OutflowState endStates)
        {
            this.start = start;
            this.startStates = startStates;
            this.end = end;
            this.endStates = endStates;
            //width = end - start + 1;
            width = end - start;  // All patterns add the final bit.
        }

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
