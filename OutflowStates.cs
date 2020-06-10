using System;
using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    [Flags]
    public enum OutflowState { Left = 1, Right = 2, Up = 4, Dead = 8 };

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
        /// <remarks>The case of a single space between to inflows is not handled here. So the
        /// set of possibilities may be slightly less if positions[i]+2 == positions[i+1]
        /// as the combination go right (for i) and go left (for i+1) is invalid. This is handled in
        /// the span enumeration once we fix a state for inflow i.</remarks>
        internal static List<OutflowState> DetermineOutflowStates(List<int> positions, List<int> components)
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
                    validStates = OutflowState.Dead;
                outflowStates.Add(validStates);
                initialPosition = positions[i];
            }
            return outflowStates;
        }
    }
}
