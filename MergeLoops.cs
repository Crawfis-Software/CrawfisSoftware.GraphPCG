using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace CrawfisSoftware.PCG
{
    static internal class MergeLoops
    {
        // Bug: This will produce duplicates
        static public IEnumerable<List<int>> MergeAnAdjacentPair(List<int> components)
        {
            for (int i=0; i < components.Count-1; i++)
            {
                List<int> newComponents = new List<int>(components);
                int ci = components[i];
                int ciPlus1 = components[i+1];
                if ((ci >= 0) && (ciPlus1 >= 0) && (ci != ciPlus1))
                {
                    int min = ci > ciPlus1 ? ciPlus1 : ci;
                    int max = ci == min ? ciPlus1 : ci;
                    newComponents[i] = -1;
                    newComponents[i + 1] = -2;
                    int matchingComponent = newComponents.FindIndex(x => x == max);
                    newComponents[matchingComponent] = min;
                    yield return newComponents;
                    foreach (var tuple in MergeAnAdjacentPair(newComponents))
                    {
                        yield return tuple;
                    }
                }
            }
        }
        private static IEnumerable<int> SpansOdd0(int width, int inBitLocation)
        {
            // If inBitLocation is the first bit, then nothing should be returned
            // as an odd number of bits is not possible before this location.
            if (inBitLocation > 0)
            {
                if (inBitLocation == width - 1)
                {
                    foreach (int pattern in BitEnumerators.AllOdd(width - 1))
                    {
                        yield return pattern << 1;
                    }
                }
                else
                {
                    foreach (int oddPattern in BitEnumerators.AllOdd(inBitLocation))
                    {
                        int oddPatternShifted = oddPattern << (width - inBitLocation);
                        foreach (int evenPattern in BitEnumerators.AllEven(width - inBitLocation - 1))
                        {
                            int pattern = oddPatternShifted + evenPattern;
                            yield return pattern;
                        }
                    }
                }
            }
        }
    }
}
