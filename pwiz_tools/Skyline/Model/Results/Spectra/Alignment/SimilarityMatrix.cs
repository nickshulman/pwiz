/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2023 University of Washington - Seattle, WA
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
using pwiz.Common.Collections;
using pwiz.Skyline.Util.Extensions;
using ZedGraph;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    /// <summary>
    /// Holds a sparse matrix of values representing the result of comparing
    /// elements pairwise from two lists.
    /// The results of these comparisons are represented by (x, y, z) coordinates
    /// where x and y are the positions in the two lists, and z is the result of the
    /// comparison, where a higher z value means that the elements at positions x and y
    /// were more similar to each other.
    /// </summary>
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

        public IEnumerable<(double x, double y, double z)> AsTuples
        {
            get
            {
                return Points.Select(pt => (pt.X, pt.Y, pt.Z));
            }
        }

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

        public string ToTsv()
        {
            var lines = new List<string>();
            var xValues = Points.Select(pt => pt.X).Distinct().OrderBy(x => x).ToList();
            lines.Add(string.Join(TextUtil.SEPARATOR_TSV_STR, xValues.Cast<object>().Prepend(string.Empty)));
            foreach (var row in Points.GroupBy(pt => pt.Y).OrderBy(group => group.Key))
            {
                var values = new List<double?> { row.Key };
                var dict = row.ToDictionary(pt => pt.X);
                foreach (var x in xValues)
                {
                    dict.TryGetValue(x, out var value);
                    values.Add(value?.Z);
                }
                lines.Add(string.Join(TextUtil.SEPARATOR_TSV_STR, values));
            }

            return TextUtil.LineSeparate(lines);
        }
    }
}
