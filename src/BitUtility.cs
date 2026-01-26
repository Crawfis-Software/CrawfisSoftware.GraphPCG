using System;
using System.Collections.Generic;
using System.Numerics;

namespace CrawfisSoftware.Path.BitPattern
{
    /// <summary>
    /// A static utility class for bit manipulation
    /// </summary>
    public static class BitUtility
    {
        /// <summary>
        /// Check if the posth bit in inflow is 1
        /// </summary>
        /// <param name="inflow">bit to check</param>
        /// <param name="pos">position to check</param>
        /// <param name="width">the width of a bit pattern</param>
        /// <returns></returns>
        public static bool IsBitSet(int inflow, int pos, int width)
        {
            return ValidPathRowEnumerator.InflowsFromBits(width, inflow).Contains(pos);
        }

        
        /// <summary>
        /// Set the posth bit in bit to 0
        /// </summary>
        /// <param name="bit">bits to manipulate</param>
        /// <param name="pos">position to block</param>
        /// <returns>a bit with posth bit set to 0</returns>
        public static BigInteger BlockBit(BigInteger bit, int pos)
        {
            BigInteger one = new BigInteger(1);
            return bit &= ~(one << pos);
        }
        
        /// <summary>
        /// Set the posth bit in bit to 0
        /// </summary>
        /// <param name="bit">bits to manipulate</param>
        /// <param name="pos">position to block</param>
        /// <returns>a bit with posth bit set to 0</returns>
        public static int BlockBit(int bit, int pos)
        {
            return bit &= ~(1 << pos);
        }

        /// <summary>
        /// Set the posth bit in bit to 1
        /// </summary>
        /// <param name="bit">bits to manipulate</param>
        /// <param name="pos">position to open</param>
        /// <returns>a bit with posth bit set to 1</returns>
        public static BigInteger OpenBit(BigInteger bit, int pos)
        {
            BigInteger one = new BigInteger(1);
            return bit |= (one << pos);
        }
        
        /// <summary>
        /// Set the posth bit in bit to 1
        /// </summary>
        /// <param name="bit">bits to manipulate</param>
        /// <param name="pos">position to open</param>
        /// <returns>a bit with posth bit set to 1</returns>
        public static int OpenBit(int bit, int pos)
        {
            return bit |= (1 << pos);
        }
        

        /// <summary>
        /// Print the given bit pattern with given width
        /// </summary>
        /// <param name="flow">bit pattern to print</param>
        /// <param name="width">the width of the bit pattern</param>
        public static void PrintFlow(int flow, int width)
        {
            Console.WriteLine(Convert.ToString(flow, 2).PadLeft(width, '0'));
        }

        /// <summary>
        /// Set the highest set bet of a bit pattern to 0
        /// </summary>
        /// <param name="bit">the bit to change</param>
        /// <returns>changed bit pattern</returns>
        public static int BlockHighestSetBit(int bit)
        {
            int bitCopy = bit;
            int highestSetBit = 0; // assume that to begin with, x is all zeroes
            while (bitCopy != 0) {
                ++highestSetBit;
                bitCopy >>= 1;
            }
            int blockedBit = BlockBit(bit, highestSetBit - 1);
            return blockedBit;
        }

        /// <summary>
        /// Set the lowest set bet of a bit pattern to 0
        /// </summary>
        /// <param name="bit">the bit to change</param>
        /// <returns>changed bit pattern</returns>
        public static int BlockLowestSetBit(int bit)
        {
            int blockedBit = bit & (bit-1);
            return blockedBit;
        }

        /// <summary>
        /// Merge multiple bits of width into a single bit
        /// </summary>
        /// <param name="bits">bits to merge</param>
        /// <param name="width">width of the bit pattern</param>
        /// <returns>merged bits</returns>
        public static BigInteger ConcatinateMultipleBits(IList<int> bits, int width)
        {
            BigInteger mergedBit = bits[0];
            for (int i = 1; i < bits.Count; i++)
            {
                mergedBit = (mergedBit << width) | bits[i];
            }
            return mergedBit;
        }
    }
}