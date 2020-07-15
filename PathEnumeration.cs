using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    public static class PathEnumeration
    {
        public static IEnumerable<IList<int>> AllPaths(int width, int height, int start, int end)
        {
            var inFlow = new List<int>() { start };
            var validStates = OutflowState.Up;
            if (start > 0) validStates |= OutflowState.Left;
            if (start < width - 1) validStates |= OutflowState.Right;
            var outFlowStates = new List<OutflowState>() { validStates };
            int rowIndex = 0;
            int previousRow = 1 << start;
            //previousRow = 19;
            foreach (int row in RowEnumerator.ValidRows(width, inFlow, outFlowStates))
            //foreach (int row in RowEnumerator.ValidRows(width, previousRow))
            {
                var verticalPaths = new int[height];
                var components = new int[width];
                components[start] = 1;
                //components[1] = 1;
                //components[4] = 2;
                //components[3] = 2;
                if (ValidateAndUpdateComponents(previousRow, row, ref components, height - 1))
                {
                    verticalPaths[0] = row;
                    foreach (var secondRow in RowEnumerator.ValidRows(width, row, components))
                    {
                        var newComponents = new int[width];
                        for (int i = 0; i < width; i++)
                            newComponents[i] = components[i];
                        if (ValidateAndUpdateComponents(row, secondRow, ref newComponents, height - 2))
                        {
                            verticalPaths[1] = secondRow;
                            yield return verticalPaths;
                        }
                    }
                    //yield return verticalPaths;
                }
            }
            yield break;
        }
        public static bool ValidateAndUpdateComponents(int inFlows, int outFlows, ref int[] components, int maxNestedComponents = System.Int16.MaxValue)
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
            int width = components.Length;
            var newOutflowComponents = new int[width];
            var componentRemap = new Dictionary<int, int>();
            int a = -1;
            int d = -1;
            int b = 0;
            int inFlowBitPattern = inFlows;
            int outFlowBitPattern = outFlows;
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
            while ((spanStart < width) && (spanLength > 0))
            {
                int e = d+1;
                int span = 0;
                int componentB = (b < width) ? components[b] : 0;
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
                int addedComponentNum = width; // Some number larger than all other component numbers (for now)
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
                            break;
                        }
                        else
                        {
                            numOfOutflowsInSpan -= 1;
                            newOutflowComponents[i + spanStart] = addedComponentNum;
                        }
                        if (!rightEdge) addedComponentNum++;
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
                // b's Inflow goes to the Right
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
                            break;
                        }
                        outFlowBitPattern >>= 1;
                        e++;
                    }
                    // No match, b and c form a closed loop. Check if valid
                    if (!matched && (c < width))
                    {
                        int componentC = components[c];
                        if (componentRemap.TryGetValue(componentC, out tempComponentNum)) componentC = tempComponentNum;
                        if (componentB == componentC) 
                            isValid = false;
                        else
                        {
                            // Remap component c to b.
                            componentRemap[componentC] = componentB;
                        }
                        // Update d and c
                        d = e;
                        c++;
                        while (c < width)
                        {
                            if ((inFlowBitPattern & 1) == 1) break;
                            inFlowBitPattern >>= 1;
                            c++;
                        }
                    }
                }
                // Reset a and d
                //while (b < width)
                //{
                //    if ((inFlowBitPattern % 2) == 1) break;
                //    inFlowBitPattern >>= 1;
                //    b++;
                //}
                a = b;
                if(matched)
                    d = e;
                b = c;
                spanStart = (a > d) ? a + 1 : d+1;
                spanLength = b - spanStart + 1;
            }

            // Renumber components left to right
            int lastMatched = 1;
            componentRemap.Clear();
            int newComponentNum = 1;
            for(int i = 0; i < width; i ++)
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
                        if(componentRemap[componentNum] - lastMatched > maxNestedComponents)
                        {
                            isValid = false;
                            break;
                        }
                        lastMatched = componentRemap[componentNum];
                    }
                    newOutflowComponents[i] = componentRemap[componentNum];
                }
            }
            components = newOutflowComponents;
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
