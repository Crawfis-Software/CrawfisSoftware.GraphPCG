using System;
using System.Collections.Generic;
using System.Data;
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
    public class PathSamplerArbitrary
    {
        private const int MaxDefaultAttempts = 10000;
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
                ValidPathRowEnumerator.BuildEvenTables(height);
            }
            else
            {
                ValidPathRowEnumerator.BuildOddTablesWithConstraints(width, globalConstraintsOracle);
                ValidPathRowEnumerator.BuildEvenTables(height);
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
            do
            {
                bool isStartVertical = _random.Next(10) <= 9;
                bool isEndVertical = _random.Next(10) <= 9;
            
                int startColumn = start.column;
                int endColumn = end.column;
            
                (int row, int column) newStart = start;
                (int row, int column) newEnd = end;
            
                if (isStartVertical)
                {
                    startColumn = _random.Next(_width);
                    newStart = (start.row, startColumn);
                }
            
                if (isEndVertical)
                {
                    endColumn = _random.Next(_width);
                    newEnd = (end.row, endColumn);
                }
            
                (IList<int> vertical, IList<int> horizontal) values = VerticalSample(newStart, newEnd);
                
                bool startCarved = CarveHorizontal(ref values.horizontal, ref values.vertical, start.row, start.column, startColumn);
                bool endCarved = CarveHorizontal(ref values.horizontal, ref values.vertical,end.row, end.column, endColumn);
                if (startCarved && endCarved)
                {
                    return (values.vertical, values.horizontal);
                }
                
            } while (true);
            
            return VerticalSample(start, end);

        }

        private bool CarveHorizontal(ref IList<int> horizontal, ref IList<int> vertical, int row, int column, int newColumn)
        {
            if (column == newColumn)
            {
                return true;
            }
            int span = horizontal[row];
            var spans = ValidPathRowEnumerator.InflowsFromBits(_width, span);
            
            if (spans.Contains(column) || spans.Contains(column - 1))
            {
                return false;
            }
            
            int small = column <= newColumn ? column : newColumn;
            int big = column > newColumn ? column : newColumn;
            
            int inflow = vertical[row];
            int outflowIndex = row + 1;
            if (row + 1 >= _height)
            {
                outflowIndex = row;
            }
            int outflow = vertical[outflowIndex];
            
            var inflows = ValidPathRowEnumerator.InflowsFromBits(_width, inflow);
            var outflows = ValidPathRowEnumerator.InflowsFromBits(_width, outflow);
            IOrderedEnumerable<int> between = inflows.Where(n => n >= small && n <= big).OrderBy(n => n);

            if (between.Any())
            {
                return false;
            }
            
            between = outflows.Where(n => n >= small && n <= big).OrderBy(n => n);

            if (between.Any())
            {
                return false;
            }

            between = spans.Where(n => n >= small && n <= big).OrderBy(n => n);

            if (between.Any())
            {
                return false;
            }

            for (int j = small; j < big; j++)
            {
                span |= 1 << j;
            }

            
            horizontal[row] = span;
            return true;
        }
        
         private (IList<int> vertical, IList<int> horizontal) 
             VerticalSample((int row, int column) start, (int row, int column) end)
        {
            int MAX_SEARCH_ATTEMPTS = 1000000;

            int pathID = 0;
            var verticalPaths = new int[_height];
            var horizontalPaths = new int[_height];
            IList<IList<int>> components;
            int overAllSearch = 0;
            do
            {
                ValidPathRowEnumerator.PointToEvenTable();
                verticalPaths[0] = EnumerationUtilities.RandomEvenBitPattern(_width, _random);
                components = InitializeComponents(verticalPaths[0]);
                UpdateHorizontalRow(ref horizontalPaths, verticalPaths[0], 0);

                int horizontalSpans = 0;

                bool found = false;
                for (int currentRow = 0; currentRow < _height - 2; currentRow++)
                {
                    int inFlow = verticalPaths[currentRow];
                    int unmodifiedInflow = inFlow;
                    var inFlows = ValidPathRowEnumerator.InflowsFromBits(_width, inFlow);
                    var inFlowComponents = new List<int>(inFlows.Count);
                    for (int i = 0; i < inFlows.Count; i++)
                        inFlowComponents.Add(components[currentRow][inFlows[i]]);
                    
                    bool startCellVisited = false;
                    bool endCellVisited = false;
                    
                    if (currentRow == start.row - 1)
                    {
                        if (inFlows.Contains(start.column))
                        {
                            inFlows.Remove(start.column);
                            inFlow &= ~(1 << start.column);
                            startCellVisited = true;
                        }
                    }
                    
                    if (currentRow == end.row - 1)
                    {
                        if (inFlows.Contains(end.column))
                        {
                            inFlows.Remove(end.column);
                            inFlow &= ~(1 << end.column);
                            startCellVisited = true;
                            endCellVisited = true;
                        }
                    }

                    if (EnumerationUtilities.CountSetBits(inFlow) % 2 == 0)
                    {
                        ValidPathRowEnumerator.PointToEvenTable();
                    }
                    else
                    {
                        ValidPathRowEnumerator.PointToOddTable();
                    }
                    
                    IList<short> rowLists = ValidPathRowEnumerator.ValidRowList(_width, inFlow).ToList();
                    int listLen = rowLists.Count;
                    if (listLen == 0)
                        break;
                    int rowCandidate = rowLists[_random.Next(listLen - 1)];
                    bool validRow = false;
                    bool validStartCell = true;
                    bool validEndCell = true;
                    bool validInflow = true;
                    int rowSearchAttemp = 0;
                    do
                    {
                        if (currentRow != _height - 2)
                        {
                            rowCandidate = rowLists[_random.Next(listLen - 1)];

                            validRow = ValidateAndUpdateComponents(inFlow, rowCandidate, components, currentRow,
                                out horizontalSpans, _height - currentRow);

                            if (currentRow == start.row - 1)
                            {
                                if (!startCellVisited)
                                {
                                    var outflows = ValidPathRowEnumerator.InflowsFromBits(_width, rowCandidate);
                                    if (!outflows.Contains(start.column))
                                    {
                                        rowCandidate |= 1 << start.column;
                                        components[currentRow + 1][start.column] = -5;
                                    }
                                }

                                validStartCell = GetEdges(unmodifiedInflow, rowCandidate, horizontalSpans, start.column)
                                    .Count(s => s == 1) == 1;
                            }

                            if (currentRow == end.row - 1)
                            {
                                if (!endCellVisited)
                                {
                                    var outflows = ValidPathRowEnumerator.InflowsFromBits(_width, rowCandidate);
                                    if (!outflows.Contains(end.column))
                                    {
                                        rowCandidate |= 1 << end.column;
                                        components[currentRow + 1][end.column] = -12;
                                    }
                                }

                                validEndCell = GetEdges(unmodifiedInflow, rowCandidate, horizontalSpans, end.column)
                                    .Count(s => s == 1) == 1;
                            }

                            rowSearchAttemp++;
                            if (rowSearchAttemp > MAX_SEARCH_ATTEMPTS)
                            {
                                break;
                            }
                        }
                    } while (!(validStartCell && validRow && validEndCell));

                    verticalPaths[currentRow + 1] = rowCandidate;
                    horizontalPaths[currentRow + 1] = horizontalSpans;
                    if (currentRow == _height - 3)
                    {
                        found = true;
                    }

                }
                overAllSearch++;
                if (found)
                {
                    break;
                }
            }while(true);


            UpdateHorizontalRow(ref horizontalPaths, verticalPaths[_height - 2], _height - 1);
            
            return (verticalPaths, horizontalPaths);
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
        

        private void UpdateHorizontalRow(ref int[] horizontalPaths, int inflow, int rowNumber)
        {
            IList<int> inflowList = ValidPathRowEnumerator.InflowsFromBits(_width, inflow);
            int horizontal = 0;
            for (int i = 1; i < inflowList.Count; i += 2)
            {
                int start = inflowList[i-1];
                int end = inflowList[i];
                for (int j = start; j < end; j++)
                {
                    horizontal |= 1 << j;
                }
            }
            horizontalPaths[rowNumber] = horizontal;
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
        
        
        
    }
}
