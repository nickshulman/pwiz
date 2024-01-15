﻿/*
 * Original author: Max Horowitz-Gelb <maxhg .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2016 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MathNet.Numerics.Statistics;
using ZedGraph;

namespace pwiz.Skyline.Model.RetentionTimes
{
    public class CosineGaussian
    {
        private readonly float _stdev;
        private readonly float _A;
        private readonly float _M;
        private readonly float _min;
        private readonly float _max;
        public CosineGaussian(float stdev)
        {
            _stdev = stdev;
            _A = (float)(Math.PI/(8.0*stdev));
            _M = (float)(Math.PI/(4.0*stdev));
            _min = -2*stdev;
            _max = 2*stdev;
        }

        public float GetDensity(float x, float y)
        {
            return (float) GetDensity(Math.Sqrt(x*x + y*y));
        }

        public double GetDensity(double delta)
        {
            if (delta > _max || delta < _min)
            {
                return 0;
            }

            return _A * Math.Cos(-delta * _M);
        }

        public double Stdev { get { return _stdev;} }
    }

    public class KdeAligner : Aligner
    {
        private readonly int _resolution;
        private double _minX;
        private double _maxX;
        private double _minY;
        private double _maxY;
        private int _maxXi;
        private int _minXi;
        private int _maxYi;
        private int _minYi;
        private int[] _consolidatedXY;
        private int[] _consolidatedYX;
        private double _rmsd;
        private double[] _smoothedY;
        private double[] _xArr;
        private double _stretchFactor;

        public KdeAligner(int origXFileIndex, int origYFileIndex, int resolution = 1000) : base(origXFileIndex, origYFileIndex)
        {
            _resolution = resolution;
        }

        /// <summary>
        /// Constructs a KdeAligner with a specific resolution and stretchFactor
        /// </summary>
        /// <param name="resolution">The number of points in the X and Y axes </param>
        /// <param name="stretchFactor">The amount that the CosineGaussian stamp is stretched along the Y=X axis</param>
        public KdeAligner(int resolution = 1000, double stretchFactor = 1) 
        {
            _resolution = resolution;
            _stretchFactor = stretchFactor;
        }
        
        public int Resolution
        {
            get { return _resolution; }
        }

        public double GetScaledX(int coordinate)
        {
            return _minX + coordinate * (_maxX - _minX) / _resolution;
        }
        public int GetXCoordinate(double x)
        {
            return (int)Math.Round(_resolution * (x - _minX) / (_maxX - _minX));
        }

        public double GetScaledY(int coordinate)
        {
            return _minY + coordinate * (_maxY - _minY) / _resolution;
        }

        public int GetYCoordinate(double y)
        {
            return (int)Math.Round(_resolution * (y - _minY) / (_maxY - _minY));
        }
        
        public override void Train(double[] xArr, double[] yArr, CancellationToken token)
        {
            Array.Sort(xArr, yArr);
            TrainWithWeights(xArr, yArr, Enumerable.Repeat(1.0, xArr.Length).ToArray(), token);
        }

        public float[,] TrainPoints(ICollection<PointPair> points, CancellationToken cancellationToken)
        {
            return TrainWithWeights(points.Select(pt => pt.X).ToArray(), points.Select(pt => pt.Y).ToArray(),
                points.Select(pt => pt.Z).ToArray(), cancellationToken);
        }
        
        public float[,] TrainWithWeights(double[] xArr, double[] yArr, double[] weights, CancellationToken cancellationToken)
        {
            _xArr = xArr;
            double[] xNormal;
            double[] yNormal;

            GetResolutionNormalizedPoints(xArr,yArr,out xNormal, out yNormal,
                out _minX, out _maxX, out _minY, out _maxY);

            var indStdev = xNormal.StandardDeviation();
            var depStdev = yNormal.StandardDeviation();

            var bandWidth = Math.Pow(xArr.Length, -1f/6f)*(indStdev + depStdev)/2.0f;

            var stdev = (float) Math.Min(_resolution/40f, bandWidth/2.3548);

            float[,] stamp = GetCosineGaussianStamp(new CosineGaussian(stdev), _stretchFactor);

            float[,] histogram = new float[_resolution, _resolution];

            for (int p = 0; p < xNormal.Length; p++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int x = (int)xNormal[p];
                int y = (int)yNormal[p];
                Stamp(histogram, stamp, x, y, (float) weights[p]);
            }

            int bestXi = -1;
            int bestYi = -1;
            double best = double.MinValue;
            for (int x = 0; x < _resolution; x ++)
            {
                for (int y = 0; y < _resolution; y ++)
                {
                    float val = histogram[x, y];
                    if (val > best)
                    {
                        best = val;
                        bestXi = x;
                        bestYi = y;
                    }

                }
            }
            var points = new LinkedList<Tuple<int, int>>();
            points.AddFirst(new Tuple<int, int>(bestXi, bestYi));
            TraceNorthEast(histogram, bestXi, bestYi, points);
            TraceSouthWest(histogram,bestXi,bestYi,points);
            _consolidatedXY = GetConsolidatedXY(points);
            if (CanCalculateReverseRegression)
            {
                _consolidatedYX = GetConsolidateYX(points);
            }
            _rmsd = 0;
            _smoothedY = new double[xArr.Length];
            for (int i = 0; i < xArr.Length; i++)
            {
                var x = xArr[i];
                var y = yArr[i];
                var pred = GetValue(x);
                _smoothedY[i] = pred;
                var diff = y - pred;
                _rmsd += diff * diff / xArr.Length;
            }
            _rmsd = Math.Sqrt(_rmsd);
            return histogram;
        }

        private int[] GetConsolidatedXY(ICollection<Tuple<int, int>> points)
        {
            var consolidatedXY = new int[_resolution];
            using var iterator = points.GetEnumerator();
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

            return consolidatedXY;
        }

        private int[] GetConsolidateYX(ICollection<Tuple<int, int>> points)
        {
            var consolidatedYX = new int[_resolution];
            using var iterator = points.GetEnumerator();
            bool notFinished = iterator.MoveNext();
            while (notFinished && iterator.Current != null)
            {
                var minX = iterator.Current.Item1;
                var maxX = minX;
                var y = iterator.Current.Item2;
                var lastY = y;
                while ((notFinished = iterator.MoveNext()) && iterator.Current != null)
                {
                    if (iterator.Current.Item2 != lastY)
                        break;
                    maxX = iterator.Current.Item1;
                }
                consolidatedYX[y] = (int)Math.Round((minX + maxX) / 2.0);
            }

            return consolidatedYX;
        }

        private void TraceNorthEast(float[,] histogram, int bestXi, int bestYi, LinkedList<Tuple<int, int>> points)
        {
            _maxXi = bestXi;
            _maxYi = bestYi;
            int xi = bestXi;
            int yi = bestYi;
            while (true)
            {
                var north = yi + 1 < histogram.GetLength(1) ? histogram[xi, yi + 1] : -1;
                var east = xi + 1 < histogram.GetLength(0) ? histogram[xi + 1, yi] : -1;
                var northeast = north != -1 && east != -1 ? histogram[xi + 1, yi + 1] : -1;
                var max = Math.Max(north, Math.Max(east, northeast));
                if (max == -1)
                    break;
                if (northeast == max || east == north)
                {
                    xi ++;
                    yi ++;
                    _maxXi ++;
                    _maxYi ++;
                }
                else if (east == max)
                {
                    xi ++;
                    _maxXi ++;
                }
                else if (north == max)
                {
                    yi ++;
                    _maxYi ++;
                }
                points.AddLast(new Tuple<int, int>(xi, yi));
            }
        }

        private void TraceSouthWest(float[,] histogram, int bestXi, int bestYi, LinkedList<Tuple<int, int>> points)
        {
            _minXi = bestXi;
            _minYi = bestYi;
            var xi = bestXi;
            var yi = bestYi;
            while (true)
            {
                var south = yi - 1 >= 0 ? histogram[xi, yi - 1] : -1;
                var west = xi - 1 >= 0 ? histogram[xi - 1, yi] : -1;
                var southwest = south != -1 && west != -1 ? histogram[xi - 1, yi - 1] : -1;
                var max = Math.Max(south, Math.Max(west, southwest));
                if (max == -1)
                    break;
                if (southwest == max || south == west)
                {
                    xi --;
                    yi --;
                    _minXi --;
                    _minYi --;
                }
                else if (south == max)
                {
                    yi --;
                    _minYi --;
                }
                else
                {
                    xi --;
                    _minXi --; 
                }
                points.AddFirst(new Tuple<int, int>(xi, yi));
            }
        }

        private void Stamp(float[,] histogram, float[,] stamp, int x, int y, float weight)
        {
            
            for (int i = x - stamp.GetLength(0)/2; i <= x + stamp.GetLength(0)/2; i++)
            {
                if(i < 0)
                    continue;
                if (i >= histogram.GetLength(0))
                    break;
                for (int j = y - stamp.GetLength(1)/2; j <= y + stamp.GetLength(1)/2; j++)
                {
                    if(j < 0)
                        continue;
                    if (j >= histogram.GetLength(1))
                        break;
                    histogram[i, j] += stamp[i - x + stamp.GetLength(0)/2, j - y + stamp.GetLength(1)/2] * weight;
                }
            }
        }

        public static float[,] GetCosineGaussianStamp(CosineGaussian cG, double stretchFactor = 1)
        {
            int stampRadius = (int) Math.Round(2.0f * cG.Stdev * stretchFactor);

            var stamp = new float[stampRadius*2+1, stampRadius*2+1];
            for (int i = 0; i < stampRadius*2 + 1; i++)
            {
                for (int j = 0; j < stampRadius*2 + 1; j++)
                {
                    double deltaX = i - stampRadius;
                    double deltaY = j - stampRadius;
                    double delta;
                    if (stretchFactor == 1)
                    {
                        delta = Math.Sqrt(deltaX + deltaY);
                    }
                    else
                    {
                        double xPrime = (deltaX + deltaY) / stretchFactor;
                        double yPrime = (deltaY - deltaX);
                        delta = Math.Sqrt(xPrime * xPrime + yPrime * yPrime) / 2;
                    }

                    stamp[i, j] = (float) cG.GetDensity(delta);
                }
            }
            return stamp;
        }

        private void GetResolutionNormalizedPoints(double[] indArr, double[] depArr, out double[] normalInd,
            out double[] normalDep, out double minInd, out double maxInd, out double minDep, out double maxDep)
        {
            minInd = double.MaxValue;
            maxInd = double.MinValue;
            minDep = double.MaxValue;
            maxDep = double.MinValue;
            foreach (var x in indArr)
            {
                minInd = Math.Min(minInd, x);
                maxInd = Math.Max(maxInd, x);
            }
            foreach (var y in depArr)
            {
                minDep = Math.Min(minDep, y);
                maxDep = Math.Max(maxDep, y);
            }

            normalInd = new double[indArr.Length];
            normalDep = new double[depArr.Length];
            for (int i = 0; i < normalDep.Length; i++)
            {
                normalInd[i] = IntegerInterpolate(indArr[i], minInd, maxInd, 0, _resolution - 1);
                normalDep[i] = IntegerInterpolate(depArr[i], minDep, maxDep, 0, _resolution - 1);
            }
        }

        private int IntegerInterpolate(double val, double minX, double maxX, int minY, int maxY)
        {
            return (int) Math.Round(Interpolate(val, minX, maxX, minY, maxY));
        }

        private double Interpolate(double val, double minX, double maxX, double minY, double maxY)
        {
            if (val < minX || val > maxX)
                return Double.NaN;
            return minY + (val - minX)/(maxX - minX)*(maxY - minY);
        }

        public override double GetValue(double x)
        {
            return _GetValueFor(x, _minX, _maxX, _minXi, _maxXi, _minY, _maxY, _minYi, _maxYi, _consolidatedXY);
        }

        public override double GetValueReversed(double y)
        {
            if(!CanCalculateReverseRegression)
                throw new Exception(@"KDE has not calculated reverse regression");
            return _GetValueFor(y, _minY, _maxY, _minYi, _maxYi, _minX, _maxX, _minXi, _maxXi, _consolidatedYX);
        }

        private double _GetValueFor(double ind, double minInd, double maxInd, int minIndNormal, int maxIndNormal,
            double minDep, double maxDep, int minDepNormal, int maxDepNormal, int[] arr)
        {
            int dep_i = -1;
            if (ind <= minInd)
                dep_i = minIndNormal;
            else if (ind >= maxInd)
                dep_i = maxIndNormal;
            else
            {
                int ind_i = IntegerInterpolate(ind, minInd, maxInd, minIndNormal, maxIndNormal);
                dep_i = arr[ind_i];
            }
            return Interpolate(dep_i, minDepNormal, maxDepNormal, minDep, maxDep);
        }

        public override double GetRmsd()
        {
            return _rmsd;    
        }

        public override void GetSmoothedValues(out double[] xArr, out double[] yArr)
        {
            xArr = _xArr;
            yArr = _smoothedY;
        }
    }
}
