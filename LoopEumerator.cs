using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static CrawfisSoftware.PCG.EnumerationUtilities;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Class to sample a path from top to bottom.
    /// </summary>
    public class LoopEnumerator
    {
        private readonly int _width;
        private readonly int _height;
        
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
        public LoopEnumerator(int width, int height, Random random, Func<int, bool> globalConstraintsOracle = null)
        {
            this._width = width;
            this._height = height;
            
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
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(IList<int> vertical, IList<int> horizontal)> Enumerate()
        {
            IList<int> verticalPaths = new int[_height + 1];
            IList<int> horizontalPaths = new int[_height];
            IList<IList<int>> components = InitializeComponents(0);
            
            foreach (var grid in EnumerateRecursive( 0, verticalPaths, horizontalPaths, components, new SweepingMetrics(_width)))
            {
                yield return grid;
            }
        }
            
        private IEnumerable<(IList<int> vertical, IList<int> horizontal)>
            EnumerateRecursive(int index, IList<int> verticalPaths, IList<int> horizontalPaths, 
                IList<IList<int>> components, SweepingMetrics metrics)
        {
            #region FirstRow
            if (index == 0)
            {
                int inflow = 0;
                components = InitializeComponents(verticalPaths[0]);
                foreach (int outflow in BitEnumerators.AllEven(_width))
                {
                    int horizontalSpans;
                    verticalPaths[1] = outflow;
                    if (ValidateAndUpdateComponents(inflow, outflow, components, 0,
                               out horizontalSpans))
                    {
                        var copy = metrics.Copy();
                        copy.CalculateMetricCurrentRow(inflow, outflow, horizontalSpans);
                        horizontalPaths[0] = horizontalSpans;
                        foreach (var newGrid in EnumerateRecursive(index + 1, verticalPaths, horizontalPaths, components, copy.Copy()))
                        {
                            yield return newGrid;
                        }
                    }
                }
                
            }

            #endregion

            #region MiddleRows

            if (index > 0 && index < _height - 1)
            {
                int inflow = verticalPaths[index];
                foreach (short outflow in ValidPathRowEnumerator.ValidRowList(_width, inflow))
                {
                    verticalPaths[index + 1] = outflow;
                    if (ValidateAndUpdateComponents(inflow, outflow, components, index,
                            out int horizontalSpans))
                    {
                        horizontalPaths[index] = horizontalSpans;
                        var copy = metrics.Copy();
                        copy.CalculateMetricCurrentRow(inflow, outflow, horizontalSpans);
                        foreach (var newGrid in EnumerateRecursive(index + 1, verticalPaths, horizontalPaths,
                                     components, copy.Copy()))
                        {
                            yield return newGrid;
                        }
                    }
                }
            }

            #endregion

            #region LastRows

            if (index == _height - 1)
            {
                int inflow = verticalPaths[index];
                int previousInflow = verticalPaths[index - 1];
                if (UpdateLastRowAndValidateComponent(ref horizontalPaths, inflow, previousInflow, index, components))
                {
                    metrics.CalculateMetricCurrentRow(inflow, 0, horizontalPaths[_height-1]);
                    Console.WriteLine(metrics);
                    yield return (verticalPaths, horizontalPaths);
                    
                }
            }
            #endregion
        }

        private IList<IList<int>> InitializeComponents(int firstRow)
        {
            int[][] components = new int[_height+1][];
            for (int i = 0; i < _height+1; i++)
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
        
        
        private bool UpdateLastRowAndValidateComponent(ref IList<int> horizontalPaths, int inflow, 
            int previousInflow, int rowNumber, IList<IList<int>> components)
        {
            List<int> previousInflows = ValidPathRowEnumerator.InflowsFromBits(_width, previousInflow);
            IList<int> currentInflows = ValidPathRowEnumerator.InflowsFromBits(_width, inflow);
            
            int previousMin = previousInflows.Min();
            int previousMax = previousInflows.Max();
            
            int currentMin = currentInflows.Min();
            int currentMax = currentInflows.Max();

            if (previousMin < currentMin || previousMax > currentMax)
            {
                return false;
            }
            
            
            IList<int> componentList = components[rowNumber];
            int horizontal = 0;
            for (int i = 1; i < currentInflows.Count; i += 2)
            {
                int start = currentInflows[i-1];
                int end = currentInflows[i];
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

        private void PrintFlow(int flow, int width)
        {
            Console.WriteLine(Convert.ToString(flow, 2).PadLeft(width, '0'));
        }
    }
}
