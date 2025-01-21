using System;
using System.Collections.Generic;
using System.Text;
using CrawfisSoftware.Path.BitPattern;

namespace CrawfisSoftware.Path
{
    [Flags]
    internal enum EdgeColor
    {
        None = 0b0000,
        LeftRight = 0b1010,
        TopBottom = 0b0101,
        LeftTop = 0b1100,
        TopRight = 0b0110,
        RightBottom = 0b0011,
        BottomLeft = 0b1001
    }
    /// <summary>
    /// A metric class that calculates the metrics as we sweep up the grid.
    /// </summary>
    public class SweepingMetrics
    {
        private int Width{ set; get;}

        private readonly EdgeColor[] previousRowEdges;
        
        /// <summary>
        /// Total length of the path or loop
        /// </summary>
        public int TotalLen{private set;get;}
        
        
        /// <summary>
        /// Total straights in the path or loop
        /// </summary>
        public int Straights{private set;get;}
        
        /// <summary>
        /// Total turns in the path or loop
        /// </summary>
        public int Turns{private set;get;}
        
        /// <summary>
        /// Total UTuns in the path or loop
        /// </summary>
        public int UTuns{private set;get;}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        public SweepingMetrics(int width)
        {
            Width = width;
            previousRowEdges = new EdgeColor[Width];
        }
        
        private SweepingMetrics(SweepingMetrics sweepingMetrics)
        {
            TotalLen = sweepingMetrics.TotalLen;
            Width = sweepingMetrics.Width;
            Turns = sweepingMetrics.Turns;
            Straights = sweepingMetrics.Straights;
            UTuns = sweepingMetrics.UTuns;
            previousRowEdges = new EdgeColor[Width];
            for (int i = 0; i < previousRowEdges.Length; i++)
            {
                previousRowEdges[i] = sweepingMetrics.previousRowEdges[i];
            }
        }

        /// <summary>
        /// Convert the metrics as string
        /// </summary>
        /// <returns>String format of the metric</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Total Len: {TotalLen}");
            sb.AppendLine($"Turns: {Turns}");
            sb.AppendLine($"Straights: {Straights}");
            sb.AppendLine($"UTuns: {UTuns}");
            return sb.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflow"></param>
        /// <param name="outflow"></param>
        /// <param name="horizontalSpan"></param>
        public void CalculateMetricCurrentRow(int inflow, int outflow,int horizontalSpan)
        {
            int previousEdges = 0;
            int[] edges = GetEdges(inflow, outflow, horizontalSpan);
            for (int i = 0; i < Width; i++)
            {
                if (EnumerationUtilities.CountSetBits(edges[i]) == 2)
                {
                    TotalLen++;
                }
                
                if (CheckTurns((EdgeColor)edges[i]))
                {
                    Turns++;
                }

                if (CheckStraights((EdgeColor)edges[i]))
                {
                    Straights++;
                }
                
                if (CheckHorizontalUTurns((EdgeColor)edges[i], (EdgeColor)previousEdges) || CheckVerticalUTurns((EdgeColor)edges[i], i))
                {
                    UTuns++;
                }
                
                previousEdges = edges[i];
                previousRowEdges[i] = (EdgeColor)edges[i];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SweepingMetrics Copy()
        {
            return new SweepingMetrics(this);
        }

        private bool CheckVerticalUTurns(EdgeColor edges, int cellNum)
        {
            if ((edges == EdgeColor.RightBottom)&&
                (previousRowEdges[cellNum] == EdgeColor.TopRight))
                return true;
            if ((edges == EdgeColor.BottomLeft) &&
                (previousRowEdges[cellNum] == EdgeColor.LeftTop))
                return true;
            return false;
        }

        private bool CheckHorizontalUTurns(EdgeColor currentEdges, EdgeColor previousEdges)
        {
            if ((currentEdges == EdgeColor.LeftTop) &&
                (previousEdges == EdgeColor.TopRight))
                return true;
            if ((currentEdges == EdgeColor.BottomLeft) &&
                (previousEdges == EdgeColor.RightBottom))
                return true;
            return false;
        }
        
        private bool CheckStraights(EdgeColor edges)
        {

            return (edges == EdgeColor.LeftRight) ||
                   (edges == EdgeColor.TopBottom);
        }

        private bool CheckTurns(EdgeColor edges)
        {
            return (edges == EdgeColor.LeftTop)||
                   (edges == EdgeColor.TopRight) ||
                   (edges == EdgeColor.RightBottom) ||
                   (edges == EdgeColor.BottomLeft);
        }
        
        private int[] GetEdges(int inflow, int outflow,int horizontalSpan)
        {
            int[] edges = new int[Width];
            List<int> inflows = ValidPathRowEnumerator.InflowsFromBits(Width, inflow);
            List<int> outflows = ValidPathRowEnumerator.InflowsFromBits(Width, outflow);
            List<int> horizontalSpans = ValidPathRowEnumerator.InflowsFromBits(Width + 1, horizontalSpan);

            for (int cellNum = 0; cellNum < Width; cellNum++)
            {
                edges[cellNum] = 0;
                if (cellNum != 0)
                {
                    if (horizontalSpans.Contains(cellNum - 1))
                    {
                        edges[cellNum] = OpenBit(edges[cellNum], 3);
                    }
                }

                if (outflows.Contains(cellNum))
                {
                    edges[cellNum] = OpenBit(edges[cellNum], 2);
                }

                if (horizontalSpans.Contains(cellNum))
                {
                    edges[cellNum] = OpenBit(edges[cellNum], 1);
                }

                if (inflows.Contains(cellNum))
                {
                    edges[cellNum] = OpenBit(edges[cellNum], 0);
                }
            }
            
            return edges;
        }
        
        private int BlockBit(int bit, int pos)
        {
            return bit & ~(1 << pos);
        }

        private int OpenBit(int bit, int pos)
        {
            return bit | (1 << pos);
        }
        
        
    }
}