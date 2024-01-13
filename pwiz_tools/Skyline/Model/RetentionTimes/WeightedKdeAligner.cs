using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using pwiz.Skyline.Util;
using ZedGraph;

namespace pwiz.Skyline.Model.RetentionTimes
{
    public class WeightedKdeAligner
    {
        private readonly int _resolution;
        
        private int _maxXi;
        private int _minXi;
        private int _maxYi;
        private int _minYi;
        private int[] _consolidateXY;
        private double[][] _histogram;

        public WeightedKdeAligner(int resolution)
        {
            _resolution = resolution;
        }
        
        public int Resolution
        {
            get { return _resolution; }
        }

        public double[][] Train(IList<PointPair> points, CancellationToken cancellationToken)
        {
            var weights = new Statistics(points.Select(pt => pt.Z));
            var statsX = new Statistics(points.Select(pt => pt.X));
            var statsY = new Statistics(points.Select(pt => pt.Y));
            var stdDevX = statsX.StdDev(weights);
            var stdDevY = statsY.StdDev(weights);
            var bandWidth = Math.Pow(_resolution, 1.0 / 6) * (stdDevX + stdDevY) / 2;
            var stdDev = (float) Math.Min(_resolution / 80.0, bandWidth / 2.3548);
            var histogram = Enumerable.Range(0, _resolution).Select(i => new double[_resolution]).ToArray();
            var cosineGaussian = new CosineGaussian(stdDev);
            foreach (var point in points)
            {
                StampOutHistogram(histogram, cosineGaussian, point.X, point.Y, point.Z);
            }
            int bestXi = -1;
            int bestYi = -1;
            double best = double.MinValue;
            for (int x = 0; x < _resolution; x++)
            {
                for (int y = 0; y < _resolution; y++)
                {
                    double val = histogram[x][y];
                    if (val > best)
                    {
                        best = val;
                        bestXi = x;
                        bestYi = y;
                    }
                }
            }
            var resultPoints = new LinkedList<Tuple<int, int>>();
            resultPoints.AddFirst(Tuple.Create(bestXi, bestYi));
            TraceNorthEast(histogram, bestXi, bestYi, resultPoints);
            TraceSouthWest(histogram, bestXi, bestYi, resultPoints);
            _consolidateXY = GetConsolidatedXY(resultPoints);
            _histogram = histogram;
            return _histogram;
        }

        public double[][] Histogram
        {
            get
            {
                return _histogram;
            }
        }

        private void StampOutHistogram(double[][] histogram, CosineGaussian cosineGaussian, double xCenter, double yCenter, double intensity)
        {
            int xStart = Math.Max(0, (int)Math.Floor(xCenter - cosineGaussian.Stdev * 2));
            int xEnd = Math.Min(_resolution - 1, (int)Math.Ceiling(xCenter + cosineGaussian.Stdev * 2));
            int yStart = Math.Max(0, (int)Math.Floor(yCenter - cosineGaussian.Stdev * 2));
            int yEnd = Math.Min(_resolution - 1, (int)Math.Ceiling(yCenter + cosineGaussian.Stdev * 2));
            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    double value = intensity * cosineGaussian.GetDensity((float)(x - xCenter), (float)(y - yCenter));
                    histogram[x][y] += value;
                }
            }
        }

        public static IEnumerable<PointPair> NormalizePoints(IList<PointPair> points, int resolution)
        {
            var xMin = points.Min(pt => pt.X);
            var dx = points.Max(pt => pt.X) - xMin;
            var yMin = points.Min(pt => pt.Y);
            var dy = points.Max(pt=> pt.Y)-yMin;
            return points.Select(pt => new PointPair(resolution * (pt.X - xMin) / dx, resolution * (pt.Y - yMin) / dy, pt.Z));
        }

        private void TraceNorthEast(double[][] histogram, int bestXi, int bestYi, LinkedList<Tuple<int, int>> points)
        {
            _maxXi = bestXi;
            _maxYi = bestYi;
            int xi = bestXi;
            int yi = bestYi;
            while (true)
            {
                var north = yi + 1 < _resolution ? histogram[xi][yi + 1] : -1;
                var east = xi + 1 < _resolution ? histogram[xi + 1][yi] : -1;
                var northeast = north != -1 && east != -1 ? histogram[xi + 1][yi + 1] : -1;
                var max = Math.Max(north, Math.Max(east, northeast));
                if (max == -1)
                    break;
                if (northeast == max || east == north)
                {
                    xi++;
                    yi++;
                    _maxXi++;
                    _maxYi++;
                }
                else if (east == max)
                {
                    xi++;
                    _maxXi++;
                }
                else if (north == max)
                {
                    yi++;
                    _maxYi++;
                }
                points.AddLast(Tuple.Create(xi, yi));
            }
        }

        private void TraceSouthWest(double[][] histogram, int bestXi, int bestYi, LinkedList<Tuple<int, int>> points)
        {
            _minXi = bestXi;
            _minYi = bestYi;
            var xi = bestXi;
            var yi = bestYi;
            while (true)
            {
                var south = yi - 1 >= 0 ? histogram[xi][yi - 1] : -1;
                var west = xi - 1 >= 0 ? histogram[xi - 1][yi] : -1;
                var southwest = south != -1 && west != -1 ? histogram[xi - 1][yi - 1] : -1;
                var max = Math.Max(south, Math.Max(west, southwest));
                if (max == -1)
                    break;
                if (southwest == max || south == west)
                {
                    xi--;
                    yi--;
                    _minXi--;
                    _minYi--;
                }
                else if (south == max)
                {
                    yi--;
                    _minYi--;
                }
                else
                {
                    xi--;
                    _minXi--;
                }
                points.AddFirst(Tuple.Create(xi, yi));
            }
        }

        private int[] GetConsolidatedXY(ICollection<Tuple<int, int>> points)
        {
            var consolidatedXY = new int[_resolution];
            using (var iterator = points.GetEnumerator())
            {
                bool notFinished = iterator.MoveNext();
                while (notFinished && iterator.Current != null)
                {
                    var minY = iterator.Current.Item2;
                    var maxY = minY;
                    var x = iterator.Current.Item1;
                    var lastX = x;
                    while ((notFinished = iterator.MoveNext()) && iterator.Current != null)
                    {
                        if (iterator.Current.Item1 != lastX)
                            break;
                        maxY = iterator.Current.Item2;
                    }
                    consolidatedXY[x] = (int)Math.Round((minY + maxY) / 2.0);
                }
            }

            return consolidatedXY;
        }

        public int GetYValue(int x)
        {
            return _consolidateXY[x];
        }
    }
}
