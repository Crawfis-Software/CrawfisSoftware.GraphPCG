using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    public static class SpanTester
    {
        public static IEnumerable<int> EnumerateLLSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Left, width - 1, OutflowState.Left);
            return spanEnumerator;
        }
        public static IEnumerable<int> EnumerateLUSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Left, width - 1, OutflowState.Up);
            return spanEnumerator;
        }
        public static IEnumerable<int> EnumerateLRSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Left, width - 1, OutflowState.Right);
            return spanEnumerator;
        }
        public static IEnumerable<int> EnumerateLDSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Left, width - 1, OutflowState.DeadGoesLeft);
            return spanEnumerator;
        }
        public static IEnumerable<int> EnumerateRLSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Right, width - 1, OutflowState.Left);
            return spanEnumerator;
        }
        public static IEnumerable<int> EnumerateRUSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Right, width - 1, OutflowState.Up);
            return spanEnumerator;
        }
        public static IEnumerable<int> EnumerateRRSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Right, width - 1, OutflowState.Right);
            return spanEnumerator;
        }
        public static IEnumerable<int> EnumerateRDSpans(int width)
        {
            var spanEnumerator = new SpanEnumeration(0, OutflowState.Right, width - 1, OutflowState.DeadGoesLeft);
            return spanEnumerator;
        }
    }
}
