using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CrawfisSoftware.PCG.EnumerationUtilities;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Static class to enumerate paths
    /// </summary>
    public static class PathEnumerationBottomToTop
    {

        /// <summary>
        /// Iterate over all non-cyclical paths from a starting cell to an ending cell on an open verticalGrid.
        /// </summary>
        /// <param name="width">The width of the underlying verticalGrid.</param>
        /// <param name="height">The height of the underlying verticalGrid</param>
        /// <param name="start">The column index of the starting cell on the first row (row 0).</param>
        /// <param name="end">The column index of the ending cell on the last row (row height-1)</param>
        /// <param name="globalConstraintsOracle">Optional function to specify some global constraints on the outflows of a row.</param>
        /// <param name="rowCandidateOracle">Function that returns true or false whether this row is desired. Parameters are: the pathID, the row number,
        /// the current candidate row value (vertical bits), all verticalBits so far, all horizontal bits so far, all components so far.</param>
        /// <param name="horizontalCandidateOracle">Function that returns true or false whether this row is desired. Parameters are: the pathID, the row number,
        /// the current candidate value (horizontal bits), all verticalBits so far, all horizontal bits so far, all components so far.</param>
        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public static IEnumerable<(IList<int> vertical, IList<int> horizontal)> AllPaths(int width, int height, int start, int end, 
            Func<int, bool> globalConstraintsOracle = null, Validator rowCandidateOracle = null,
            Validator horizontalCandidateOracle = null)
        {
            if (globalConstraintsOracle == null)
            {
                ValidPathRowEnumerator.BuildOddTables(width);
            }
            else
            {
                ValidPathRowEnumerator.BuildOddTablesWithConstraints(width, globalConstraintsOracle);
            }
            int pathID = 0;
            var inFlow = new List<int>() { start };
            //var validStates = OutflowState.Up;
            //if (start > 0) validStates |= OutflowState.Left;
            //if (start < width - 1) validStates |= OutflowState.Right;
            //var outFlowStates = new List<OutflowState>() { validStates };
            var outFlowStates = OutflowStates.DetermineOutflowStates(width, inFlow);
            //foreach (var outFlowState in outFlowStates)
            {
                //foreach (int row in RowEnumerator.ValidRowsFixedFlowStates(width, inFlow, outFlowState))
                //foreach (int row in RowEnumerator.ValidRows(width, previousRow))
                {
                    var verticalPaths = new int[height];
                    var horizontalPaths = new int[height];
                    int[][] components = new int[height][];
                    for (int i = 0; i < height; i++)
                        components[i] = new int[width];
                    components[0][start] = 1;
                    verticalPaths[0] = 1 << start; // row;
                    int endRow = 1 << end;
                    verticalPaths[height - 1] = endRow;
                    foreach (var grid in AllPathRecursive(width, height, 0, verticalPaths, horizontalPaths, components, pathID, rowCandidateOracle, horizontalCandidateOracle))
                    {
                        yield return grid;
                    }
                }
            }
            yield break;
        }

        private static IEnumerable<(IList<int> vertical, IList<int> horizontal)> AllPathRecursive(int width, int height, int index, IList<int> verticalGrid, IList<int> horizontalGrid, 
            IList<IList<int>> components, int pathID, 
            Validator rowCandidateOracle = null,
            Validator horizontalCandidateOracle = null)
        {
            int horizontalSpans;
            if (index == (height - 2))
            {
                if (ValidateAndUpdateComponents(verticalGrid[index], verticalGrid[height - 1], components, index, out horizontalSpans))
                {
                    if (horizontalCandidateOracle == null || horizontalCandidateOracle(pathID, height - 1, horizontalSpans, verticalGrid, horizontalGrid, components))
                    {
                        horizontalGrid[height - 1] = horizontalSpans;
                        yield return (verticalGrid, horizontalGrid);
                    }
                }
                yield break;
            }
            // Todo: Compute all Valid OutflowStates (using components)
            //  Loop over those calling a more constrained ValidRows.
            int inFlow = verticalGrid[index];
            var inFlows = ValidPathRowEnumerator.InflowsFromBits(width, inFlow);
            var inFlowComponents = new List<int>(inFlows.Count);
            for (int i = 0; i < inFlows.Count; i++)
                inFlowComponents.Add(components[index][inFlows[i]]);
            foreach (var child in ValidPathRowEnumerator.ValidRowList(width, verticalGrid[index]))
            {
                if (rowCandidateOracle == null || rowCandidateOracle(pathID, index+1, child, verticalGrid, horizontalGrid, components))
                {
                    verticalGrid[index + 1] = child;
                    if (ValidateAndUpdateComponents(inFlow, child, components, index, out horizontalSpans, height - index))
                    {
                        if (horizontalCandidateOracle == null || horizontalCandidateOracle(pathID, index + 1, horizontalSpans, verticalGrid, horizontalGrid, components))
                        {
                            horizontalGrid[index + 1] = horizontalSpans;
                            foreach (var newGrid in AllPathRecursive(width, height, index + 1, verticalGrid, horizontalGrid, components, pathID++, rowCandidateOracle, horizontalCandidateOracle))
                            {
                                yield return newGrid;
                            }
                        }
                    }
                }
            }
            yield break;
        }
        
    }
}
