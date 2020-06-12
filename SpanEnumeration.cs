using System.Collections;
using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    internal class SpanEnumeration : IEnumerable<int>
    {
        private readonly int start;
        private readonly OutflowState startStates;
        private readonly int end;
        private readonly OutflowState endStates;
        private readonly int width;
        internal SpanEnumeration(int start, OutflowState startStates, int end, OutflowState endStates)
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
            if(width == 0)
            {
                // Only valid case is start has to be true for LeftUpOrDead and end can be Up (1)
                // or RightOrDead (0). Error checking is performed else where.
                if ((endStates & OutflowState.Up) == OutflowState.Up)
                    yield return 1;
                else
                    yield return 0;
                yield break;
            }
            if((startStates & OutflowState.DeadLeft) == OutflowState.DeadLeft &&
                (endStates & OutflowState.DeadRight) == OutflowState.DeadRight) // clause not really needed.
            {
                yield return 0;
                yield break;
            }    
            bool startGoesLeftUpOrIsDead = (startStates & OutflowState.Left) == OutflowState.Left
                || (startStates & OutflowState.Up) == OutflowState.Up
                || (startStates & OutflowState.DeadLeft) == OutflowState.DeadLeft
                || (startStates & OutflowState.DeadRight) == OutflowState.DeadRight;
            bool startGoesRight = (startStates & OutflowState.Right) == OutflowState.Right;
            // There are 6 cases, but L-L and R-(R|D) are the same - Odd0.
            if (startGoesLeftUpOrIsDead)
            {
                if ((endStates & OutflowState.Left) == OutflowState.Left)
                {
                    // All Odd + 0;
                    foreach (int oddPattern in BitEnumerators.AllOdd(width))
                    {
                        yield return oddPattern << 1;
                    }
                }
                if ((endStates & OutflowState.Up) == OutflowState.Up)
                {
                    // All Even + 1;
                    foreach (int evenPattern in BitEnumerators.AllEven(width))
                    {
                        yield return (evenPattern << 1) + 1;
                    }
                }
                if ((endStates & OutflowState.Right) == OutflowState.Right 
                    || (endStates & OutflowState.DeadLeft) == OutflowState.DeadLeft)
                {
                    // All Even + 0;
                    foreach (int evenPattern in BitEnumerators.AllEven(width))
                    {
                        yield return evenPattern << 1;
                    }
                }
            }
            if (startGoesRight)
            {
                if ((endStates & OutflowState.Left) == OutflowState.Left)
                {
                    // All Even with at least 2 bits + 0;
                    foreach (int evenPattern in BitEnumerators.AllEven(width))
                    {
                        if (evenPattern != 0)
                        {
                            yield return evenPattern << 1;
                        }
                    }
                }
                if ((endStates & OutflowState.Up) == OutflowState.Up)
                {
                    // All Odd + 1;
                    foreach (int oddPattern in BitEnumerators.AllOdd(width))
                    {
                        yield return (oddPattern << 1) + 1;
                    }
                }
                if ((endStates & OutflowState.Right) == OutflowState.Right
                    || (endStates & OutflowState.DeadLeft) == OutflowState.DeadLeft)
                {
                    // All Odd + 0;
                    foreach (int oddPattern in BitEnumerators.AllOdd(width))
                    {
                        yield return oddPattern << 1;
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
