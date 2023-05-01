using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static CrawfisSoftware.PCG.EnumerationUtilities;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Class to sample a path from top to bottom.
    /// </summary>
    public class PathSamplerBottomToSide
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
        public PathSamplerBottomToSide(int width, int height, Random random,
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
        /// <param name="endRow">The row index of the existing cell</param>
        /// <param name="isLeft">If the path exits from the left</param>

        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public (IList<int> vertical, IList<int> horizontal) Sample(int start, int endRow, bool isLeft)
        {
            int pathID = 0;
            var inFlow = new List<int>() { start };
            var verticalPaths = new int[_height];
            var horizontalPaths = new int[_height];
            int[][] components = new int[_height][];
            for (int i = 0; i < _height; i++)
                components[i] = new int[_width];
            components[0][start] = 1;
            verticalPaths[0] = 1 << start; // row;
            return SampleRecursive(_width, _height, 0, verticalPaths, horizontalPaths, components, 
                pathID, endRow, isLeft,
                _verticalCandidateOracle, _horizontalCandidateOracle);

        }


        private (IList<int> vertical, IList<int> horizontal) SampleRecursive(int width, int height, int index,
            IList<int> verticalGrid, IList<int> horizontalGrid,
            IList<IList<int>> components, int pathID, int endRow, bool isLeft,
            Validator rowCandidateOracle = null,
            Validator horizontalCandidateOracle = null)
        {

            int horizontalSpans;


            int inFlow = verticalGrid[index];
            var inFlows = ValidPathRowEnumerator.InflowsFromBits(width, inFlow);
            var inFlowComponents = new List<int>(inFlows.Count);
            for (int i = 0; i < inFlows.Count; i++)
                inFlowComponents.Add(components[index][inFlows[i]]);

            
            int attempts = 0;
            IList<short> rowLists = ValidPathRowEnumerator.ValidRowList(width, verticalGrid[index]).ToList();
            int listLen = rowLists.Count;
            short rowCandidate = rowLists[_random.Next(listLen)];
            
            while (attempts < MaxDefaultAttempts)
            {
                if (rowCandidateOracle == null ||
                    rowCandidateOracle(pathID, index + 1, rowCandidate, verticalGrid, horizontalGrid, components))
                {
                    verticalGrid[index + 1] = rowCandidate;
                    if (ValidateAndUpdateComponents(inFlow, rowCandidate, components, index, out horizontalSpans,
                            height - index))
                    {
                        if (horizontalCandidateOracle == null || horizontalCandidateOracle(pathID, index + 1,
                                horizontalSpans, verticalGrid, horizontalGrid, components))
                        {
                            horizontalGrid[index + 1] = horizontalSpans;
                            return SampleRecursive(width, height, index + 1, verticalGrid, horizontalGrid, components,
                                pathID++, endRow, isLeft, rowCandidateOracle, horizontalCandidateOracle);

                        }
                    }
                }

                attempts++;
                rowCandidate= rowLists[_random.Next(listLen)];
            }
            
            return (null, null);
        }
    }
}
