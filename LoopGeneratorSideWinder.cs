using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using System.Collections.Generic;


namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Generate a random loop on a grid that consists of two paths that merge at
    /// the bottom row and top row.
    /// </summary>
    public class LoopGeneratorSideWinder : MazeBuilderAbstract<int, int>
    {
        // Todo: Add Properties or functions for:
        //    min and max horizontal span width
        //        - per row? 
        //        - per left  versus per right
        //    Initial row span
        //    Max left edge per row (could be zero to force to edge)
        //    Min right edge per row (could be Width-1 to force to edge)
        //    Bool on whether to allow "overlap" between rows (or int for minimum spread)
        int[] rowValues;
        int[] newRowValues;
        int currentNumberOfComponents = 0;
        int currentNewComponentNumber = 0;
        int currentRow = 0;
        List<int> spanEndPoints = new List<int>();
        List<int> newSpanEndPoints = new List<int>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the desired maze</param>
        /// <param name="height">The height of the desired maze</param>
        public LoopGeneratorSideWinder(int width, int height) : base(width, height)
        {
            this.Width = width;
            this.Height = height;
            rowValues = new int[width];
            newRowValues = new int[width];
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells)
        {
            CreateBaseRow(1);
            currentNewComponentNumber = currentNumberOfComponents;
            for (int row = 1; row < Height - 1; row++)
            {
                currentRow++;
                CreateNextRow(2);
                int[] tempSwapArray = rowValues;
                rowValues = newRowValues;
                newRowValues = tempSwapArray;
                List<int> tempSwap = spanEndPoints;
                spanEndPoints = newSpanEndPoints;
                newSpanEndPoints = tempSwap;
                newSpanEndPoints.Clear();
                for (int i = 0; i < newRowValues.Length; i++)
                    newRowValues[i] = 0;
            }
            currentRow++;
            CloseTopOfLoop();
        }

        private void CloseTopOfLoop()
        {
            int start = spanEndPoints[0];
            int end = spanEndPoints[spanEndPoints.Count - 1];
            for (int i = start; i < end; i++)
            {
                CarvePassage(i + currentRow * Width, i + currentRow * Width + 1);
            }
        }

        private void CreateBaseRow(int numberOfSpans)
        {
            int spanLength = Width - 2 * numberOfSpans;
            int lastPoint = -1;
            for (int i = 0; i < numberOfSpans * 2; i++)
            {
                int nextPoint = this.RandomGenerator.Next(spanLength) + lastPoint + 1;
                spanEndPoints.Add(nextPoint);
                lastPoint = nextPoint;
                spanLength = Width - lastPoint - (2 * numberOfSpans - i - 1);
            }
            spanEndPoints.Sort();
            int component = 1;
            for (int i = 0; i < spanEndPoints.Count; i += 2)
            {
                int start = spanEndPoints[i];
                CarvePassage(start, start + Width);
                int end = spanEndPoints[i + 1];
                CarvePassage(end, end + Width);
                rowValues[end] = component;
                for (int j = start; j < end; j++)
                {
                    rowValues[j] = component;
                    CarvePassage(j, j + 1);
                }
                component++;
            }
        }
        private void CreateNextRow(int desiredNumberOfComponents)
        {
            int lastEnd = -1;
            spanEndPoints.Add(Width);
            for (int i = 0; i < spanEndPoints.Count - 1; i++)
            {
                int start = spanEndPoints[i];
                int next = spanEndPoints[i + 1];
                //if (desiredNumberOfComponents > currentNumberOfComponents)
                //{
                //    lastEnd = AddNewSpan(lastEnd + 1, start - 1);
                //}
                //if(rowValues[start] == componentToTryToRemove)
                //{
                //    // Component logic is broken here. Will try to fix later.
                //    lastEnd = next;
                //    i++;
                //    continue;
                //}
                // Pick a random location to move up for start.
                int spanWidth = next - lastEnd - 2;
                spanWidth = spanWidth > 0 ? spanWidth : 0;
                int newConnection = this.RandomGenerator.Next(spanWidth) + lastEnd + 1;
                newSpanEndPoints.Add(newConnection);
                int component = rowValues[start];
                newRowValues[newConnection] = component;
                CarvePassage(newConnection + currentRow * Width, newConnection + Width + currentRow * Width);
                if (newConnection > start)
                {
                    for (int cell = start; cell < newConnection; cell++)
                    {
                        CarvePassage(cell + currentRow * Width, cell + 1 + currentRow * Width);
                        newRowValues[cell] = component;
                        lastEnd = newConnection;
                    }
                }
                else
                {
                    for (int cell = newConnection; cell < start; cell++)
                    {
                        CarvePassage(cell + currentRow * Width, cell + 1 + currentRow * Width);
                        newRowValues[cell] = component;
                        lastEnd = start;
                    }
                }
            }
        }

        private int AddNewSpan(int start, int end)
        {
            int maxSpanLength = end - start;
            if (maxSpanLength < 1) return start - 1;
            int newSpanStart = this.RandomGenerator.Next(maxSpanLength - 1) + start;
            int newSpanEnd = this.RandomGenerator.Next(end - newSpanStart) + newSpanStart;
            if (newSpanEnd > newSpanStart)
            {
                currentNewComponentNumber++;
                newSpanEndPoints.Add(newSpanStart);
                CarvePassage(newSpanStart + currentRow * Width, newSpanStart + Width + currentRow * Width);
                newSpanEndPoints.Add(newSpanEnd);
                CarvePassage(newSpanEnd + currentRow * Width, newSpanEnd + Width + currentRow * Width);
                for (int i = newSpanStart; i < newSpanEnd; i++)
                {
                    newRowValues[i] = currentNewComponentNumber;
                    CarvePassage(i + currentRow * Width, i + 1 + currentRow * Width);
                }
                newRowValues[newSpanEnd] = currentNewComponentNumber;

                return newSpanEnd;
            }
            return start - 1;
        }
    }
}
