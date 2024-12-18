using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            }
            else
            {
                ValidPathRowEnumerator.BuildEvenTablesWithConstraints(_tableWidth, globalConstraintsOracle);
            }
            
        }

        /// <summary>
        /// Iterate over all non-cyclical paths from a starting cell to an ending cell on an open verticalGrid.
        /// </summary>
        /// <param name="start">The column index of the starting cell on the first row (row 0).</param>
        /// <param name="end">The column index of the ending cell on the last row (row height-1)</param>

        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public (IList<BigInteger> vertical, IList<BigInteger> horizontal)
            Sample(int numColumns)
        {
            const int MAX_ATTEMPTS = 1000000;
            int currentAttempt = 0;

            IList<int[]> verticals = new List<int[]>();
            for (int i = 0; i < _height + 1; i++)
            {
                verticals.Add(new int[numColumns]);
            }
            BigInteger[] verticalPaths = new BigInteger[_height + 1];
            BigInteger[] horizontalPaths = new BigInteger[_height];
            

            while (currentAttempt < MAX_ATTEMPTS)
            {
                try
                {
                    ValidPathRowEnumerator.PointToEvenTable();
            
                    #region FirstRow
            
                    BigInteger inflow = verticalPaths[0] = 0;
                    IList<IList<int>> components = InitializeComponents(0, numColumns);
                    IList<short> rowLists =
                        ValidPathRowEnumerator.ValidRowList(_tableWidth, RandomEvenBitPattern(_tableWidth, _random))
                            .ToList();
                    int[] outflowCandidatesOriginal = new int[numColumns];
                    int[] outflowCandidatesModified = new int[numColumns];
                    
                    bool previousBlocked = false;
                    for (int i = 0; i < numColumns; i++)
                    {
                        outflowCandidatesOriginal[i] = rowLists[_random.Next(rowLists.Count - 1)];
                        outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                        if (previousBlocked)
                        {
                            outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                        }

                        if (_random.Next(2) == 0 && i != numColumns - 1)
                        {
                            outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                            previousBlocked = true;
                        }
                        else
                        {
                            previousBlocked = false;
                        }
                    }

                    BigInteger outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                    BigInteger horizontalSpans;
                    while (!ValidateAndUpdateComponentsCarryOverBit(inflow, outflowCandidate, components, 0,
                               out horizontalSpans))
                    {
                        previousBlocked = false;
                        for (int i = 0; i < numColumns; i++)
                        {
                            outflowCandidatesOriginal[i] = rowLists[_random.Next(rowLists.Count - 1)];
                            outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                            if (previousBlocked)
                            {
                                outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                            }

                            if (_random.Next(2) == 0 && i != numColumns - 1)
                            {
                                outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                                previousBlocked = true;
                            }
                            else
                            {
                                previousBlocked = false;
                            }
                        }
                        outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                    }

                    verticalPaths[1] = outflowCandidate;
                    for (int i = 0; i < numColumns; i++)
                    {
                        verticals[1][i] = outflowCandidatesOriginal[i];
                    }
                    horizontalPaths[0] = horizontalSpans;
            
                    #endregion
            
                    #region MiddleRows
                    
                    for (int currentRow = 1; currentRow < _height - 2; currentRow++)
                    {
                        inflow = verticalPaths[currentRow];
                        
                        int[] middleInflows = new int[numColumns];
                        for (int i = 0; i < numColumns; i++)
                        {
                            middleInflows[i] = verticals[currentRow][i];
                        }
                    
                        outflowCandidatesOriginal = new int[numColumns];
                        outflowCandidatesModified = new int[numColumns];
                        
                        previousBlocked = false;
                        for (int i = 0; i < numColumns; i++)
                        {
                            outflowCandidatesOriginal[i] = ValidPathRowEnumerator.ValidRowList
                                (_tableWidth, middleInflows[i]).OrderBy(x => _random.Next()).First();
                            outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                            if (previousBlocked)
                            {
                                outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                            }
                    
                            if (_random.Next(2) == 0 && i != numColumns - 1)
                            {
                                outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                                previousBlocked = true;
                            }
                            else
                            {
                                previousBlocked = false;
                            }
                        }
                        outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        while (!(ValidateAndUpdateComponentsCarryOverBit(inflow, outflowCandidate, components, currentRow,
                                   out horizontalSpans, 1)))
                        {
                            previousBlocked = false;
                            for (int i = 0; i < numColumns; i++)
                            {
                                outflowCandidatesOriginal[i] = rowLists[_random.Next(rowLists.Count - 1)];
                                outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                                if (previousBlocked)
                                {
                                    outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                                }
                    
                                if (_random.Next(2) == 0 && i != numColumns - 1)
                                {
                                    outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                                    previousBlocked = true;
                                }
                                else
                                {
                                    previousBlocked = false;
                                }
                            }
                            outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        }
                        
                        verticalPaths[currentRow + 1] = outflowCandidate;
                        for (int i = 0; i < numColumns; i++)
                        {
                            verticals[currentRow+1][i] = outflowCandidatesOriginal[i];
                        }
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
                        int[] middleInflows = new int[numColumns];
                        for (int i = 0; i < numColumns; i++)
                        {
                            middleInflows[i] = verticals[secondToLastRow][i];
                        }
                    
                        outflowCandidatesOriginal = new int[numColumns];
                        outflowCandidatesModified = new int[numColumns];
                        
                        previousBlocked = false;
                        for (int i = 0; i < numColumns; i++)
                        {
                            outflowCandidatesOriginal[i] = ValidPathRowEnumerator.ValidRowList
                                (_tableWidth, middleInflows[i]).OrderBy(x => _random.Next()).First();
                            outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                            if (previousBlocked)
                            {
                                outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                            }
                    
                            if (_random.Next(2) == 0 && i != numColumns - 1)
                            {
                                outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                                previousBlocked = true;
                            }
                            else
                            {
                                previousBlocked = false;
                            }
                        }
                        outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        
                        while (!ValidateAndUpdateComponentsCarryOverBit(inflow, outflowCandidate, components, secondToLastRow,
                                   out horizontalSpans))
                        {
                            previousBlocked = false;
                            for (int i = 0; i < numColumns; i++)
                            {
                                outflowCandidatesOriginal[i] = rowLists[_random.Next(rowLists.Count - 1)];
                                outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                                if (previousBlocked)
                                {
                                    outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                                }
                    
                                if (_random.Next(2) == 0 && i != numColumns - 1)
                                {
                                    outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                                    previousBlocked = true;
                                }
                                else
                                {
                                    previousBlocked = false;
                                }
                            }
                            outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        }
                    
                        verticalPaths[secondToLastRow + 1] = outflowCandidate;
                        for (int i = 0; i < numColumns; i++)
                        {
                            verticals[secondToLastRow+1][i] = outflowCandidatesOriginal[i];
                        }
                        horizontalPaths[secondToLastRow] = horizontalSpans;
                    
                        int lastRow = _height - 1;
                        inflow = verticalPaths[lastRow];
                        if (UpdateLastRowAndValidateComponent(ref horizontalPaths, inflow,
                                verticalPaths[secondToLastRow], lastRow, components, numColumns))
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

        private IList<IList<int>> InitializeComponents(int firstRow, int numColumns)
        {
            int[][] components = new int[_height][];
            for (int i = 0; i < _height; i++)
                components[i] = new int[_tableWidth*numColumns];
            IList<int> inflowList = ValidPathRowEnumerator.InflowsFromBits(_tableWidth*numColumns, firstRow);
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
        
        private bool UpdateLastRowAndValidateComponent(ref BigInteger[] horizontalPaths, BigInteger inflow, 
            BigInteger previousInflow, int rowNumber, IList<IList<int>> components, int numColumns)
        {
            BigInteger bigOne = new BigInteger(1);
            List<int> previousInflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth*numColumns, previousInflow);
            IList<int> currentInflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth*numColumns, inflow);
            
            int previousMin = previousInflows.Min();
            int previousMax = previousInflows.Max();
            
            int currentMin = currentInflows.Min();
            int currentMax = currentInflows.Max();

            if (previousMin < currentMin || previousMax > currentMax)
            {
                return false;
            }
            
            
            IList<int> componentList = components[rowNumber];
            BigInteger horizontal = new BigInteger(0);
            for (int i = 1; i < currentInflows.Count; i += 2)
            {
                int start = currentInflows[i-1];
                int end = currentInflows[i];
                for (int j = start; j < end; j++)
                {
                    horizontal |= bigOne << j;
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

        private BigInteger ConcatinateMultipleBits(IList<int> bits, int width)
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
