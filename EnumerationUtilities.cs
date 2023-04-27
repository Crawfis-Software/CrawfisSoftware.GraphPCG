using System;
using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Static class to provide some useful enumeration utilities.
    /// </summary>
    public static class EnumerationUtilities
    {
        
        /// <summary>
        /// Defines a function that takes in the state of the Path enumeration including a 
        /// possible new row or vertical bits and returns true if the user wants to allow it, false otherwise.
        /// This is useful to define constraints on thepath enumeration.
        /// </summary>
        /// <param name="pathID">A unique pathID for each path being enumerated.</param>
        /// <param name="rowIndex">The index of the row currently being enumerated.</param>
        /// <param name="bitsToValidate">The candiate vertical bits to accept or reject.</param>
        /// <param name="verticalBitsGrid">The state of the vertical paths up to this row index.</param>
        /// <param name="horizontalBitsGrid">The state of the horizontal paths up to this row index.</param>
        /// <param name="componentsGrid">The state of the components up to this row index.</param>
        /// <returns>True if this row should be included in the enumeration. False otherwise.</returns>
        public delegate bool Validator(int pathID, int rowIndex, int bitsToValidate, 
            IList<int> verticalBitsGrid, IList<int> horizontalBitsGrid, IList<IList<int>> componentsGrid);
        
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
        public static bool ValidateAndUpdateComponents(int inFlows, int outFlows, IList<IList<int>> componentsGrid, 
            int index, out int horizontalSpans, int maxNestedComponents = System.Int16.MaxValue)
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
        
        /// <summary>
        /// Trim the bit pattern to the span between start and end (inclusive)
        /// </summary>
        /// <param name="bitPattern"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static int TrimToSpan(int bitPattern, int start, int end)
        {
            int trimmedPattern = bitPattern >> start;
            int mask = (1 << (end - start + 1)) - 1;
            return (mask & trimmedPattern);
        }

        /// <summary>
        /// Count the number of set bits in the bit pattern
        /// </summary>
        /// <param name="n"> Base 10 representation of bits</param>
        /// <returns></returns>
        public static int CountSetBits(int n)
        {
            int count = 0;
            while (n > 0)
            {
                n &= (n - 1);
                count++;
            }
            return count;
        }
        
        /// <summary>
        /// Generate a random bit pattern with an odd number of set bits under given width
        /// </summary>
        /// <param name="width"> Maximum number of digits permitted in a bit pattern</param>
        /// <param name="random"> Random number generator </param>
        /// <returns></returns>
        public static int RandomOddBitPattern(int width, Random random)
        {
            int max = (int) Math.Pow(2, width);
            int bitPattern = random.Next(max);
            while (CountSetBits(bitPattern) % 2 == 0)
            {
                bitPattern = random.Next(max);
            }

            return bitPattern;
        }
        
        /// <summary>
        /// Generate a random bit pattern with an even number of set bits under given width
        /// </summary>
        /// <param name="width"> Maximum number of digits permitted in a bit pattern</param>
        /// <param name="random"> Random number generator </param>
        /// <returns></returns>
        public static int RandomEvenBitPattern(int width, Random random)
        {
            int max = (int) Math.Pow(2, width);
            int bitPattern = random.Next(max);
            while (CountSetBits(bitPattern) % 2 != 0)
            {
                bitPattern = random.Next(max);
            }

            return bitPattern;
        }
        
    }
}