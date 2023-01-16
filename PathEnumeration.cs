using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Static class to enumerate paths
    /// </summary>
    public static class PathEnumeration
    {
        /// <summary>
        /// Iterate over all non-cyclical paths from a starting cell to an ending cell on an open verticalGrid.
        /// </summary>
        /// <param name="width">The width of the underlying verticalGrid.</param>
        /// <param name="height">The height of the underlying verticalGrid</param>
        /// <param name="start">The column index of the starting cell on the first row (row 0).</param>
        /// <param name="end">The column index of the ending cell on the last row (row height-1)</param>
        /// <param name="globalConstraintsOracle">Optional function to specify some global constraints on the outflows of a row.</param>
        /// <param name="rowCandidateOracle">Function that returns true or false whether this row is desired. Parameters are: the pathID, the row number,
        /// the current candidate row value (vertical bits), all verticalBits so far, all horizontal bits so far, all components so far.</param>
        /// <param name="horizontalCandidateOracle">Function that returns true or false whether this row is desired. Parameters are: the pathID, the row number,
        /// the current candidate value (horizontal bits), all verticalBits so far, all horizontal bits so far, all components so far.</param>
        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public static IEnumerable<(IList<int> vertical, IList<int> horizontal)> AllPaths(int width, int height, int start, int end, 
            Func<int, bool> globalConstraintsOracle = null, Func<int, int, int, IList<int>, IList<int>, IList<IList<int>>, bool> rowCandidateOracle = null,
            Func<int, int, int, IList<int>, IList<int>, IList<IList<int>>, bool> horizontalCandidateOracle = null)
        {
            if (globalConstraintsOracle == null)
            {
                ValidPathRowEnumerator.BuildOddTables(width);
            }
            else
            {
                ValidPathRowEnumerator.BuildOddTablesWithConstraints(width, globalConstraintsOracle);
            }
            int pathID = 0;
            var inFlow = new List<int>() { start };
            //var validStates = OutflowState.Up;
            //if (start > 0) validStates |= OutflowState.Left;
            //if (start < width - 1) validStates |= OutflowState.Right;
            //var outFlowStates = new List<OutflowState>() { validStates };
            var outFlowStates = OutflowStates.DetermineOutflowStates(width, inFlow);
            //foreach (var outFlowState in outFlowStates)
            {
                //foreach (int row in RowEnumerator.ValidRowsFixedFlowStates(width, inFlow, outFlowState))
                //foreach (int row in RowEnumerator.ValidRows(width, previousRow))
                {
                    var verticalPaths = new int[height];
                    var horizontalPaths = new int[height];
                    int[][] components = new int[height][];
                    for (int i = 0; i < height; i++)
                        components[i] = new int[width];
                    components[0][start] = 1;
                    verticalPaths[0] = 1 << start; // row;
                    int endRow = 1 << end;
                    verticalPaths[height - 1] = endRow;
                    foreach (var grid in AllPathRecursive(width, height, 0, verticalPaths, horizontalPaths, components, pathID, rowCandidateOracle, horizontalCandidateOracle))
                    {
                        yield return grid;
                    }
                }
            }
            yield break;
        }

        private static IEnumerable<(IList<int> vertical, IList<int> horizontal)> AllPathRecursive(int width, int height, int index, IList<int> verticalGrid, IList<int> horizontalGrid, 
            IList<IList<int>> components, int pathID, 
            Func<int, int, int, IList<int>, IList<int>, IList<IList<int>>, bool> rowCandidateOracle = null,
            Func<int, int, int, IList<int>, IList<int>, IList<IList<int>>, bool> horizontalCandidateOracle = null)
        {
            int horizontalSpans;
            if (index == (height - 2))
            {
                if (ValidateAndUpdateComponents(verticalGrid[index], verticalGrid[height - 1], components, index, out horizontalSpans))
                {
                    if (horizontalCandidateOracle == null || horizontalCandidateOracle(pathID, height - 1, horizontalSpans, verticalGrid, horizontalGrid, components))
                    {
                        horizontalGrid[height - 1] = horizontalSpans;
                        yield return (verticalGrid, horizontalGrid);
                    }
                }
                yield break;
            }
            // Todo: Compute all Valid OutflowStates (using components)
            //  Loop over those calling a more constrained ValidRows.
            int inFlow = verticalGrid[index];
            var inFlows = ValidPathRowEnumerator.InflowsFromBits(width, inFlow);
            var inFlowComponents = new List<int>(inFlows.Count);
            for (int i = 0; i < inFlows.Count; i++)
                inFlowComponents.Add(components[index][inFlows[i]]);
            foreach (var child in ValidPathRowEnumerator.OddRowList(width, verticalGrid[index]))
            {
                if (rowCandidateOracle == null || rowCandidateOracle(pathID, index+1, child, verticalGrid, horizontalGrid, components))
                {
                    verticalGrid[index + 1] = child;
                    if (ValidateAndUpdateComponents(inFlow, child, components, index, out horizontalSpans, height - index))
                    {
                        if (horizontalCandidateOracle == null || horizontalCandidateOracle(pathID, index + 1, horizontalSpans, verticalGrid, horizontalGrid, components))
                        {
                            horizontalGrid[index + 1] = horizontalSpans;
                            foreach (var newGrid in AllPathRecursive(width, height, index + 1, verticalGrid, horizontalGrid, components, pathID++, rowCandidateOracle, horizontalCandidateOracle))
                            {
                                yield return newGrid;
                            }
                        }
                    }
                }
            }
            yield break;
        }

        /// <summary>
        /// Checks two rows to see if they are valid. If so, components from the first row are matched (or merged) and
        /// new component numbers are created (as well as new loops). 
        /// </summary>
        /// <param name="inFlows">Incoming row of vertical edges</param>
        /// <param name="outFlows">Outgoing row of vertical edges</param>
        /// <param name="componentsGrid">The verticalGrid of component numbers for each inflow edge on each row</param>
        /// <param name="index">The current row index</param>
        /// <param name="horizontalSpans">A bit vector of new horizontal edges created by the component matching,
        /// merging and creation</param>
        /// <param name="maxNestedComponents">A constraint to check on the maximum allowed nested loops for this row.</param>
        /// <returns>True is the outFlows row is a valid row based on the inFlows row.</returns>
        internal static bool ValidateAndUpdateComponents(int inFlows, int outFlows, IList<IList<int>> componentsGrid, int index, out int horizontalSpans, int maxNestedComponents = System.Int16.MaxValue)
        {
            // Given:
            //    a = last known inflow and a matching outflow of d
            //    b = next inflow that we are trying to match or merge.
            //    c = next inflow after b, which marks a boundary for our match.
            //    d = last known outflow, which is matched to inflow a.
            //    e = the next outflow we are trying to match b to.
            // Rules
            //    1) If no outflows from d until c-1 (e >= c), then the inflows b and c were merged (an outflow at c would thus be an error).
            //    2) if the number of outflow bits from max(a,d)+1 to b is odd, then b matches with last outflow. All others are new components (in pairs).
            //    3) if the number of outflow bits from max(a,d)+1 to b is even, then these are all new components (in pairs). Note outflow at b must be zero.
            //    4) if no match still and e < c, then match b to e.
            //
            bool isValid = true;
            IList<int> components = componentsGrid[index];
            int width = components.Count;
            var newOutflowComponents = componentsGrid[index+1]; // new int[width];
            for (int i = 0; i < width; i++)
                newOutflowComponents[i] = 0;
            var componentRemap = new Dictionary<int, int>();
            int a = -1;
            int d = -1;
            int b = 0;
            int inFlowBitPattern = inFlows;
            int outFlowBitPattern = outFlows;
            horizontalSpans = 0;
            int addedComponentNum = width; // Some number larger than all other component numbers (for now)
            // Find first outflow bit (b)
            while (b < width)
            {
                if ((inFlowBitPattern & 1) == 1) break;
                inFlowBitPattern >>= 1;
                b++;
            }
            inFlowBitPattern >>= 1;
            int spanStart = a + 1;
            int spanLength = b - spanStart + 1;
            while ((spanStart < width) && (spanLength > 0) )
            {
                int e = d+1;
                int span = 0;
                int componentB = (b < width) ? components[b] : 0;
                //if (componentB == 0) throw new InvalidOperationException("ComponentB is zero!");
                int tempComponentNum;
                if (componentRemap.TryGetValue(componentB, out tempComponentNum)) componentB = tempComponentNum;
                int numOfOutflowsInSpan = 0;
                if (spanLength > 0)
                {
                    span = TrimToSpan(outFlows, spanStart, b);
                    numOfOutflowsInSpan = CountSetBits(span);
                    outFlowBitPattern = outFlows >> spanStart;
                }
                bool rightEdge = true;
                bool matched = false;
                int mask = 1; // << (width - 1);
                // add any extra outflow pairs as new components, if odd number of bits, match b to the last outflow bit.
                for (int i = 0; i < spanLength; i++)
                {
                    if (numOfOutflowsInSpan == 0) break;
                    if ((span & mask) == mask)
                    {
                        if (rightEdge && numOfOutflowsInSpan == 1)
                        {
                            e = i + spanStart;
                            newOutflowComponents[e] = componentB;
                            matched = true;
                            int bitPattern = ((1 << (b - e)) - 1) << e;
                            if (bitPattern < 0) throw new InvalidOperationException("Horizontal bit pattern is negative!");
                            horizontalSpans = horizontalSpans | bitPattern;
                            break;
                        }
                        //else
                        {
                            numOfOutflowsInSpan -= 1;
                            newOutflowComponents[i + spanStart] = addedComponentNum;
                        }
                        if (!rightEdge)
                        {
                            // new loop 
                            addedComponentNum++;
                            e = i + spanStart;
                            int bitPattern = ((1 << (e - d)) - 1) << d;
                            if (bitPattern < 0) throw new InvalidOperationException("Horizontal bit pattern is negative!");
                            horizontalSpans = horizontalSpans | bitPattern;
                        }
                        d = i + spanStart;
                        rightEdge = !rightEdge;
                    }
                    mask = mask << 1;
                }
                int c = b + 1;
                while (c < width)
                {
                    if ((inFlowBitPattern & 1) == 1) break;
                    inFlowBitPattern >>= 1;
                    c++;
                }
                inFlowBitPattern >>= 1;
                // b's Inflow goes to the Left
                // Try to match b to the next outFlow bit.
                if (!matched)
                {
                    outFlowBitPattern = outFlowBitPattern >> spanLength;
                    e = b + 1;
                    while (e < c)
                    {
                        if ((outFlowBitPattern & 1) == 1)
                        {
                            newOutflowComponents[e] = componentB;
                            matched = true;
                            int bitPattern = ((1 << (e - b)) - 1) << b;
                            if (bitPattern < 0) throw new InvalidOperationException("Horizontal bit pattern is negative!");
                            horizontalSpans = horizontalSpans | bitPattern;
                            break;
                        }
                        outFlowBitPattern >>= 1;
                        e++;
                    }
                    // No match, b and c form a closed loop. Check if valid
                    if (!matched && (c < width))
                    {
                        int componentC = components[c];
                        if (componentC == 0) throw new InvalidOperationException("ComponentC is zero!");
                        if (componentRemap.TryGetValue(componentC, out tempComponentNum)) componentC = tempComponentNum;
                        if (componentB == componentC) 
                            isValid = false;
                        else
                        {
                            // Remap component c to b.
                            componentRemap[componentC] = componentB;
                            int bitPattern = ((1 << (c - b)) - 1) << b;
                            if (bitPattern < 0) throw new InvalidOperationException("Horizontal bit pattern is negative!");
                            horizontalSpans = horizontalSpans | bitPattern;
                        }
                        // Update d and c
                        d = e;
                        b = c;
                        c++;
                        while (c < width)
                        {
                            if ((inFlowBitPattern & 1) == 1) break;
                            inFlowBitPattern >>= 1;
                            c++;
                        }
                        inFlowBitPattern >>= 1;
                    }
                }
                a = b;
                if(matched)
                    d = e;
                b = c;
                spanStart = (a > d) ? a + 1 : d+1;
                spanLength = b - spanStart + 1;
            }

            // Renumber components left to right
            for (int i = 0; i < width; i++)
            {
                int componentNum = newOutflowComponents[i];
                if (componentNum != 0)
                {
                    if (componentRemap.ContainsKey(componentNum))
                    {
                        newOutflowComponents[i] = componentRemap[componentNum];
                    }
                }
            }
            int lastMatched = 0;
            componentRemap.Clear();
            int newComponentNum = 1;
            for (int i = 0; i < width; i++)
            {
                int componentNum = newOutflowComponents[i];
                if (componentNum != 0)
                {
                    if (!componentRemap.ContainsKey(componentNum))
                    {
                        componentRemap[componentNum] = newComponentNum++;
                    }
                    else
                    {
                        if (componentRemap[componentNum] - lastMatched > maxNestedComponents)
                        {
                            isValid = false;
                            break;
                        }
                        lastMatched = componentRemap[componentNum];
                    }
                    newOutflowComponents[i] = componentRemap[componentNum];
                }
            }
            //components = newOutflowComponents;
            return isValid;
        }

        private static int TrimToSpan(int bitPattern, int start, int end)
        {
            int trimmedPattern = bitPattern >> start;
            int mask = (1 << (end - start + 1)) - 1;
            return (mask & trimmedPattern);
        }

        static int CountSetBits(int n)
        {
            int count = 0;
            while (n > 0)
            {
                n &= (n - 1);
                count++;
            }
            return count;
        }
    }
}
