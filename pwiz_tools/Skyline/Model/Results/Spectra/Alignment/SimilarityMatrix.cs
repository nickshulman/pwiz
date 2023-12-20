using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using ZedGraph;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    public class SimilarityMatrix
    {
        public SimilarityMatrix(IEnumerable<PointPair> points)
        {
            Points = ImmutableList.ValueOf(points.OrderByDescending(point => point.Z));
        }

        /// <summary>
        /// List of points sorted descending by Z-value.
        /// </summary>
        public ImmutableList<PointPair> Points { get; }

        /// <summary>
        /// Returns points with the highest Z-coordinate values.
        /// The points will have unique X-values and unique Y-values.
        /// <param name="onlyIncludeBestPoints">If true, the returned points will all have the highest Z-value among all points with the same X-coordinate and all points with the same Y-coordinate</param>
        /// </summary>
        public IEnumerable<PointPair> FindBestPath(bool onlyIncludeBestPoints)
        {
            var xValues = new HashSet<double>();
            var yValues = new HashSet<double>();
            foreach (var point in Points)
            {
                if (onlyIncludeBestPoints)
                {
                    if (!xValues.Add(point.X) | !yValues.Add(point.Y))
                    {
                        continue;
                    }
                }
                else
                {
                    if (xValues.Contains(point.X) || yValues.Contains(point.Y))
                    {
                        continue;
                    }

                    xValues.Add(point.X);
                    yValues.Add(point.Y);
                }

                yield return new PointPair(point.X, point.Y);
            }
        }
    }
}
