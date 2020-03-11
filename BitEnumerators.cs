using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    public static class BitEnumerators
    {

        // Tables store values with an odd or even number of bits. Entry zero is ignored.
        private static int[][] oddNumberOfBitsTable = new int[][] { null, new int[] { 1 }, new int[] { 1, 2 }, new int[] { 1, 2, 4, 7 } };
        private static int[][] evenNumberOfBitsTable = new int[][] { new int[] { 0 }, new int[] { 0 }, new int[] { 0, 3 }, new int[] { 0, 3, 5, 6 } };
        private static int tableSize = 4;
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
            // else
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
        public static void BuildTables(int width)
        {
            int[][] newEvenTable = new int[width + 1][];
            int[][] newOddTable = new int[width + 1][];
            for (int i = 0; i < tableSize; i++)
            {
                newEvenTable[i] = evenNumberOfBitsTable[i];
                newOddTable[i] = oddNumberOfBitsTable[i];
            }
            for (int i = tableSize; i <= width; i++)
            {
                newEvenTable[i] = BitEnumerators.AllEven(i).ToArray();
                newOddTable[i] = BitEnumerators.AllOdd(i).ToArray();
            }
            oddNumberOfBitsTable = newOddTable;
            evenNumberOfBitsTable = newEvenTable;
            tableSize = width + 1;
        }
    }
}
