using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawfisSoftware.PCG
{
    public class PathGenerator
    {
        private const int maxDefaultAttempts = 1000;
        private readonly int width;
        private readonly int height;
        private readonly Random random;

        public PathGenerator(int width, int height, System.Random random)
        {
            this.width = width;
            this.height = height;
            this.random = random;
            RowEnumerator.BuildOddTables(width);
        }
        public IList<int> GenerateRandomPath(int start, int end)
        {
            // Build row tables for efficiency (if width < 16)
            // For each row
            // Determine # of Row entries and randomly pick one
            // If this row doesn't work with the previous row, repeat above step until it does or some count (or just repeat entire grid all of the time)
            // Ensure the end row is compatible, if not handle previous row like above.
            var verticalPaths = new int[height];
            int[][] components = new int[height][];
            for (int i = 0; i < height; i++)
                components[i] = new int[width];
            components[0][start] = 1;
            verticalPaths[0] = 1 << start; // row;
            int endRow = 1 << end;
            verticalPaths[height - 1] = endRow;
            //verticalPaths[height - 1] = endRow;
            int previousRow = verticalPaths[0];
            for (int j = 0; j < height-2; j++)
            {
                int nextRow;
                if (TryGetValidRow(previousRow, components, j, out nextRow))
                {
                    verticalPaths[j+1] = nextRow;
                    previousRow = nextRow;
                }
                else
                {
                    throw new InvalidOperationException("Sorry, this search seems to be too hard!");
                }
            }
            int secondToLastRow;
            previousRow = verticalPaths[height - 2];
            int lastRowSearchAttempt = 0;
            while (lastRowSearchAttempt < maxDefaultAttempts)
            {
                if (PathEnumeration.ValidateAndUpdateComponents(previousRow, endRow, components, height - 2, 1))
                    break;
                if (TryGetValidRow(verticalPaths[height-3], components, height - 3, out previousRow))
                {
                    verticalPaths[height - 2] = previousRow;
                    //previousRow = secondToLastRow;
                    lastRowSearchAttempt++;
                    //break;
                }
                else
                {
                    lastRowSearchAttempt = maxDefaultAttempts;
                }
            }
            if(lastRowSearchAttempt >= maxDefaultAttempts)
                throw new InvalidOperationException("Sorry, this search seems to be too hard!");

            return verticalPaths;
        }

        private bool TryGetValidRow(int previousRow, IList<IList<int>> componentsGrid, int index, out int nextRow, int maxNumberOfTries = maxDefaultAttempts)
        {
            nextRow = RowEnumerator.GetRandomRow(width, previousRow, random);
            int searchAttempt = 0;
            while((searchAttempt < maxNumberOfTries) && !PathEnumeration.ValidateAndUpdateComponents(previousRow, nextRow, componentsGrid, index))
            {
                nextRow = RowEnumerator.GetRandomRow(width, previousRow, random);
                searchAttempt++;
            }
            return (searchAttempt < maxNumberOfTries);
        }
    }
}
