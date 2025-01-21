using System;
using System.Collections.Generic;
using System.Linq;
using CrawfisSoftware.Path.BitPattern;
using static CrawfisSoftware.Path.BitPattern.EnumerationUtilities;

namespace CrawfisSoftware.Path
{
    /// <summary>
    /// Class to sample a path from top to bottom.
    /// </summary>
    public class LoopSampler
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
        public LoopSampler(int width, int height, Random random,
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
        public (IList<int> vertical, IList<int> horizontal)
            Sample()
        {
            const int MAX_ATTEMPTS = 1000000;
            int currentAttempt = 0;
            int[] verticalPaths = new int[_height + 1];
            int[] horizontalPaths = new int[_height];
            

            while (currentAttempt < MAX_ATTEMPTS)
            {
                try
                {
                    ValidPathRowEnumerator.PointToEvenTable();
            
                    #region FirstRow
            
                    int inflow = verticalPaths[0] = 0;
                    IList<IList<int>> components = InitializeComponents(verticalPaths[0], 1);
                    IList<short> rowLists =
                        ValidPathRowEnumerator.ValidRowList(_tableWidth, RandomEvenBitPattern(_tableWidth, _random))
                            .ToList();
                    int outflowCandidate = rowLists[_random.Next(0, rowLists.Count)];
                    int horizontalSpans;
                    while (!ValidateAndUpdateComponents(inflow, outflowCandidate, components, 0,
                               out horizontalSpans))
                    {
                        outflowCandidate = rowLists[_random.Next(0, rowLists.Count)];
                    }

                    verticalPaths[1] = outflowCandidate;
                    horizontalPaths[0] = horizontalSpans;
            
                    #endregion
            
                    #region MiddleRows
                    
                    for (int currentRow = 1; currentRow < _height - 2; currentRow++)
                    {
                        inflow = verticalPaths[currentRow];
                        rowLists =
                            ValidPathRowEnumerator.ValidRowList(_tableWidth, inflow)
                                .ToList();
                        outflowCandidate = rowLists[_random.Next(0, rowLists.Count)];
                        while (!(ValidateAndUpdateComponents(inflow, outflowCandidate, components, currentRow,
                                   out horizontalSpans, 1)))
                        {
                            outflowCandidate = rowLists[_random.Next(0, rowLists.Count)];
                        }
                        
                        verticalPaths[currentRow + 1] = outflowCandidate;
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
                        rowLists =
                            ValidPathRowEnumerator.ValidRowList(_tableWidth, inflow)
                                .ToList();
                        outflowCandidate = rowLists[_random.Next(0, rowLists.Count)];
                        
                        while (!ValidateAndUpdateComponents(inflow, outflowCandidate, components, secondToLastRow,
                                   out horizontalSpans))
                        {
                            outflowCandidate = rowLists[_random.Next(0, rowLists.Count)];
                        }
                    
                        verticalPaths[secondToLastRow + 1] = outflowCandidate;
                        horizontalPaths[secondToLastRow] = horizontalSpans;
                    
                        int lastRow = _height - 1;
                        inflow = verticalPaths[lastRow];
                        if (UpdateLastRowAndValidateComponent(ref horizontalPaths, inflow,
                                verticalPaths[secondToLastRow], lastRow, components, 1))
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
        
        private bool UpdateLastRowAndValidateComponent(ref int[] horizontalPaths, int inflow, 
            int previousInflow, int rowNumber, IList<IList<int>> components, int numColumns)
        {
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
            int horizontal = 0;
            for (int i = 1; i < currentInflows.Count; i += 2)
            {
                int start = currentInflows[i-1];
                int end = currentInflows[i];
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

        private void PrintFlow(int flow, int width)
        {
            Console.WriteLine(Convert.ToString(flow, 2).PadLeft(width, '0'));
        }
    }
}
