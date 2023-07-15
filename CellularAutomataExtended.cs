using CrawfisSoftware.Collections.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrawfisSoftware.PCG
{
    public class CellularAutomataExtended : CellularAutomata
    {
        public float[,] KernelWeights { get; set; }
        public Func<int, int, int, bool, float, int, int, bool> ComputeCell { get; set; }

        public CellularAutomataExtended(OccupancyGrid occupancyGrid, Random random = null) : base(occupancyGrid, random)
        {
        }

        public CellularAutomataExtended(int width, int height, Random random = null) : base(width, height, random)
        {
        }

        public void IterateFiltered(int numberOfIterations = 1)
        {
            for (int iteration = 0; iteration < numberOfIterations; iteration++)
            {
                PreIterationFunc(iteration);
                for (int column = 0; column < Width; column++)
                {
                    for (int row = 0; row < Height; row++)
                    {
                        float filterValue = GetFilteredValue(column, row, out int numberTrue, out int numberOfNeighbors);
                        bool newValue = ComputeCell(column, row, iteration, _occupancyGridFrontBuffer.GetNodeLabel(column, row), filterValue, numberTrue, numberOfNeighbors);
                        _occupancyGridBackBuffer.MarkCell(column, row, newValue);
                    }
                }
                PostIterationFunc(iteration);
                var tmp = _occupancyGridFrontBuffer;
                _occupancyGridFrontBuffer = _occupancyGridBackBuffer;
                _occupancyGridBackBuffer = tmp;
            }
        }
        protected float GetFilteredValue(int column, int row, out int numberTrue, out int totalCells)
        {
            int neighborhoodWidth = KernelWeights.GetLength(0) / 2;
            int neighborhoodHeight = KernelWeights.GetLength(1) / 2;
            int rowMin = Math.Max(0, row - neighborhoodHeight);
            int rowMax = Math.Min(Height - 1, row + neighborhoodHeight);
            int ColumnMin = Math.Max(0, column - neighborhoodWidth);
            int ColumnMax = Math.Min(Width - 1, column + neighborhoodWidth);
            totalCells = (rowMax - rowMin + 1) * (ColumnMax - ColumnMin + 1);
            float filteredValue = 0;
            if (totalCells <= 0)
                throw new ArgumentOutOfRangeException("row, column of NeighborhoodSize is wrong");

            numberTrue = 0;
            int filterRow = 0;
            for (int rowIndex = rowMin; rowIndex <= rowMax; rowIndex++)
            {
                int filterColumn = 0;
                for (int columnIndex = ColumnMin; columnIndex <= ColumnMax; columnIndex++)
                {
                    bool cellValue = _occupancyGridFrontBuffer.GetNodeLabel(columnIndex, rowIndex);
                    if (cellValue)
                        numberTrue++;
                    float cellAsFloat = (cellValue) ? 1 : 0;
                    filteredValue += cellAsFloat * KernelWeights[filterColumn, filterRow];
                    filterColumn++;
                }
                filterRow++;
            }
            return filteredValue;
        }
    }
}