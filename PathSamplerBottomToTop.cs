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
    public class PathSamplerBottomToTop
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
        public PathSamplerBottomToTop(int width, int height, Random random,
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
                ValidPathRowEnumerator.BuildOddTables(_width);
            }
            else
            {
                ValidPathRowEnumerator.BuildOddTablesWithConstraints(_width, globalConstraintsOracle);
            }
            
        }

        /// <summary>
        /// Iterate over all non-cyclical paths from a starting cell to an ending cell on an open verticalGrid.
        /// </summary>
        /// <param name="start">The column index of the starting cell on the first row (row 0).</param>
        /// <param name="end">The column index of the ending cell on the last row (row height-1)</param>

        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public (IList<int> vertical, IList<int> horizontal)
            Sample(int start, int end)
        {
            const int MAX_ATTEMPTS = 1000000;
            int currentAttempt = 0;
            int[] verticalPaths = new int[_height + 1];
            int[] horizontalPaths = new int[_height];
            

            while (currentAttempt < MAX_ATTEMPTS)
            {
                try
                {
                    ValidPathRowEnumerator.PointToOddTable();
            
                    #region FirstRow
            
                    int inflow = verticalPaths[0] = 1 << start;
                    int[][] components = new int[_height + 1][];
                    for (int i = 0; i < _height + 1; i++)
                        components[i] = new int[_width];
                    components[0][start] = 1;
                    IList<short> rowLists =
                        ValidPathRowEnumerator.ValidRowList(_width, inflow )
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
                            ValidPathRowEnumerator.ValidRowList(_width, inflow)
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
                            ValidPathRowEnumerator.ValidRowList(_width, inflow)
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
                        int lastOutflow = 1 << end;
                        if (ValidateAndUpdateComponents(inflow, lastOutflow, components, lastRow,
                                   out horizontalSpans))
                        {
                            lastRowFixed = true;
                            horizontalPaths[lastRow] = horizontalSpans;
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
    }
}
