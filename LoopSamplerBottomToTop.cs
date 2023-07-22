using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static CrawfisSoftware.PCG.EnumerationUtilities;

namespace CrawfisSoftware.PCG
{
    public class LoopSamplerBottomToTop
    {
        
        private const int MaxDefaultAttempts = 10000;
        private readonly int _width;
        private readonly int _height;
        private readonly Random _random;
        private readonly EnumerationUtilities.Validator _verticalCandidateOracle;
        private readonly EnumerationUtilities.Validator _horizontalCandidateOracle;


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
        public LoopSamplerBottomToTop(int width, int height, Random random,
            Func<int, bool> globalConstraintsOracle = null, EnumerationUtilities.Validator verticalCandidateOracle = null,
            EnumerationUtilities.Validator horizontalCandidateOracle = null)
        {
            this._width = width;
            this._height = height - 1;
            this._random = random;
            this._verticalCandidateOracle = verticalCandidateOracle;
            this._horizontalCandidateOracle = horizontalCandidateOracle;

            if (globalConstraintsOracle == null)
            {
                ValidPathRowEnumerator.BuildEvenTables(width);
            }
            else
            {
                ValidPathRowEnumerator.BuildEvenTablesWithConstraints(width, globalConstraintsOracle);
            }
        }

        /// <summary>
        /// Iterate over all non-cyclical paths from a starting cell to an ending cell on an open verticalGrid.
        /// </summary>
        /// <param name="start">The column index of the starting cell on the first row (row 0).</param>
        /// <param name="end">The column index of the ending cell on the last row (row height-1)</param>

        /// <returns>A value tuple of a list of vertical bits and a list of horizontal bits.</returns>
        public (IList<int> vertical, IList<int> horizontal) Sample()
        {
            int pathID = 0;
            var verticalPaths = new int[_height];
            var horizontalPaths = new int[_height + 1];
            int[][] components = new int[_height][];
            for (int i = 0; i < _height; i++)
                components[i] = new int[_width];

            
                    
            
            // verticalPaths[0] = (int)Math.Pow(2, _width -1) + 1; // row;
            // horizontalPaths[0] = (int)Math.Pow(2, _width) - 1;

            verticalPaths[0] = EnumerationUtilities.RandomBitPattern(_width, _random, 2);
            horizontalPaths[0] = HorizontalOnEdges(verticalPaths[0], _width);

            components[0] = DetermineComponent(horizontalPaths[0], _width);
            
            verticalPaths[_height - 1] = (int)Math.Pow(2, _width -1) + 1; // row;
            //verticalPaths[_height - 1] = EnumerationUtilities.RandomBitPattern(_width, _random, 2);

            //int endRow = verticalPaths[0];
            //verticalPaths[_height - 1] = endRow;
            int attempt = 0;
            while (attempt < MaxDefaultAttempts)
            {
                try
                {
                    return SampleRecursive(_width, _height, 0, verticalPaths, horizontalPaths, components, pathID,
                        _verticalCandidateOracle, _horizontalCandidateOracle);
                }
                catch (Exception)
                {
                    attempt++;
                }
            }
            throw new Exception("Unable to form a loop");

        }


        private (IList<int> vertical, IList<int> horizontal) SampleRecursive(int width, int height, int index,
            IList<int> verticalGrid, IList<int> horizontalGrid,
            IList<IList<int>> components, int pathID,
            EnumerationUtilities.Validator rowCandidateOracle = null,
            EnumerationUtilities.Validator horizontalCandidateOracle = null)
        {
            int horizontalSpans;

            if (index == (height - 2))
            {
                int lastRowAttempts = 0;
                while (lastRowAttempts < MaxDefaultAttempts)
                {
                    if (ValidateAndUpdateComponents(verticalGrid[index], verticalGrid[height - 1], components, index,
                            out horizontalSpans))
                    {
                        if (horizontalCandidateOracle == null || horizontalCandidateOracle(pathID, height - 1,
                                horizontalSpans, verticalGrid, horizontalGrid, components))
                        {
                            horizontalGrid[height - 1] = horizontalSpans;
                            horizontalGrid[height] = (int)Math.Pow(2, _width) - 1;
                            
                            //horizontalGrid[height] = HorizontalOnEdges(verticalGrid[height], _width);;
                            Console.WriteLine($"Number of components: {components.Distinct().Count()}");
                            return (verticalGrid, horizontalGrid);
                        }
                    }

                    lastRowAttempts++;
                }
                if (lastRowAttempts == MaxDefaultAttempts)
                    throw new Exception("Unable to find a valid last row.");

            }
            

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
                                pathID++, rowCandidateOracle, horizontalCandidateOracle);

                        }
                    }
                }

                attempts++;
                rowCandidate= rowLists[_random.Next(listLen)];
            }
            
            return (null, null);
        }
        
        private int HorizontalOnEdges(int verticalBitPattern, int width)
        {
            int onesSoFar = 0;
            int horizontal = 0;
            for (int i = 0; i < width; i++)
            {
                int shifted = verticalBitPattern >> i;
                if ((shifted & 1) == 1)
                {
                    onesSoFar += 1;
                }

                if (onesSoFar % 2 != 0)
                {
                    horizontal = horizontal | (1 << i);
                }
                
            }

            return horizontal;
        }

        private int[] DetermineComponent(int horizontalBitPattern, int width)
        {
            int[] component = new int[width];
            int num = 1;
            int previous = 2;
            for (int i = 0; i < width; i++)
            {
                int shifted = horizontalBitPattern >> i;
                if ((shifted & 1) == 1)
                {
                    component[width - 1 - i] = num;
                    previous = 1;
                } else if ( ((shifted & 1) == 0) && previous == 1 )
                {
                    num++;
                    previous = 0;
                }
            }

            return component;
        }
    }

}