using System;
using System.Collections.Generic;
using ScottPlot;

namespace Plotter
{
    public class MySignalSource : ISignalSource
    {
        const int MaximumValues = 80;
        
        private readonly List<double> values = [];
        private double minValue = double.MaxValue;
        private double maxValue = double.MinValue;

        public double Period { get; set; } = 0.25;  // Time between samples in seconds
        public double XOffset { get; set; } = 0;
        public double YOffset { get; set; } = 0;
        public double YScale { get; set; } = 1.0;
        

        public int MaximumIndex
        {
            get => _count - 1;
            set { /* Ignore attempts to set - always based on count */ }
        }

        public int MinimumIndex
        {
            get => 0;  // Always start from beginning
            set { /* Ignore attempts to set */ }
        }
        private int _count = 0;  // Track number of values added

        public void Add(double value)
        {
            values.Add(value);
            _count++;

//            if (_count > MaximumValues)
//                values.RemoveAt(0);

            minValue = Math.Min(minValue, value);
            maxValue = Math.Max(maxValue, value);
        }
        public int GetIndex(double x, bool clamp)
        {
            int index = (int)Math.Round((x - XOffset) / Period);

            if (!clamp)
                return index;

            return Math.Max(MinimumIndex, Math.Min(MaximumIndex, index));
        }

        public AxisLimits GetLimits()
        {
            if (values.Count == 0)
                return new AxisLimits(0, Period, 0, 1);

            double xMin = GetX(MinimumIndex);
            double xMax = GetX(MaximumIndex);
            return new AxisLimits(xMin, xMax, minValue * YScale + YOffset, maxValue * YScale + YOffset);
        }

        public CoordinateRange GetLimitsX()
        {
            if (values.Count == 0)
                return new CoordinateRange(0, Period);

            return new CoordinateRange(
                GetX(MinimumIndex),
                GetX(MaximumIndex)
            );
        }

        public CoordinateRange GetLimitsY()
        {
            if (values.Count == 0)
                return new CoordinateRange(0, 1);

            return new CoordinateRange(
                minValue * YScale + YOffset,
                maxValue * YScale + YOffset
            );
        }

        public PixelColumn GetPixelColumn(IAxes axes, int xPixelIndex)
        {
            var xCoord = axes.GetCoordinateX(xPixelIndex);
            int index = GetIndex(xCoord, true);

            if (index < 0 || index >= values.Count)
                return new PixelColumn(xPixelIndex, float.NaN, float.NaN, float.NaN, float.NaN);

            // Get current value and adjacent values for enter/exit points
            float currentY = (float)GetY(index);
            float prevY = (float)GetY(index - 1);
            float nextY = (float)GetY(index + 1);

            // Calculate enter and exit points
            float enter = float.IsNaN(prevY) ? currentY : (prevY + currentY) / 2;
            float exit = float.IsNaN(nextY) ? currentY : (currentY + nextY) / 2;

            // Calculate bottom and top for the column
            float bottom = Math.Min(enter, exit);
            float top = Math.Max(enter, exit);

            return new PixelColumn(xPixelIndex, enter, exit, bottom, top);
        }
        
        public double GetX(int index)
        {
            return index * Period + XOffset;
        }

        public double GetY(int index)
        {
            if (index < 0 || index >= values.Count)
                return double.NaN;

            return values[index] * YScale + YOffset;
        }

        public IReadOnlyList<double> GetYs()
        {
            return values;
        }

        public IEnumerable<double> GetYs(int index1, int index2)
        {
            int start = Math.Max(0, Math.Min(index1, index2));
            int end = Math.Min(values.Count - 1, Math.Max(index1, index2));

            for (int i = start; i <= end; i++)
                yield return values[i];
        }

        public void Clear()
        {
            values.Clear();
            minValue = double.MaxValue;
            maxValue = double.MinValue;
            _count = 0;
        }
    }
}