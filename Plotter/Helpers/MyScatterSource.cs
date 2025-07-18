using System;
using System.Collections.Generic;
using ScottPlot;

namespace Plotter
{
    public class MyScatterSource : IScatterSource
    {
        private readonly List<Coordinates> xy = [];
        public double Xspacing { get; set; } = 0.25;

        // Track bounds as we add points instead of maintaining separate lists
        private double minX => 0;
        private double maxX => Xspacing * (Count-1);

        private double minY = double.MaxValue;
        private double maxY = double.MinValue;

        // Expose count for external usage
        public int Count => xy.Count;

        public void Add(double y)
        {
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
            xy.Add(new Coordinates(Xspacing * Count, y));
        }

        public IReadOnlyList<Coordinates> GetScatterPoints()
        {
            // If MinRenderIndex and MaxRenderIndex define a valid range, return a slice
            if (MinRenderIndex >= 0 && MaxRenderIndex < xy.Count && MinRenderIndex <= MaxRenderIndex)
            {
                return xy.GetRange(MinRenderIndex, MaxRenderIndex - MinRenderIndex + 1);
            }

            // Otherwise return the full list
            return xy;
        }

        public DataPoint GetNearest(Coordinates location, RenderDetails renderInfo, float maxDistance = 15)
        {
            if (xy.Count == 0) return DataPoint.None;

            double minDist = double.MaxValue;
            int minIndex = -1;

            // Optimize search by starting from visible range if defined
            int startIdx = Math.Max(MinRenderIndex, 0);
            int endIdx = Math.Min(MaxRenderIndex, xy.Count - 1);

            for (int i = endIdx; i >= startIdx; i--)
            {
                var c = xy[i];
                var d = c.Distance(location);   if (d > maxDistance) continue;
                if (d < minDist)
                {
                    minDist = d;
                    minIndex = i;
                }
            }

            return minIndex < 0 ? DataPoint.None : new DataPoint(xy[minIndex], minIndex);
        }

        public DataPoint GetNearestX(Coordinates location, RenderDetails renderInfo, float maxDistance = 15)
        {
            if (xy.Count == 0) return DataPoint.None;

            // Handle out of bounds with early returns
            if (location.X < minX - maxDistance) return DataPoint.None;
            if (location.X > maxX + maxDistance) return DataPoint.None;

            // Calculate nearest index based on X spacing
            double dIndex = location.X / Xspacing;
            int index = (int)Math.Round(dIndex);

            // Clamp index to valid range
            index = Math.Max(0, Math.Min(index, xy.Count - 1));

            return Math.Abs(xy[index].X - location.X) <= maxDistance
                ? new DataPoint(xy[index], index)
                : DataPoint.None; ;
        }

        public CoordinateRange GetLimitsX()
            => (xy.Count == 0) 
                ? new CoordinateRange(0, 1)
                : new CoordinateRange(minX, maxX);
            

        public CoordinateRange GetLimitsY()
            => (xy.Count == 0) 
                ? new CoordinateRange(0, 1)
                : new CoordinateRange(minY, maxY);

        public AxisLimits GetLimits()
            => (xy.Count == 0)
                ? new AxisLimits(0, 1, 0, 1)
                : new AxisLimits(minX, maxX, minY, maxY);
            

        // These control what range of points to render
        public int MinRenderIndex { get; set; } = 0;
        public int MaxRenderIndex { get; set; } = int.MaxValue;
    }
}
