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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using LiveCharts.Charts;

namespace LiveCharts.Series
{
    public class ScatterSerie : Serie
    {
        //TODO I think this could be better, strongly typing points.
        //problem is that i attach a watcher to PrimaryValues in Chart class
        public override ObservableCollection<double> PrimaryValues { get; set; }
        public double[] SecondaryValues { get; set; }

        public override void Plot(bool animate = true)
        {
            var points = new List<Point>();
            for (int index = 0; index < PrimaryValues.Count; index++)
            {
                var value = new Point(SecondaryValues[index], PrimaryValues[index]);
                var point = ToPlotArea(value);
                points.Add(point);

                var e = new Ellipse
                {
                    Width = PointRadius*2,
                    Height = PointRadius*2,
                    Fill = new SolidColorBrush {Color = Color},
                    Stroke = new SolidColorBrush {Color = Chart.PointHoverColor},
                    StrokeThickness = 2
                };

                Panel.SetZIndex(e, int.MaxValue-2);
                Canvas.SetLeft(e, point.X - e.Width*.5);
                Canvas.SetTop(e, point.Y - e.Height*.5);
                Chart.Canvas.Children.Add(e);

                if (Chart.Hoverable)
                {
                    var r = new Rectangle
                    {
                        Fill = Brushes.Transparent,
                        Width = 40,
                        Height = 40,
                        StrokeThickness = 0
                    };

                    r.MouseEnter += Chart.OnDataMouseEnter;
                    r.MouseLeave += Chart.OnDataMouseLeave;

                    Canvas.SetLeft(r, point.X - r.Width/2);
                    Canvas.SetTop(r, point.Y - r.Height/2);
                    Panel.SetZIndex(r, int.MaxValue);

                    Chart.Canvas.Children.Add(r);

                    Chart.HoverableShapes.Add(new HoverableShape
                    {
                        Serie = this,
                        Shape = r,
                        Target = e,
                        Value = value
                    });
                }
                Shapes.Add(e);
            }

            var c = Chart as ScatterChart;
            if (c == null) return;

            if (c.LineType == LineChartLineType.Bezier) 
                Shapes.AddRange(_addSerieAsBezier(points.ToArray(), Color, StrokeThickness, animate));
            if (c.LineType == LineChartLineType.Polyline)
                Shapes.AddRange(_addSeriesAsPolyline(points.ToArray(), Color, StrokeThickness, animate));
        }

        private IEnumerable<Shape> _addSerieAsBezier(Point[] points, Color color, double storkeThickness,
            bool animate = true)
        {
            var addedFigures = new List<Shape>();

            Point[] cp1, cp2;
            BezierSpline.GetCurveControlPoints(points, out cp1, out cp2);

            var lines = new PathSegmentCollection();
            var l = 0d;
            for (var i = 0; i < cp1.Length; ++i)
            {
                lines.Add(new BezierSegment(cp1[i], cp2[i], points[i + 1], true));
                //it would be awesome to use a better formula to calculate bezier lenght
                l += Math.Sqrt(
                    Math.Pow(Math.Abs(cp1[i].X - cp2[i].X), 2) +
                    Math.Pow(Math.Abs(cp1[i].Y - cp2[i].Y), 2));
                l += Math.Sqrt(
                    Math.Pow(Math.Abs(cp2[i].X - points[i + 1].X), 2) +
                    Math.Pow(Math.Abs(cp2[i].Y - points[i + 1].Y), 2));
            }
            //aprox factor, it was calculated by aproximation.
            //the more line is curved, the more it fails.
            l = l * .65;
            var f = new PathFigure(points[0], lines, false);
            var g = new PathGeometry(new[] { f });

            var path = new Path
            {
                Stroke = new SolidColorBrush { Color = color },
                StrokeThickness = storkeThickness,
                Data = g,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeDashOffset = l,
                StrokeDashArray = new DoubleCollection { l, l },
                ClipToBounds = true
            };

            Chart.Canvas.Children.Add(path);
            addedFigures.Add(path);

            var draw = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = TimeSpan.FromSeconds(0),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new SplineDoubleKeyFrame
                    {
                        KeyTime = TimeSpan.FromMilliseconds(1),
                        Value = l
                    },
                    new SplineDoubleKeyFrame
                    {
                        KeyTime = TimeSpan.FromMilliseconds(750),
                        Value = 0
                    }
                }
            };

            Storyboard.SetTarget(draw, path);
            Storyboard.SetTargetProperty(draw, new PropertyPath(Shape.StrokeDashOffsetProperty));
            var sbDraw = new Storyboard();
            sbDraw.Children.Add(draw);
            var animated = false;
            if (!Chart.DisableAnimation)
            {
                if (animate)
                {
                    sbDraw.Begin();
                    animated = true;
                }
            }
            if (!animated) path.StrokeDashOffset = 0;
            return addedFigures;
        }

        private IEnumerable<Shape> _addSeriesAsPolyline(Point[] points, Color color, double storkeThickness,
            bool animate = true)
        {
            var addedFigures = new List<Shape>();

            var l = 0d;
            for (var i = 1; i < points.Length; i++)
            {
                var p1 = points[i - 1];
                var p2 = points[i];
                l += Math.Sqrt(
                    Math.Pow(Math.Abs(p1.X - p2.X), 2) +
                    Math.Pow(Math.Abs(p1.Y - p2.Y), 2)
                    );
            }

            var f = points.First();
            var p = points.Where(x => x != f);
            var g = new PathGeometry
            {
                Figures = new PathFigureCollection(new List<PathFigure>
                {
                    new PathFigure
                    {
                        StartPoint = f,
                        Segments = new PathSegmentCollection(
                            p.Select(x => new LineSegment {Point = new Point(x.X, x.Y)}).Count())
                    }
                })
            };

            var path = new Path
            {
                Stroke = new SolidColorBrush { Color = color },
                StrokeThickness = storkeThickness,
                Data = g,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeDashOffset = l,
                StrokeDashArray = new DoubleCollection { l, l },
                ClipToBounds = true
            };

            var sp = points.ToList();
            sp.Add(new Point(points.Max(x => x.X), ToPlotArea(Chart.Min.Y, AxisTags.Y)));

            Chart.Canvas.Children.Add(path);
            addedFigures.Add(path);

            var draw = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = TimeSpan.FromSeconds(0),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new SplineDoubleKeyFrame
                    {
                        KeyTime = TimeSpan.FromMilliseconds(1),
                        Value = l
                    },
                    new SplineDoubleKeyFrame
                    {
                        KeyTime = TimeSpan.FromMilliseconds(750),
                        Value = 0
                    }
                }
            };
            Storyboard.SetTarget(draw, path);
            Storyboard.SetTargetProperty(draw, new PropertyPath(Shape.StrokeDashOffsetProperty));
            var sbDraw = new Storyboard();
            sbDraw.Children.Add(draw);
            var animated = false;
            if (!Chart.DisableAnimation)
            {
                if (animate)
                {
                    sbDraw.Begin();
                    animated = true;
                }
            }
            if (!animated) path.StrokeDashOffset = 0;
            return addedFigures;
        }
    }
}
