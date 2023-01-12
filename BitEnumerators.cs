using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Two static utility methods for enumerating Bit patterns, plus an additional 
    /// set-up method that can be used to trade-off memory versus computation. If 
    /// performance is an issue, users should call ExpandPreComputedTables before 
    /// calling AllOdd or AllEven.
    /// </summary>
    public static class BitEnumerators
    {
        // Tables store values with an odd or even number of bits. Entry zero is ignored.
        private static int[][] oddNumberOfBitsTable = new int[][] { null, new int[] { 1 }, new int[] { 1, 2 } }; //, new int[] { 1, 2, 4, 7 } };
        private static int[][] evenNumberOfBitsTable = new int[][] { null, new int[] { 0 }, new int[] { 0, 3 } }; //, new int[] { 0, 3, 5, 6 } };
        //private static int tableSize = 4;
        private static int tableSize = 3;
        /// <summary>
        /// Algorithm to iterate over all bit patterns up to the specified width that have an
        /// even number of 1's.
        /// </summary>
        /// <param name="width">The number of bits in the patterns. Must be less than 31.</param>
        /// <returns>An enumerated stream of ints, where each value has an even number of 1's.</returns>
        /// <remarks>This method is NP in runtime, as is the number of return values!</remarks>
        public static IEnumerable<int> AllEven(int width)
        {
            if (width < tableSize)
            {
                if (evenNumberOfBitsTable[width] != null)
                {
                    foreach (int pattern in evenNumberOfBitsTable[width])
                    {
                        yield return pattern;
                    }
                }
                // else
                yield break;
            }
            // else
            int leadingOne = 1 << width - 1;
            foreach (int pattern in AllEven(width - 1))
            {
                yield return pattern;
            }
            foreach (int pattern in AllOdd(width - 1))
            {
                yield return leadingOne + pattern;
            }
        }
        /// <summary>
        /// Algorithm to iterate over all bit patterns up to the specified width that have an
        /// odd number of 1's.
        /// </summary>
        /// <param name="width">The number of bits in the patterns. Must be less than 31.</param>
        /// <returns>An enumerated stream of ints, where each value has an odd number of 1's.</returns>
        /// <remarks>This method is NP in runtime, as is the number of return values!</remarks>
        public static IEnumerable<int> AllOdd(int width)
        {
            if (width < tableSize)
            {
                if (oddNumberOfBitsTable[width] != null)
                {
                    foreach (int pattern in oddNumberOfBitsTable[width])
                    {
                        yield return pattern;
                    }
                }
                // else
                yield break;
            }
            // else - Uses Recursion until reaches the table size.
            int leadingOne = 1 << width - 1;
            foreach (int pattern in AllOdd(width - 1))
            {
                yield return pattern;
            }
            foreach (int pattern in AllEven(width - 1))
            {
                yield return leadingOne + pattern;
            }
        }
        // I don't want to use a BigArray or anything, so we need to keep each array under
        // the CLR limit of 2GB for an array.
        private const int maxTableSize = 28;
        /// <summary>
        /// Precompute the enumeration for small N.
        /// </summary>
        /// <param name="maxTablePatternSize">The size of the precomputed pattern you wish to precompute.
        /// Due to CLR array size limits, this value is clamped to 28.</param>
        /// <remarks>This algorithm is 1) NP in run time and 2) NP in memory!</remarks>
        public static void ExpandPreComputedTables(int maxTablePatternSize)
        {
            if (maxTablePatternSize <= tableSize)
                return; // Can only increase the table size.
            if (maxTablePatternSize > maxTableSize)
                maxTablePatternSize = maxTableSize;
            int[][] newEvenTable = new int[maxTablePatternSize + 1][];
            int[][] newOddTable = new int[maxTablePatternSize + 1][];
            // Populate the new tables with the existing table.
            for (int i = 0; i < tableSize; i++)
            {
                newEvenTable[i] = evenNumberOfBitsTable[i];
                newOddTable[i] = oddNumberOfBitsTable[i];
            }
            for (int i = tableSize; i <= maxTablePatternSize; i++)
            {
                newEvenTable[i] = BitEnumerators.AllEven(i).ToArray();
                newOddTable[i] = BitEnumerators.AllOdd(i).ToArray();
            }
            oddNumberOfBitsTable = newOddTable;
            evenNumberOfBitsTable = newEvenTable;
            tableSize = maxTablePatternSize + 1;
        }

        /// <summary>
        /// Returns the population count (number of bits set) of a mask.
        /// Similar in behavior to the x86 instruction POPCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(uint value)
        {
            const uint c1 = 0x_55555555u;
            const uint c2 = 0x_33333333u;
            const uint c3 = 0x_0F0F0F0Fu;
            const uint c4 = 0x_01010101u;

            value -= (value >> 1) & c1;
            value = (value & c2) + ((value >> 2) & c2);
            value = (((value + (value >> 4)) & c3) * c4) >> 24;

            return (int)value;
        }
    }
}
