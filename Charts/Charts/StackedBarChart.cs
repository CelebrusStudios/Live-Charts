﻿//The MIT License(MIT)

//Copyright(c) 2015 Alberto Rodriguez

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LiveCharts.Charts
{
    public class StackedBarChart : Chart
    {
        public StackedBarChart()
        {
            PrimaryAxis = new Axis();
            SecondaryAxis = new Axis {Separator = new Separator {Step = 1}};
            Hoverable = true;
            PrimaryAxis.MinValue = 0d;
            ShapeHoverBehavior = ShapeHoverBehavior.Shape;
        }

        /// <summary>
        /// Gets or sets maximum column width, default is 60
        /// </summary>
        public double MaxColumnWidth { get; set; } = 60;

        public Dictionary<int, StackedBarHelper> IndexTotals = new Dictionary<int, StackedBarHelper>();
        protected override bool ScaleChanged => GetMax() != Max ||
                                                GetMin() != Min;

        private Point GetMax()
        {
            var s = Series.FirstOrDefault();
            if (s == null) return new Point(0,0);
            var p = new Point(
                Series.Select(x => x.PrimaryValues.Count).DefaultIfEmpty(0).Max(),
                s.PrimaryValues.Select((t, i) => Series.Sum(serie => serie.PrimaryValues[i]))
                    .Concat(new[] { double.MinValue }).Max());
            p.Y = PrimaryAxis.MaxValue ?? p.Y;
            return p;
        }

        private Point GetMin()
        {
            var s = Series.FirstOrDefault();
            if (s==null) return new Point(0,0);
            var p = new Point(.01,
                s.PrimaryValues.Select((t, i) => Series.Sum(serie => serie.PrimaryValues[i]))
                    .Concat(new[] { double.MaxValue }).Min());
            p.Y = PrimaryAxis.MinValue ?? p.Y;
            return p;
        }

        private Point GetS()
        {
            return new Point(
                SecondaryAxis.Separator.Step ?? CalculateSeparator(Max.X - Min.X, AxisTags.X),
                PrimaryAxis.Separator.Step ?? CalculateSeparator(Max.Y - Min.Y, AxisTags.Y));
        }

        protected override void Scale()
        {
            var fSerie = Series.FirstOrDefault();
            if (fSerie == null) return;
            for (var i = 0; i < fSerie.PrimaryValues.Count; i++)
            {
                var helper = new StackedBarHelper();
                var sum = 0d;
                for (int index = 0; index < Series.Count; index++)
                {
                    var serie = Series[index];
                    helper.Stacked[index] = new StackedItem
                    {
                        Value = serie.PrimaryValues[i],
                        Stacked = sum
                    };
                    sum += serie.PrimaryValues[i];
                }
                helper.Total = sum;
                IndexTotals[i] = helper;
            }

            Max = GetMax();
            Min = GetMin();
            S = GetS();

            Max.Y = PrimaryAxis.MaxValue ?? (Math.Truncate(Max.Y / S.Y) + 1) * S.Y;
            Min.Y = PrimaryAxis.MinValue ?? (Math.Truncate(Min.Y / S.Y) - 1) * S.Y;

            var unitW = ToPlotArea(1, AxisTags.X) - PlotArea.X + 5;
            LabelOffset = unitW / 2;

            DrawAxis();
        }

        protected override Point GetToolTipPosition(HoverableShape sender, List<HoverableShape> sibilings, Border b)
        {
            var unitW = ToPlotArea(1, AxisTags.X) - PlotArea.X + 5;
            var overflow = unitW - MaxColumnWidth > 0 ? unitW - MaxColumnWidth : 0;
            unitW = unitW > MaxColumnWidth ? MaxColumnWidth : unitW;
            var x = sender.Value.X + 1 > (Min.X + Max.X) / 2
                ? ToPlotArea(sender.Value.X, AxisTags.X) + overflow * .5 - b.DesiredSize.Width
                : ToPlotArea(sender.Value.X, AxisTags.X) + unitW + overflow * .5;
            var y = ActualHeight * .5 - b.DesiredSize.Height * .5;
            return new Point(x, y);
        }

        protected override void DrawAxis()
        {
            ConfigureSmartAxis(SecondaryAxis);
            base.DrawAxis();
        }
    }

    public class StackedBarHelper
    {
        public StackedBarHelper()
        {
            Stacked = new Dictionary<int, StackedItem>();
        }
        public double Total { get; set; }
        public Dictionary<int, StackedItem> Stacked { get; set; }
    }

    public class StackedItem
    {
        public double Value { get; set; }
        public double Stacked { get; set; }
    }
}
