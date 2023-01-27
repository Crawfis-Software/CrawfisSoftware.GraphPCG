using System;
using System.Collections.Generic;
using System.Linq;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Enum of possible directions an inflow can go.
    /// </summary>
    [Flags]
    // Bug: Or bad design. Why is this a Flag? They are mutually exclusive.
    internal enum OutflowState { Left = 1, Right = 2, Up = 4, DeadGoesLeft = 8, DeadGoesRight = 16 };

    internal class OutflowStates
    {
        /// <summary>
        /// This method will eliminate invald states between two inflows based on positions and
        /// components. All possible combinations can then be tried for each span
        /// So there would be 3^N possible state cases. 
        /// </summary>
        /// <param name="positions">The positions of the inflows.</param>
        /// <param name="components">The component numbers of the inflows (after any merges).</param>
        /// <returns>A vector of states flags.</returns>
        /// <remarks>The case of a single space between two inflows is not handled here. So the
        /// set of possibilities may be slightly less if positions[i]+2 == positions[i+1]
        /// as the combination go right (for i) and go left (for i+1) is invalid. This is handled in
        /// the span enumeration once we fix a state for inflow i.</remarks>
        internal static List<OutflowState> OldDetermineOutflowStates(List<int> positions, List<int> components)
        {
            var outflowStates = new List<OutflowState>(positions.Count);
            var initialPosition = -1;
            for (int i = 0; i < positions.Count - 1; i++)
            {
                var validStates = OutflowState.Left | OutflowState.Right | OutflowState.Up;
                if (initialPosition == positions[i] - 1)
                    validStates &= ~OutflowState.Left;
                if (positions[i] + 1 == positions[i + 1])
                    validStates &= ~OutflowState.Right;
                if (components[i] == -1)
                    validStates = OutflowState.DeadGoesLeft;
                if (components[i] == -2)
                    validStates = OutflowState.DeadGoesRight;
                outflowStates.Add(validStates);
                initialPosition = positions[i];
            }
            return outflowStates;
        }
        /// <summary>
        /// Enumerate all rows that have the desired inflows and outflow states (left, up, right). All merges
        /// are marked with states dead-goes-right and dead-goes-left.
        /// </summary>
        /// <param name="width">The width of the row.</param>
        /// <param name="positions">The positions of the inflows.</param>
        /// <param name="components">List of component numbers for each position.</param>
        /// <returns>An Enumerable of the valid row directions for each inflow position.</returns>
        public static IEnumerable<IEnumerable<OutflowState>> DetermineOutflowStates(int width, List<int> positions, List<int> components)
        {
            IEnumerable<IList<OutflowState>> allStates = DetermineOutflowStates(width, positions);
            foreach (var states in allStates)
            {
                yield return states;
                foreach (var state in MergeComponents(0, positions.Count - 1, positions, components, states))
                    yield return state;
            }
            yield break;
        }
        /// <summary>
        /// Enumerate all rows that have the desired inflows and outflow states (left, up, right). All merges
        /// are marked with states dead-goes-right and dead-goes-left.
        /// </summary>
        /// <param name="width">The width of the row.</param>
        /// <param name="positions">The positions of the inflows.</param>
        /// <returns>An Enumerable of the valid row directions for each inflow position.</returns>
        public static IEnumerable<IList<OutflowState>> DetermineOutflowStates(int width, List<int> positions)
        {
            List<OutflowState> possibleOutflowStates = DeterminePathTurns(width, positions);
            // Another set cross product problem {a,b,c,d} U {e,f} -> ae, af, be, bf, ... de. 
            // Note, this is NP as well O(4^N), where N is the number of inflows < 31
            var stateSets = new List<IEnumerable<OutflowState>>(possibleOutflowStates.Count);
            foreach (var flags in possibleOutflowStates)
            {
                var enumValues = EnumerableExtensions.EnumFlagSubsets<OutflowState>(flags);
                stateSets.Add(enumValues);
            }
            var crossProduct = EnumerableExtensions.CartesianProduct<OutflowState>(stateSets);
            foreach (var outflowState in crossProduct)
            {
                if (CheckForValidRightLeftCombo(width, positions, outflowState))
                {
                    yield return outflowState.ToList();
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
            foreach (var state in outflowState)
            {
                if (positions[index] == 0 && (state & OutflowState.Right) == OutflowState.Right)
                    return false;
                if (positions[index] == (width - 1) && (state & OutflowState.Left) == OutflowState.Left)
                    return false;
                if ((positions[index] - lastPosition) == 2)
                {
                    if ((lastState & OutflowState.Left) == OutflowState.Left &&
                        (state & OutflowState.Right) == OutflowState.Right)
                        return false;
                }
                lastState = state;
                lastPosition = positions[index];
                index++;
            }
            return true;
        }
        public static List<OutflowState> DeterminePathTurns(int width, List<int> inFlows)
        {
            var outFlowStates = new List<OutflowState>(inFlows.Count);
            int lastPosition = -1;
            for (int i = 0; i < inFlows.Count; i++)
            {
                OutflowState validStates = OutflowState.Up;
                int position = inFlows[i];
                int nextPosition = (i == inFlows.Count - 1) ? width : inFlows[i + 1];
                if (lastPosition + 1 < position)
                    validStates |= OutflowState.Right;
                if (position + 1 < nextPosition)
                    validStates |= OutflowState.Left;
                outFlowStates.Add(validStates);
                lastPosition = position;
            }
            return outFlowStates;
        }
        public static IEnumerable<List<OutflowState>> MergeComponents(int startIndex, int endIndex, IList<int> inFlows, IList<int> components, IList<OutflowState> rowState)
        {
            if ((endIndex - startIndex) < 2)
                yield break;
            var newStates = new List<OutflowState>(rowState);
            int width = components.Count;
            //yield return newStates;
            for (int i = startIndex; i < endIndex; i++)
            {
                for (int j = 0; j < rowState.Count; j++)
                    newStates[j] = rowState[j];
                if (components[i] == components[i+1])
                    continue;
                // Merge i and i+1 and recurse
                newStates[i] = OutflowState.DeadGoesLeft;
                newStates[i + 1] = OutflowState.DeadGoesRight;
                yield return newStates;
                foreach (var state in MergeComponents(startIndex, i - 1, inFlows, components, newStates))
                {
                    yield return state;
                }
                foreach (var state in MergeComponents(i + 2, endIndex, inFlows, components, newStates))
                {
                    yield return state;
                }
            }
        }
    }
}
