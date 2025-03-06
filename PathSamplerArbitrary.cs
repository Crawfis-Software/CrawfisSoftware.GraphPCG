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
    public class PathSamplerArbitrary
    {
        private const int MaxDefaultAttempts = 1000;
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
        public PathSamplerArbitrary(int width, int height, Random random,
            Func<int, bool> globalConstraintsOracle = null, Validator verticalCandidateOracle = null,
            Validator horizontalCandidateOracle = null)
        {
            this._width = width;
            this._height = height;
            this._random = random;
            this._verticalCandidateOracle = verticalCandidateOracle;
            this._horizontalCandidateOracle = horizontalCandidateOracle;

            if (globalConstraintsOracle == null)
            {
                ValidPathRowEnumerator.BuildOddTables(width);
                ValidPathRowEnumerator.BuildEvenTables(width);
            }
            else
            {
                ValidPathRowEnumerator.BuildOddTablesWithConstraints(width, globalConstraintsOracle);
                ValidPathRowEnumerator.BuildEvenTablesWithConstraints(width, globalConstraintsOracle);
            }
        }
        
        

        /// <summary>
        /// Iterate over all non-cyclical paths from a starting cell to an ending cell on an open verticalGrid.
        /// </summary>
        /// <param name="start">The column index of the starting cell on the first row (row 0).</param>
        /// <param name="end">The column index of the ending cell on the last row (row height-1)</param>

        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public (IList<int> vertical, IList<int> horizontal)
            Sample((int row, int column) start, (int row, int column) end)
        {
            int counter = 0;
            for (int i = 0; i < MaxDefaultAttempts; i++)
            {
                try
                {
                    int[] verticalPaths = new int[_height + 1];
                    int[] horizontalPaths = new int[_height];
                    ValidPathRowEnumerator.PointToEvenTable();

                    int higherRow = start.row > end.row ? start.row : end.row;

                    #region FirstRow

                    int inflow = verticalPaths[0] = 0;
                    IList<IList<int>> components = InitializeComponents(verticalPaths[0]);
                    IList<short> rowLists =
                        ValidPathRowEnumerator.ValidRowList(_width, RandomEvenBitPattern(_width, _random)).ToList();
                    int outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                    int horizontalSpans;
                    counter = 0;
                    while (!ValidateAndUpdateComponents(inflow, outflowCandidate, components, 0,
                               out horizontalSpans, 1) ||
                           !CheckOneRowBeforeStartAndEnd(outflowCandidate, 0, start.row, end.row))
                    {
                        outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                        counter++;
                        if (counter >= MaxDefaultAttempts)
                        {
                            throw new Exception("Too many attempts");
                        }
                    }
                    verticalPaths[1] = outflowCandidate;
                    horizontalPaths[0] = horizontalSpans;

                    #endregion

                    #region MiddleRows
                    
                    for (int currentRow = 1; currentRow < _height - 2; currentRow++)
                    { 
                        inflow = verticalPaths[currentRow];

                        #region StartRowCase

                        int alteredInflow = inflow;
                        if (currentRow == start.row)
                        {
                            // Exits from bottom
                            if (IsBitSet(inflow, start.column))
                            {
                                alteredInflow = BlockBit(alteredInflow, start.column);
                                if (alteredInflow == 0)
                                {
                                    throw new Exception("Inflow cannot be zero");
                                }
                                DetermineTable(alteredInflow);
                                rowLists = ValidPathRowEnumerator.ValidRowList(_width, alteredInflow).ToList();
                                outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                counter = 0;
                                while (!ValidateAndUpdateComponents(alteredInflow, outflowCandidate, components,
                                           currentRow,
                                           out horizontalSpans, 1) ||
                                       IsBitSet(outflowCandidate, start.column) ||
                                       TotalNumberOfSetBitsBeforeAPos(inflow, outflowCandidate, start.column) % 2 != 0)
                                {
                                    outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                    counter++;
                                    if (counter >= MaxDefaultAttempts)
                                    {
                                        throw new Exception("Too many attempts");
                                    }
                                }
                                verticalPaths[currentRow + 1] = outflowCandidate;
                                horizontalPaths[currentRow] = horizontalSpans;
                            }
                            // Not Exit from bottom
                            else
                            {
                                alteredInflow = OpenBit(alteredInflow, start.column);
                                components[currentRow][start.column] = -1;
                                DetermineTable(alteredInflow);
                                rowLists = ValidPathRowEnumerator.ValidRowList(_width, alteredInflow).ToList();
                                outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                counter = 0;
                                while (!(ValidateAndUpdateComponents(alteredInflow, outflowCandidate, components,
                                           currentRow,
                                           out horizontalSpans, 1)))
                                {
                                    outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                    counter++;
                                    if (counter >= MaxDefaultAttempts)
                                    {
                                        throw new Exception("Too many attempts");
                                    }
                                }

                                verticalPaths[currentRow + 1] = outflowCandidate;
                                horizontalPaths[currentRow] = horizontalSpans;
                            }
                        }

                        #endregion
                        #region EndRowCase

                        if (currentRow == end.row)
                        {    
                            // Exits from bottom
                            if (IsBitSet(inflow, end.column))
                            {
                                alteredInflow = BlockBit(alteredInflow, end.column);
                                if (alteredInflow == 0)
                                { 
                                    throw new Exception("Inflow cannot be zero");
                                }
                                DetermineTable(alteredInflow);
                                rowLists = ValidPathRowEnumerator.ValidRowList(_width, alteredInflow).ToList();
                                outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                counter = 0;
                                while (!ValidateAndUpdateComponents(alteredInflow, outflowCandidate, components,
                                           currentRow,
                                           out horizontalSpans, 1) ||
                                       IsBitSet(outflowCandidate, end.column) ||
                                       TotalNumberOfSetBitsBeforeAPos(inflow, outflowCandidate, end.column) % 2 != 0)
                                {
                                    outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                    counter++;
                                    if (counter >= MaxDefaultAttempts)
                                    {
                                        throw new Exception("Too many attempts");
                                    }
                                }

                                verticalPaths[currentRow + 1] = outflowCandidate;
                                horizontalPaths[currentRow] = horizontalSpans;
                            }
                            // Not Exit from bottom
                            else
                            {  
                                alteredInflow = OpenBit(inflow, end.column);
                                components[currentRow][end.column] = -2;
                                DetermineTable(alteredInflow);
                                rowLists = ValidPathRowEnumerator.ValidRowList(_width, alteredInflow).ToList();
                                outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                counter = 0;
                                while (!(ValidateAndUpdateComponents(alteredInflow, outflowCandidate, components,
                                           currentRow,
                                           out horizontalSpans, 1)))
                                {
                                    outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                    counter++;
                                    if (counter >= MaxDefaultAttempts)
                                    {
                                        throw new Exception("Too many attempts");
                                    }
                                }

                                verticalPaths[currentRow + 1] = outflowCandidate;
                                horizontalPaths[currentRow] = horizontalSpans;
                            }
                        }

                        #endregion

                        #region OtherCases
                        if (currentRow != start.row && currentRow != end.row)
                        {
                            DetermineTable(alteredInflow);
                            rowLists = ValidPathRowEnumerator.ValidRowList(_width, alteredInflow).ToList();
                            outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                            counter = 0;
                            while (!(ValidateAndUpdateComponents(alteredInflow, outflowCandidate, components,
                                       currentRow,
                                       out horizontalSpans, 1)) ||
                                   !CheckOneRowBeforeStartAndEnd(outflowCandidate, currentRow, start.row, end.row))
                            {
                                outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                                counter++;
                                if (counter >= MaxDefaultAttempts)
                                {
                                    throw new Exception("Too many attempts");
                                }
                            }

                            verticalPaths[currentRow + 1] = outflowCandidate;
                            horizontalPaths[currentRow] = horizontalSpans;
                        }
                        #endregion

                        // Check if merged
                        if (currentRow > higherRow && NumberOfDistinctValues(components[currentRow + 1]) < 3)
                        {
                            throw new Exception($"Not Merged");
                        }

                    }

                    #endregion

                    #region LastTwoRows

                    bool lastRowFixed = false;
                    counter = 0;
                    while (!lastRowFixed)
                    {
                        counter++;
                        int secondToLastRow = _height - 2;
                        inflow = verticalPaths[secondToLastRow];
                        rowLists = ValidPathRowEnumerator.ValidRowList(_width, inflow).ToList();
                        outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                        while (!ValidateAndUpdateComponents(inflow, outflowCandidate, components, secondToLastRow,
                                   out horizontalSpans) ||
                               NumberOfDistinctValues(components[secondToLastRow + 1]) < 3 ||
                               !CheckOneRowBeforeStartAndEnd(outflowCandidate, secondToLastRow, start.row, end.row))
                        {
                            outflowCandidate = rowLists[_random.Next(rowLists.Count - 1)];
                        }

                        verticalPaths[secondToLastRow + 1] = outflowCandidate;
                        horizontalPaths[secondToLastRow] = horizontalSpans;

                        int lastRow = _height - 1;
                        inflow = verticalPaths[lastRow];
                        if (UpdateLastRowAndValidateComponent(ref horizontalPaths, inflow, lastRow, components))
                        {
                            lastRowFixed = true;
                        }

                        if (counter >= MaxDefaultAttempts)
                        {
                            throw new Exception("Last Row Not Fixable");
                        }
                    }

                    #endregion

                    return (verticalPaths, horizontalPaths);
                }
                catch (Exception ex)
                {
                    // ignored
                }
            }

            throw new Exception("Search Too Long");
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
                components[i] = new int[_width];
            IList<int> inflowList = ValidPathRowEnumerator.InflowsFromBits(_width, firstRow);
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
            List<int> inflows = ValidPathRowEnumerator.InflowsFromBits(_width, inflow);
            List<int> outflows = ValidPathRowEnumerator.InflowsFromBits(_width, outflow);
            List<int> horizontalSpans = ValidPathRowEnumerator.InflowsFromBits(_width, horizontalSpan);

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
        
        private bool UpdateLastRowAndValidateComponent(ref int[] horizontalPaths, int inflow, int rowNumber, IList<IList<int>> components)
        {
            IList<int> inflowList = ValidPathRowEnumerator.InflowsFromBits(_width, inflow);
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
            return ValidPathRowEnumerator.InflowsFromBits(_width, inflow).Contains(pos);
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
            List<int> inflows = ValidPathRowEnumerator.InflowsFromBits(_width, inflow);
            List<int> outflows = ValidPathRowEnumerator.InflowsFromBits(_width, outflow);
            return inflows.Count(n => n < pos) + outflows.Count(n => n < pos);
        }
        
    }
}
