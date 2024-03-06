using System;
using System.Collections.Generic;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.RetentionTimes
{
    public class PiecewiseLinearRegression
    {
        private static readonly ImmutableList<double> ZERO_ONE = ImmutableList.ValueOf(new double[] { 0, 1 });
        public static PiecewiseLinearRegression FromSlopeAndIntercept(double slope, double intercept)
        {
            return new PiecewiseLinearRegression(ZERO_ONE, new[] { intercept, intercept + slope });
        }
        public PiecewiseLinearRegression(IEnumerable<double> xValues, IEnumerable<double> yValues)
        {
            XValues = ImmutableList.ValueOf(xValues);
            YValues = ImmutableList.ValueOf(yValues);
        }
        
        public ImmutableList<double> XValues { get; }
        public ImmutableList<double> YValues { get; }

        public double GetY(double x)
        {
            return Interpolate(x, XValues, YValues);
        }

        public double GetX(double y)
        {
            return Interpolate(y, YValues, XValues);
        }

        private static double Interpolate(double x, IList<double> xList, IList<double> yList)
        {
            if (xList.Count == 0)
            {
                return x;
            }

            if (xList.Count == 1)
            {
                return yList[0];
            }

            int index;
            if (x < xList[0])
            {
                index = 1;
            }
            else if (x > xList[xList.Count - 1])
            {
                index = xList.Count - 1;
            }
            else
            {
                index = CollectionUtil.BinarySearch(xList, x);
                if (index >= 0)
                {
                    return yList[index];
                }

                index = ~index;
                index = Math.Max(1, Math.Min(xList.Count - 1, index));
            }
            double xPrev = xList[index - 1];
            double xNext = xList[index];
            double yPrev = yList[index - 1];
            double yNext = yList[index];
            return yPrev + (x - xPrev) / (xNext - xPrev) * (yNext - yPrev);
        }

        protected bool Equals(PiecewiseLinearRegression other)
        {
            return XValues.Equals(other.XValues) && YValues.Equals(other.YValues);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PiecewiseLinearRegression)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (XValues.GetHashCode() * 397) ^ YValues.GetHashCode();
            }
        }

        public double? Slope
        {
            get
            {
                if (XValues.Count == 2)
                {
                    return (YValues[1] - YValues[0]) / (XValues[1] - XValues[0]);
                }

                return null;
            }
        }

        public double Intercept
        {
            get
            {
                return GetY(0);
            }
        }
    }
}
