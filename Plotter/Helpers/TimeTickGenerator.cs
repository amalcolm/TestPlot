using System;
using System.Collections.Generic;
using System.Linq;

using ScottPlot;
using SkiaSharp;

namespace Plotter
{
    public class TimeTickGenerator : ITickGenerator
    {
        private Tick[] _ticks = [];
        public Tick[] Ticks => _ticks;

        private int _maxTickCount = 100;
        public int MaxTickCount
        {
            get => _maxTickCount;
            set => _maxTickCount = Math.Max(2, value);
        }

        public void Regenerate(CoordinateRange range, Edge edge, PixelLength size, SKPaint paint, LabelStyle labelStyle)
        {
            // Calculate range in seconds
            double rangeSec = range.Max - range.Min;

            // Let's aim for a tick roughly every 60 pixels
            double pixelsPerTick = 60;
            int targetTickCount = (int)(size.Length / pixelsPerTick);

            double N = 1.0;

            // Calculate tick spacing in seconds (round to nearest N seconds)
            double tickSpacingSeconds = N * Math.Max(1, Math.Round(rangeSec / (targetTickCount * N)));

            // Generate major tick positions
            var ticks = new List<Tick>();
            double currentPosition = Math.Ceiling(range.Min / tickSpacingSeconds) * tickSpacingSeconds;

            while (currentPosition <= range.Max)
            {
                int minutes = (int)(currentPosition / 60);
                int seconds = (int)(currentPosition % 60);
                string label = (seconds % 5 == 0) ? $"{minutes:00}:{seconds:00}" : string.Empty;
                ticks.Add(new Tick(currentPosition, label));
                currentPosition += tickSpacingSeconds;
            }

            // Add minor ticks between major ticks
            if (tickSpacingSeconds >= 30) // Only add minor ticks if major ticks are far enough apart
            {
                double minorSpacing = tickSpacingSeconds / 4; // 4 minor ticks between major ticks
                currentPosition = Math.Ceiling(range.Min / minorSpacing) * minorSpacing;

                while (currentPosition <= range.Max)
                {
                    if (Math.Abs(currentPosition % tickSpacingSeconds) > 0.1) // Not a major tick
                    {
                        ticks.Add(new Tick(currentPosition, string.Empty));
                    }
                    currentPosition += minorSpacing;
                }
            }

            _ticks = [.. ticks.OrderBy(x => x.Position)];
        }
    }
}