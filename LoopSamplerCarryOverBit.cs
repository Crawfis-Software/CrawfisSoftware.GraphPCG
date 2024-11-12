using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static CrawfisSoftware.PCG.EnumerationUtilities;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Class to sample a path from top to bottom.
    /// </summary>
    public class LoopSamplerCarryOverBit
    {
        private const int MaxDefaultAttempts = 10000;
        private readonly int _tableWidth;
        private readonly int _width;
        private readonly int _height;
        private readonly Random _random;
        private readonly Validator _verticalCandidateOracle;
        private readonly Validator _horizontalCandidateOracle;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="width">The width of the underlying verticalGrid.</param>
        /// <param name="height">The height of the underlying verticalGrid</param>
        /// <param name="random">Random number generator</param>
        /// <param name="globalConstraintsOracle">Optional function to specify some global constraints on the outflows of a row.</param>
        /// <param name="verticalCandidateOracle">Function that returns true or false whether this row is desired. Parameters are: the pathID, the row number,
        /// the current candidate row value (vertical bits), all verticalBits so far, all horizontal bits so far, all components so far</param>
        /// <param name="horizontalCandidateOracle">Function that returns true or false whether this row is desired. Parameters are: the pathID, the row number,
        /// the current candidate value (horizontal bits), all verticalBits so far, all horizontal bits so far, all components so far.</param>
        public LoopSamplerCarryOverBit(int width, int height, Random random,
            Func<int, bool> globalConstraintsOracle = null, Validator verticalCandidateOracle = null,
            Validator horizontalCandidateOracle = null)
        {
            this._width = width;
            this._tableWidth = width;
            this._height = height;
            this._random = random;
            this._verticalCandidateOracle = verticalCandidateOracle;
            this._horizontalCandidateOracle = horizontalCandidateOracle;

            if (globalConstraintsOracle == null)
            {
                ValidPathRowEnumerator.BuildEvenTables(_tableWidth);
                //ValidPathRowEnumerator.BuildOddTables(_tableWidth);
            }
            else
            {
                ValidPathRowEnumerator.BuildEvenTablesWithConstraints(_tableWidth, globalConstraintsOracle);
                //ValidPathRowEnumerator.BuildOddTablesWithConstraints(_tableWidth, globalConstraintsOracle);
            }
            
        }

        /// <summary>
        /// Iterate over all non-cyclical paths from a starting cell to an ending cell on an open verticalGrid.
        /// </summary>
        /// <param name="start">The column index of the starting cell on the first row (row 0).</param>
        /// <param name="end">The column index of the ending cell on the last row (row height-1)</param>

        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public (IList<int> vertical, IList<int> horizontal)
            Sample()
        {
            const int MAX_ATTEMPTS = 1000000;
            int currentAttempt = 0;
            int[] verticalLeft = new int[_height + 1];
            int[] verticalRight = new int[_height + 1];
            int[] verticalPaths = new int[_height + 1];
            int[] horizontalPaths = new int[_height];
            

            while (currentAttempt < MAX_ATTEMPTS)
            {
                try
                {
                    ValidPathRowEnumerator.PointToEvenTable();
            
                    #region FirstRow
            
                    int inflow = verticalPaths[0] = 0;
                    IList<IList<int>> components = InitializeComponents(verticalPaths[0]);
                    IList<short> rowLists =
                        ValidPathRowEnumerator.ValidRowList(_tableWidth, RandomEvenBitPattern(_tableWidth, _random))
                            .ToList();
                    int outflowCandidateLeft = rowLists[_random.Next(rowLists.Count - 1)];
                    int outflowCandidateRight = rowLists[_random.Next(rowLists.Count - 1)];
                    int outflowCandidateLeftMod = BlockLowestSetBit(outflowCandidateLeft);
                    int outflowCandidateRightMod = BlockHighestSetBit(outflowCandidateRight);
                    if (_random.Next(2) == 0)
                    {
                        outflowCandidateLeftMod = outflowCandidateLeft;
                        outflowCandidateRightMod = outflowCandidateRight;
                    }

                    int outflowCandidate = ConcatinateTwoBits(outflowCandidateLeftMod, outflowCandidateRightMod, _width);
                    int horizontalSpans;
                    while (!ValidateAndUpdateComponents(inflow, outflowCandidate, components, 0,
                               out horizontalSpans))
                    {
                        outflowCandidateLeft = rowLists[_random.Next(rowLists.Count - 1)];
                        outflowCandidateRight = rowLists[_random.Next(rowLists.Count - 1)];
                        outflowCandidateLeftMod = BlockLowestSetBit(outflowCandidateLeft);
                        outflowCandidateRightMod = BlockHighestSetBit(outflowCandidateRight);
                        if (_random.Next(2) == 0)
                        {
                            outflowCandidateLeftMod = outflowCandidateLeft;
                            outflowCandidateRightMod = outflowCandidateRight;
                        }
                        outflowCandidate = ConcatinateTwoBits(outflowCandidateLeftMod, outflowCandidateRightMod, _width);
                    }

                    verticalPaths[1] = outflowCandidate;
                    verticalLeft[1] = outflowCandidateLeft;
                    verticalRight[1] = outflowCandidateRight;
                    horizontalPaths[0] = horizontalSpans;
            
                    #endregion
            
                    #region MiddleRows
            
                    for (int currentRow = 1; currentRow < _height - 2; currentRow++)
                    {
                        inflow = verticalPaths[currentRow];
                        int inflowLeft = verticalLeft[currentRow];
                        int inflowRight = verticalRight[currentRow];
            
                        var rowListsLeft = ValidPathRowEnumerator.ValidRowList(_tableWidth, inflowLeft).ToList();
                        var rowListsRight = ValidPathRowEnumerator.ValidRowList(_tableWidth, inflowRight).ToList();
                        outflowCandidateLeft = rowListsLeft[_random.Next(rowListsLeft.Count - 1)];
                        outflowCandidateRight = rowListsRight[_random.Next(rowListsRight.Count - 1)];
                        outflowCandidateLeftMod = BlockLowestSetBit(outflowCandidateLeft);
                        outflowCandidateRightMod = BlockHighestSetBit(outflowCandidateRight);
                        if (_random.Next(2) == 0)
                        {
                            outflowCandidateLeftMod = outflowCandidateLeft;
                            outflowCandidateRightMod = outflowCandidateRight;
                        }
                        outflowCandidate = ConcatinateTwoBits(outflowCandidateLeftMod, outflowCandidateRightMod, _width);
                        while (!(ValidateAndUpdateComponents(inflow, outflowCandidate, components, currentRow,
                                   out horizontalSpans, 1)))
                        {
                            outflowCandidateLeft = rowListsLeft[_random.Next(rowListsLeft.Count - 1)];
                            outflowCandidateRight = rowListsRight[_random.Next(rowListsRight.Count - 1)];
                            outflowCandidateLeftMod = BlockLowestSetBit(outflowCandidateLeft);
                            outflowCandidateRightMod = BlockHighestSetBit(outflowCandidateRight);
                            if (_random.Next(2) == 0)
                            {
                                outflowCandidateLeftMod = outflowCandidateLeft;
                                outflowCandidateRightMod = outflowCandidateRight;
                            }
                            outflowCandidate = ConcatinateTwoBits(outflowCandidateLeftMod, outflowCandidateRightMod, _width);
                        }
            
                        verticalPaths[currentRow + 1] = outflowCandidate;
                        verticalLeft[currentRow + 1] = outflowCandidateLeft;
                        verticalRight[currentRow + 1] = outflowCandidateRight;
                        horizontalPaths[currentRow] = horizontalSpans;
            
                    }
            
                    #endregion
            
                    #region LastTwoRows
            
                    bool lastRowFixed = false;
                    int lastRowAttemp = 0;
                    while (!lastRowFixed)
                    {
                        int secondToLastRow = _height - 2;
                        inflow = verticalPaths[secondToLastRow];
                        int inflowLeft = verticalLeft[secondToLastRow];
                        int inflowRight = verticalRight[secondToLastRow];
                        var rowListsLeft = ValidPathRowEnumerator.ValidRowList(_tableWidth, inflowLeft).ToList();
                        var rowListsRight = ValidPathRowEnumerator.ValidRowList(_tableWidth, inflowRight).ToList();
                        outflowCandidateLeft = rowListsLeft[_random.Next(rowListsLeft.Count - 1)];
                        outflowCandidateRight = rowListsRight[_random.Next(rowListsRight.Count - 1)];
                        outflowCandidateLeftMod = BlockLowestSetBit(outflowCandidateLeft);
                        outflowCandidateRightMod = BlockHighestSetBit(outflowCandidateRight);
                        if (_random.Next(2) == 0)
                        {
                            outflowCandidateLeftMod = outflowCandidateLeft;
                            outflowCandidateRightMod = outflowCandidateRight;
                        }
                        outflowCandidate = ConcatinateTwoBits(outflowCandidateLeftMod, outflowCandidateRightMod, _width);
            
                        while (!ValidateAndUpdateComponents(inflow, outflowCandidate, components, secondToLastRow,
                                   out horizontalSpans))
                        {
                            outflowCandidateLeft = rowListsLeft[_random.Next(rowListsLeft.Count - 1)];
                            outflowCandidateRight = rowListsRight[_random.Next(rowListsRight.Count - 1)];
                            outflowCandidateLeftMod = BlockLowestSetBit(outflowCandidateLeft);
                            outflowCandidateRightMod = BlockHighestSetBit(outflowCandidateRight);
                            if (_random.Next(2) == 0)
                            {
                                outflowCandidateLeftMod = outflowCandidateLeft;
                                outflowCandidateRightMod = outflowCandidateRight;
                            }
                            outflowCandidate = ConcatinateTwoBits(outflowCandidateLeftMod, outflowCandidateRightMod, _width);
                        }
            
                        verticalPaths[secondToLastRow + 1] = outflowCandidate;
                        verticalLeft[secondToLastRow + 1] = outflowCandidateLeft;
                        verticalRight[secondToLastRow + 1] = outflowCandidateRight;
                        horizontalPaths[secondToLastRow] = horizontalSpans;
            
                        int lastRow = _height - 1;
                        inflow = verticalPaths[lastRow];
                        if (UpdateLastRowAndValidateComponent(ref horizontalPaths, inflow,
                                verticalPaths[secondToLastRow], lastRow, components))
                        {
                            lastRowFixed = true;
                        }
            
                        lastRowAttemp++;
                        if (lastRowAttemp > MaxDefaultAttempts)
                        {
                            throw new TimeoutException("Cannot find a valid last row.");
                        }
            
                    }
            
                    #endregion
            
                    return (verticalPaths, horizontalPaths);
                }
                catch (TimeoutException e)
                {
                    currentAttempt++;
                    if (currentAttempt > MaxDefaultAttempts)
                    {
                        throw new TimeoutException("Search Too Long.");
                    }
                }
            }
            return  (verticalPaths, horizontalPaths);
            

        }
        

        private bool CheckOneRowBeforeStartAndEnd(int outflow, int currentRow, int startRow, int endRow)
        {
            if (currentRow == startRow - 1 || currentRow == endRow - 1)
            {
                if (CountSetBits(outflow) == 1)
                {
                    return false;
                }
            }
            return true;
        }
        private void DetermineTable(int inflow)
        {
            if (EnumerationUtilities.CountSetBits(inflow) % 2 == 0)
            {
                ValidPathRowEnumerator.PointToEvenTable();
            }
            else
            {
                ValidPathRowEnumerator.PointToOddTable();
            }
        }

        private IList<IList<int>> InitializeComponents(int firstRow)
        {
            int[][] components = new int[_height][];
            for (int i = 0; i < _height; i++)
                components[i] = new int[_tableWidth*2];
            IList<int> inflowList = ValidPathRowEnumerator.InflowsFromBits(_tableWidth*2, firstRow);
            int counter = 0;
            int currentComponentNumber = 1;
            for (int i = 0; i < components[0].Count(); i++)
            {
                if (inflowList.Contains(i))
                {
                    components[0][i] = currentComponentNumber;
                    counter++;
                    if (counter % 2 == 0)
                    {
                        currentComponentNumber++;
                    }
                }
            }

            return components;
        }
        
        
        private int[] GetEdges(int inflow, int outflow,int horizontalSpan, int cellNumber)
        {
            int[] edges = new int[4];
            List<int> inflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth, inflow);
            List<int> outflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth, outflow);
            List<int> horizontalSpans = ValidPathRowEnumerator.InflowsFromBits(_tableWidth, horizontalSpan);

            if (cellNumber == 0)
            {
                edges[0] = 0;
            }
            else
            {
                edges[0] = horizontalSpans.Contains(cellNumber - 1) ? 1 : 0; 
            }
            edges[1] = outflows.Contains(cellNumber) ? 1 : 0;
            edges[2] = horizontalSpans.Contains(cellNumber) ? 1 : 0; 
            edges[3] = inflows.Contains(cellNumber) ? 1 : 0;
            
            return edges;
        }
        
        private bool UpdateLastRowAndValidateComponent(ref int[] horizontalPaths, int inflow, 
            int previousInflow, int rowNumber, IList<IList<int>> components)
        {
            List<int> previousInflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth*2, previousInflow);
            
            int min = previousInflows.Min();
            int max = previousInflows.Max();
            
            IList<int> inflowList = ValidPathRowEnumerator.InflowsFromBits(_tableWidth*2, inflow);
            IList<int> componentList = components[rowNumber];
            int horizontal = 0;
            for (int i = 1; i < inflowList.Count; i += 2)
            {
                int start = inflowList[i-1];
                int end = inflowList[i];
                for (int j = start; j < end; j++)
                {
                    horizontal |= 1 << j;
                }

                componentList[end] = componentList[start];
            }
            if (NumberOfDistinctValues(componentList) != 2)
            {
                return false;
            }
            horizontalPaths[rowNumber] = horizontal;
            return true;
        }

        private bool IsBitSet(int inflow, int pos)
        {
            return ValidPathRowEnumerator.InflowsFromBits(_tableWidth, inflow).Contains(pos);
        }

        private int BlockBit(int bit, int pos)
        {
            return bit &= ~(1 << pos);
        }

        private int OpenBit(int bit, int pos)
        {
            return bit |= (1 << pos);
        }

        private int TotalNumberOfSetBitsBeforeAPos(int inflow, int outflow, int pos)
        {
            List<int> inflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth, inflow);
            List<int> outflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth, outflow);
            return inflows.Count(n => n < pos) + outflows.Count(n => n < pos);
        }

        private void PrintFlow(int flow, int width)
        {
            Console.WriteLine(Convert.ToString(flow, 2).PadLeft(width, '0'));
        }

        private int BlockHighestSetBit(int bit)
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

        private int BlockLowestSetBit(int bit)
        {
            int blockedBit = bit & (bit-1);
            return blockedBit;
        }

        private int ConcatinateTwoBits(int bit1, int bit2, int width)
        {
            
            int mergedBit = (bit1 << width) | bit2;
            return mergedBit;
        }
    }
}
