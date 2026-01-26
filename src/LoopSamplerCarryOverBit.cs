using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CrawfisSoftware.Path.BitPattern;
using static CrawfisSoftware.Path.BitPattern.EnumerationUtilities;

namespace CrawfisSoftware.Path
{
    /// <summary>
    /// Class to sample a path from top to bottom.
    /// </summary>
    public class LoopSamplerCarryOverBit
    {
        private const int MaxDefaultAttempts = 1000;
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
        /// <param name="numColumns">Numeber of columns, each column has a width of the given table</param>
        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public (IList<BigInteger> vertical, IList<BigInteger> horizontal)
            Sample(int numColumns)
        {
            ISet<BigInteger> seenCandidates = new HashSet<BigInteger>();
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
                            outflowCandidatesModified[i] = BitUtility.BlockHighestSetBit(outflowCandidatesModified[i]);
                        }

                        if (_random.Next(2) == 0 && i != numColumns - 1)
                        {
                            outflowCandidatesModified[i] = BitUtility.BlockLowestSetBit(outflowCandidatesModified[i]);
                            previousBlocked = true;
                        }
                        else
                        {
                            previousBlocked = false;
                        }
                    }

                    BigInteger outflowCandidate = BitUtility.ConcatinateMultipleBits(outflowCandidatesModified, _width);
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
                                outflowCandidatesModified[i] = BitUtility.BlockHighestSetBit(outflowCandidatesModified[i]);
                                //outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                            }

                            if (_random.Next(2) == 0 && i != numColumns - 1)
                            {
                                outflowCandidatesModified[i] = BitUtility.BlockLowestSetBit(outflowCandidatesModified[i]);
                                //outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                                previousBlocked = true;
                            }
                            else
                            {
                                previousBlocked = false;
                            }
                        }
                        outflowCandidate = BitUtility.ConcatinateMultipleBits(outflowCandidatesModified, _width);
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
                                outflowCandidatesModified[i] = BitUtility.BlockHighestSetBit(outflowCandidatesModified[i]);
                                //outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                            }

                            if (_random.Next(2) == 0 && i != numColumns - 1)
                            {
                                outflowCandidatesModified[i] = BitUtility.BlockLowestSetBit(outflowCandidatesModified[i]);
                                //outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                                previousBlocked = true;
                            }
                            else
                            {
                                previousBlocked = false;
                            }
                        }
                        outflowCandidate = BitUtility.ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        seenCandidates.Clear();
                        int midfailed = 0;
                        while (!(ValidateAndUpdateComponentsCarryOverBit(inflow, outflowCandidate, components, currentRow,
                                   out horizontalSpans, 1)) && !seenCandidates.Contains(outflowCandidate))
                        {
                            midfailed++;
                            previousBlocked = false;
                            for (int i = 0; i < numColumns; i++)
                            {
                                outflowCandidatesOriginal[i] = rowLists[_random.Next(rowLists.Count - 1)];
                                outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                                if (previousBlocked)
                                {
                                    outflowCandidatesModified[i] = BitUtility.BlockHighestSetBit(outflowCandidatesModified[i]);
                                    //outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                                }

                                if (_random.Next(2) == 0 && i != numColumns - 1)
                                {
                                    outflowCandidatesModified[i] = BitUtility.BlockLowestSetBit(outflowCandidatesModified[i]);
                                    //outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                                    previousBlocked = true;
                                }
                                else
                                {
                                    previousBlocked = false;
                                }
                            }
                            outflowCandidate = BitUtility.ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        }
                        //Console.WriteLine($"Row {currentRow} failed: {midfailed} times.");
                        
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
                        IList<int> currentInflows = ValidPathRowEnumerator.InflowsFromBits(_tableWidth*numColumns, inflow);
                        int currentMin = currentInflows.Min();
                        int currentMax = currentInflows.Max();
                        
                        outflowCandidate = BigInteger.Zero;
                        outflowCandidate = BitUtility.OpenBit(outflowCandidate, currentMin);
                        outflowCandidate = BitUtility.OpenBit(outflowCandidate, currentMax);
                        // int[] middleInflows = new int[numColumns];
                        // for (int i = 0; i < numColumns; i++)
                        // {
                        //     middleInflows[i] = verticals[secondToLastRow][i];
                        // }
                        //
                        // outflowCandidatesOriginal = new int[numColumns];
                        // outflowCandidatesModified = new int[numColumns];
                        //
                        // previousBlocked = false;
                        // for (int i = 0; i < numColumns; i++)
                        // {
                        //     outflowCandidatesOriginal[i] = ValidPathRowEnumerator.ValidRowList
                        //         (_tableWidth, middleInflows[i]).OrderBy(x => _random.Next()).First();
                        //     outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                        //     if (previousBlocked)
                        //     {
                        //         outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                        //     }
                        //
                        //     if (_random.Next(2) == 0 && i != numColumns - 1)
                        //     {
                        //         outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                        //         previousBlocked = true;
                        //     }
                        //     else
                        //     {
                        //         previousBlocked = false;
                        //     }
                        // }
                        // outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        seenCandidates.Clear();
                        
                        while (!ValidateAndUpdateComponentsCarryOverBit(inflow, outflowCandidate, components, secondToLastRow,
                                   out horizontalSpans) && !seenCandidates.Contains(outflowCandidate))
                        {
                            seenCandidates.Add(outflowCandidate);
                            outflowCandidate = BigInteger.Zero;
                            outflowCandidate = BitUtility.OpenBit(outflowCandidate, currentMin);
                            outflowCandidate = BitUtility.OpenBit(outflowCandidate, currentMax);
                            // previousBlocked = false;
                            // for (int i = 0; i < numColumns; i++)
                            // {
                            //     outflowCandidatesOriginal[i] = rowLists[_random.Next(rowLists.Count - 1)];
                            //     outflowCandidatesModified[i] = outflowCandidatesOriginal[i];
                            //     if (previousBlocked)
                            //     {
                            //         outflowCandidatesModified[i] = BlockHighestSetBit(outflowCandidatesModified[i]);
                            //     }
                            //
                            //     if (_random.Next(2) == 0 && i != numColumns - 1)
                            //     {
                            //         outflowCandidatesModified[i] = BlockLowestSetBit(outflowCandidatesModified[i]);
                            //         previousBlocked = true;
                            //     }
                            //     else
                            //     {
                            //         previousBlocked = false;
                            //     }
                            // }
                            // outflowCandidate = ConcatinateMultipleBits(outflowCandidatesModified, _width);
                        }
                    
                        verticalPaths[secondToLastRow + 1] = outflowCandidate;
                        for (int i = 0; i < numColumns; i++)
                        {
                            verticals[secondToLastRow+1][i] = outflowCandidatesOriginal[i];
                        }
                        horizontalPaths[secondToLastRow] = horizontalSpans;
                    
                        int lastRow = _height - 1;
                        inflow = verticalPaths[lastRow];
                        if (UpdateLastRowAndValidateComponent(ref horizontalPaths, ref inflow,
                                verticalPaths[secondToLastRow], lastRow, components, numColumns))
                        {
                            lastRowFixed = true;
                            verticalPaths[lastRow] = inflow;
                        }
                    
                        lastRowAttemp++;
                        if (lastRowAttemp > MaxDefaultAttempts)
                        {
                            throw new TimeoutException("Cannot find a valid last row.");
                        }
                    
                    }
                    //Console.WriteLine($"Last two row attempts: {lastRowAttemp} times.");
                    
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
        
        /// <summary>
        /// Return the width of the LoopSampler
        /// </summary>
        /// <returns>The width of the LoopSampler</returns>
        public int GetWidth()
        {
            return _tableWidth;
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
        
        private bool UpdateLastRowAndValidateComponent(ref BigInteger[] horizontalPaths, ref BigInteger inflow, 
            BigInteger previousInflow, int rowNumber, IList<IList<int>> components, int numColumns)
        {
            IList<int> componentList = components[rowNumber];
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
    }
}
