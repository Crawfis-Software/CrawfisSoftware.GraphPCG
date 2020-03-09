using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CrawfisSoftware.PCG.Loops
{
    public class LoopGenerator : MazeBuilderAbstract<int,int>
    {
        int[] rowValues;
        int[] newRowValues;
        int currentNumberOfComponents = 0;
        int currentNewComponentNumber = 0;
        int currentRow = 0;
        List<int> spanEndPoints = new List<int>();
        List<int> newSpanEndPoints = new List<int>();
        public LoopGenerator(int width, int height) : base(width, height, NodeValues, EdgeValues)
        {
            this.width = width;
            this.height = height;
            rowValues = new int[width];
            newRowValues = new int[width];
        }

        public override void CreateMaze(bool preserveExistingCells)
        {
            CreateBaseRow(this.RandomGenerator.Next(width / 50) + 1);
            currentNewComponentNumber = currentNumberOfComponents;
            for(int row=1; row < height-1; row++)
            {
                currentRow++;
                CreateNextRow(this.RandomGenerator.Next(width / 3));
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
            for(int i = start; i < end; i++)
            {
                CarvePassage(i + currentRow * width, i + currentRow * width + 1);
            }
        }

        private void CreateBaseRow(int numberOfSpans)
        {
            int spanLength = width - 2 * numberOfSpans;
            int lastPoint = -1;
            for(int i = 0; i < numberOfSpans*2; i++)
            {
                int nextPoint = this.RandomGenerator.Next(spanLength) + lastPoint+1;
                spanEndPoints.Add(nextPoint);
                lastPoint = nextPoint;
                spanLength = width - lastPoint - (2*numberOfSpans-i-1);
            }
            spanEndPoints.Sort();
            int component = 1;
            for(int i = 0; i < spanEndPoints.Count; i+=2)
            {
                int start = spanEndPoints[i];
                CarvePassage(start, start + width);
                int end = spanEndPoints[i + 1];
                CarvePassage(end, end + width);
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
            int componentToTryToRemove = 0;
            //if (desiredNumberOfComponents < currentNumberOfComponents)
            //{
            //    componentToTryToRemove = this.RandomGenerator.Next(currentNumberOfComponents) + 1;
            //}
            spanEndPoints.Add(width);
            for (int i = 0; i < spanEndPoints.Count-1; i++)
            {
                int start = spanEndPoints[i];
                int next = spanEndPoints[i + 1];
                //if (desiredNumberOfComponents > currentNumberOfComponents)
                //{
                //    lastEnd = AddNewSpan(lastEnd + 1, start - 1);
                //}
                //if(rowValues[start] == componentToTryToRemove)
                //{
                //    // Component logic is broken here. While try to fix later.
                //    lastEnd = next;
                //    i++;
                //    continue;
                //}
                // Pick a random location to move up for start.
                int spanWidth = next - lastEnd - 2;
                int newConnection = this.RandomGenerator.Next(spanWidth) + lastEnd + 1;
                newSpanEndPoints.Add(newConnection);
                int component = rowValues[start];
                newRowValues[newConnection] = component;
                CarvePassage(newConnection + currentRow*width, newConnection + width + currentRow * width);
                if( newConnection > start )
                {
                    for(int cell=start; cell < newConnection; cell++)
                    {
                        CarvePassage(cell + currentRow * width, cell + 1 + currentRow * width);
                        newRowValues[cell] = component;
                        lastEnd = newConnection;
                    }
                }
                else
                {
                    for (int cell = newConnection; cell < start; cell++)
                    {
                        CarvePassage(cell + currentRow * width, cell + 1 + currentRow * width);
                        newRowValues[cell] = component;
                        lastEnd = start;
                    }
                }
                // Pick a random location to move up for end.
                //spanWidth = next - lastEnd - 2;
                //newConnection = this.RandomGenerator.Next(spanWidth) + lastEnd + 1;
                //newSpanEndPoints.Add(newConnection);
                //component = rowValues[next];
                //newRowValues[newConnection] = component;
                //CarvePassage(newConnection+ currentRow*width, newConnection + width)+ currentRow*width;
                //if (newConnection > start)
                //{
                //    for (int cell = start; cell < newConnection; cell++)
                //    {
                //        CarvePassage(cell+ currentRow*width, cell + 1+ currentRow*width);
                //        newRowValues[cell] = component;
                //        lastEnd = newConnection;
                //    }
                //}
                //else
                //{
                //    for (int cell = newConnection; cell < start; cell++)
                //    {
                //        CarvePassage(cell+ currentRow*width, cell + 1+ currentRow*width);
                //        newRowValues[cell] = component;
                //        lastEnd = newConnection;
                //    }
                //}
            }
        }

        private int AddNewSpan(int start, int end)
        {
            int maxSpanLength = end - start;
            if (maxSpanLength < 1) return start-1;
            int newSpanStart = this.RandomGenerator.Next(maxSpanLength - 1) + start;
            int newSpanEnd = this.RandomGenerator.Next(end - newSpanStart) + newSpanStart;
            if (newSpanEnd > newSpanStart)
            {
                currentNewComponentNumber++;
                newSpanEndPoints.Add(newSpanStart);
                CarvePassage(newSpanStart + currentRow * width, newSpanStart + width + currentRow * width);
                newSpanEndPoints.Add(newSpanEnd);
                CarvePassage(newSpanEnd + currentRow * width, newSpanEnd + width + currentRow * width);
                for (int i = newSpanStart; i < newSpanEnd; i++)
                {
                    newRowValues[i] = currentNewComponentNumber;
                    CarvePassage(i + currentRow * width, i + 1 + currentRow * width);
                }
                newRowValues[newSpanEnd] = currentNewComponentNumber;

                return newSpanEnd;
            }
            return start-1;
        }

        private static int NodeValues(int i, int j)
        {
            return 1;
        }
        private static int EdgeValues(int i, int j, Direction dir)
        {
            return 1;
        }
    }
}
