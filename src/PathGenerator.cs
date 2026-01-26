using System;
using System.Collections.Generic;
using CrawfisSoftware.Path.BitPattern;

namespace CrawfisSoftware.Path
{
    /// <summary>
    /// Create a path using the Path enumerator logic
    /// </summary>
    public class PathGenerator
    {
        private const int maxDefaultAttempts = 10000;
        private readonly int width;
        private readonly int height;
        private readonly Random random;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the underlying grid</param>
        /// <param name="height">The height of the underlying grid</param>
        /// <param name="random">A random number generator</param>
        /// <remarks>Width must be less than 32.</remarks>
        public PathGenerator(int width, int height, System.Random random)
        {
            if (width > 31)
                throw new ArgumentOutOfRangeException("Width must be less than 32");
            this.width = width;
            this.height = height;
            this.random = random;
            ValidPathRowEnumerator.BuildOddTables(width);
        }

        /// <summary>
        /// Generate a random path from the bottom edge to the top edge
        /// </summary>
        /// <param name="start">The column index of the starting cell</param>
        /// <param name="end">The column index of the ending cell</param>
        /// <param name="verticalPaths">A bit pattern of each row that represents the vertical edges of the path</param>
        /// <param name="horizontalPaths">A bit pattern of each row that represents the horizontal edges of the path</param>
        public void GenerateRandomPath(int start, int end, out IList<int> verticalPaths, out IList<int> horizontalPaths)
        {
            // Build row tables for efficiency (if width < 16)
            // For each row
            // Determine # of Row entries and randomly pick one
            // If this row doesn't work with the previous row, repeat above step until it does or some count (or just repeat entire grid all of the time)
            // Ensure the end row is compatible, if not handle previous row like above.
            verticalPaths = new int[height];
            horizontalPaths = new int[height];
            int[][] components = new int[height][];
            for (int i = 0; i < height; i++)
                components[i] = new int[width];
            components[0][start] = 1;
            verticalPaths[0] = 1 << start; // row;
            int endRow = 1 << end;
            verticalPaths[height - 1] = endRow;
            int fullLoopAttempts = 0;
            while (fullLoopAttempts < maxDefaultAttempts)
            {
                int previousRow = verticalPaths[0];
                int horizontalSpans;
                bool success = true;
                for (int j = 0; j < height - 2; j++)
                {
                    if (TryGetValidRow(previousRow, components, j, out int nextRow, out horizontalSpans))
                    {
                        verticalPaths[j + 1] = nextRow;
                        horizontalPaths[j] = horizontalSpans;
                        previousRow = nextRow;
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                {
                    previousRow = verticalPaths[height - 2];
                    int lastRowSearchAttempt = 0;
                    //int endRowBlocks = (1 << width) - 1;
                    //endRowBlocks &= ~endRow;
                    // Todo: Bug: leaves new components dangling at the end of the grid. I think height-1 has the components we want to check.
                    while (lastRowSearchAttempt < maxDefaultAttempts)
                    {
                        if (EnumerationUtilities.ValidateAndUpdateComponents(verticalPaths[height - 2], endRow, components, height - 2, out horizontalSpans, 1))
                        {
                            horizontalPaths[height - 2] = horizontalSpans;
                            break;
                        }
                        int newRow;
                        if (TryGetValidMatchingRow(verticalPaths[height - 2], components, height - 2, out newRow, out horizontalSpans, endRow, endRow))
                        {
                            verticalPaths[height - 1] = newRow;
                            horizontalPaths[height - 2] = horizontalSpans;
                            lastRowSearchAttempt++;
                        }
                        else
                        {
                            lastRowSearchAttempt = maxDefaultAttempts;
                        }
                    }
                    if (lastRowSearchAttempt < maxDefaultAttempts)
                        break;
                }
                fullLoopAttempts++;
            }
            if(fullLoopAttempts >= maxDefaultAttempts)
                throw new InvalidOperationException("Sorry, this search seems to be too hard!");
        }

        private bool TryGetValidMatchingRow(int previousRow, IList<IList<int>> componentsGrid, int index, out int nextRow, out int horizontalSpans, int inFlowMask, int outFlowMask)
        {
            var rowCandidates = ValidPathRowEnumerator.CandidateRows(width, previousRow);
            nextRow = 0;
            horizontalSpans = 0;
            if (rowCandidates.Count == 0)
                return false;
            var candidates = new List<int>();
            foreach (int row in rowCandidates)
            {
                if (((row & inFlowMask) == inFlowMask) && ((row & outFlowMask) == row))
                {
                    if (EnumerationUtilities.ValidateAndUpdateComponents(previousRow, nextRow, componentsGrid, index, out horizontalSpans))
                    {
                        candidates.Add(row);
                    }
                }
            }
            if (candidates.Count == 0)
                return false;
            int randomIndex = random.Next(0, candidates.Count);
            nextRow = candidates[randomIndex];
            EnumerationUtilities.ValidateAndUpdateComponents(previousRow, nextRow, componentsGrid, index, out horizontalSpans);
            return true;
        }
        private bool TryGetValidRow(int previousRow, IList<IList<int>> componentsGrid, int index, out int nextRow, out int horizontalSpans, int maxNumberOfTries = maxDefaultAttempts)
        {

            horizontalSpans = 0;
            nextRow = ValidPathRowEnumerator.GetRandomRow(width, previousRow, random);
            int searchAttempt = 0;
            while((searchAttempt < maxNumberOfTries) && !EnumerationUtilities.ValidateAndUpdateComponents(previousRow, nextRow, componentsGrid, index, out horizontalSpans))
            {
                nextRow = ValidPathRowEnumerator.GetRandomRow(width, previousRow, random);
                searchAttempt++;
            }
            return (searchAttempt < maxNumberOfTries);
        }
    }
}
