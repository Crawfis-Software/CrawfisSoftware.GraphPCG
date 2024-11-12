using System;
using System.Collections.Generic;
using System.Linq;

namespace CrawfisSoftware.PCG
{

    public class SweepingMetrics
    {
        private enum MetricDirection
        {
            LeftRight = 0b1010,
            TopBottom = 0b0101,
            LeftTop = 0b1100,
            TopRight = 0b0110,
            RightBottom = 0b0011,
            BottomLeft = 0b1001
        }
        private int Width{ set; get;}
        /// <summary>
        /// 
        /// </summary>
        public int TotalLen{private set;get;}
        
        public int Straights{private set;get;}
        
        public int Turns{private set;get;}
        
        public int UTuns{private set;get;}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        public SweepingMetrics(int width)
        {
            TotalLen = 0;
            Width = width;
        }

        private SweepingMetrics(SweepingMetrics sweepingMetrics)
        {
            TotalLen = sweepingMetrics.TotalLen;
            Width = sweepingMetrics.Width;
            Turns = sweepingMetrics.Turns;
            Straights = sweepingMetrics.Straights;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflow"></param>
        /// <param name="outflow"></param>
        /// <param name="horizontalSpan"></param>
        public void CalculateMetricCurrentRow(int inflow, int outflow,int horizontalSpan)
        {
            CalculateLength(inflow, outflow, horizontalSpan);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SweepingMetrics Copy()
        {
            return new SweepingMetrics(this);
        }
        private void CalculateLength(int inflow, int outflow,int horizontalSpan)
        {
            for (int i = 0; i < Width; i++)
            {
                int edges = GetEdges(inflow, outflow, horizontalSpan, i);
                if (EnumerationUtilities.CountSetBits(edges) == 2)
                {
                    TotalLen++;
                }

                if (CheckTurns(edges) > 0)
                {
                    Turns++;
                }

                if (CheckStraights(edges))
                {
                    Straights++;
                }
            }
        }
        
        private bool CheckStraights(int edges)
        {
            const int leftRight = 0b1010;
            const int topBottom = 0b0101;

            return (edges ^ leftRight) == 0 ||
                   (edges ^ topBottom) == 0;
        }

        private int CheckTurns(int edges)
        {
            const int leftTop = 0b1100;
            const int topRight = 0b0110;
            const int rightBottom = 0b0011;
            const int bottomLeft = 0b1001;

            if ((edges ^ leftTop) == 0)
                return 1;
            
            if ((edges ^ topRight) == 0)
                return 2;
            
            if ((edges ^ rightBottom) == 0)
                return 3;
            
            if ((edges ^ bottomLeft) == 0)
                return 4;

            return 0;

        }
        
        private int GetEdges(int inflow, int outflow,int horizontalSpan, int cellNumber)
        {
            //int[] edges = new int[4];
            int edges = 0;
            List<int> inflows = ValidPathRowEnumerator.InflowsFromBits(Width, inflow);
            List<int> outflows = ValidPathRowEnumerator.InflowsFromBits(Width, outflow);
            List<int> horizontalSpans = ValidPathRowEnumerator.InflowsFromBits(Width, horizontalSpan);

            if (cellNumber != 0)
            {
                if (horizontalSpans.Contains(cellNumber - 1))
                {
                    edges = OpenBit(edges, 0);
                }
            }

            if (outflows.Contains(cellNumber))
            {
                edges =OpenBit(edges, 1);
            }

            if (horizontalSpans.Contains(cellNumber))
            {
                edges =OpenBit(edges, 2);
            }

            if (inflows.Contains(cellNumber))
            {
                edges =OpenBit(edges, 3);
            }
            
            return edges;
        }
        
        private int BlockBit(int bit, int pos)
        {
            return bit &= ~(1 << pos);
        }

        private int OpenBit(int bit, int pos)
        {
            return bit |= (1 << pos);
        }
        
    }
}