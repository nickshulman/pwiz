﻿/*
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

using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
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
        public SimilarityMatrix(IEnumerable<SubMatrix> scoreMatrices)
        {
            ScoreMatrices = ImmutableList.ValueOf(scoreMatrices);
        }

        public ImmutableList<SubMatrix> ScoreMatrices{ get; }

        public IEnumerable<PointPair> Points
        {
            get
            {
                return ScoreMatrices.SelectMany(matrix => matrix.AsPoints);
            }
        }

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
        /// </summary>
        public IList<KeyValuePair<double, double>> FindBestPath()
        {
            return ScoreMatrices.SelectMany(matrix => matrix.GetBestPath()).ToList();
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

        public class SubMatrix
        {
            public SubMatrix(IEnumerable<double> xValues, IEnumerable<double> yValues,
                IEnumerable<IEnumerable<double>> scoreRows)
            {
                XValues = ImmutableList.ValueOf(xValues);
                YValues = ImmutableList.ValueOf(yValues);
                ScoreRows = ImmutableList.ValueOf(scoreRows.Select(ImmutableList.ValueOf));
            }

            public ImmutableList<double> XValues { get; }
            public ImmutableList<double> YValues { get; }
            public ImmutableList<ImmutableList<double>> ScoreRows { get; }

            public IEnumerable<KeyValuePair<double, double>> GetBestPath()
            {
                var excludedColumnIndices = new bool[XValues.Count];
                var excludedRowIndices = new bool[YValues.Count];
                var rowDatas = new RowData[ScoreRows.Count];
                var returnedRows = new List<KeyValuePair<double, double>>();
                ParallelEx.For(0, ScoreRows.Count, i =>
                {
                    var rowData = new RowData(ScoreRows[i]);
                    rowDatas[i] = rowData;
                });
                while (true)
                {
                    double? bestScore = null;
                    int bestRow = -1;
                    int bestCol = -1;
                    for (int iRow = 0; iRow < YValues.Count; iRow++)
                    {
                        if (excludedRowIndices[iRow])
                        {
                            continue;
                        }
                        var rowData = rowDatas[iRow];
                        while (rowData.CurrentScoreIndex < rowData.SortedScoreIndexes.Length && excludedColumnIndices[rowData.SortedScoreIndexes[rowData.CurrentScoreIndex]])
                        {
                            rowData.CurrentScoreIndex++;
                        }

                        if (rowData.CurrentScoreIndex >= rowData.SortedScoreIndexes.Length)
                        {
                            excludedRowIndices[iRow] = true;
                        }
                        else
                        {
                            int iCol = rowData.SortedScoreIndexes[rowData.CurrentScoreIndex];
                            var score = ScoreRows[iRow][iCol];
                            if (bestScore == null || score > bestScore)
                            {
                                bestScore = score;
                                bestRow = iRow;
                                bestCol = iCol;
                            }
                        }
                    }
                    if (bestScore == null)
                    {
                        return returnedRows;
                    }

                    excludedColumnIndices[bestCol] = true;
                    excludedRowIndices[bestRow] = true;
                    returnedRows.Add(new KeyValuePair<double, double>(XValues[bestCol], YValues[bestRow]));
                }
            }

            public IEnumerable<PointPair> AsPoints
            {
                get
                {
                    for (int iRow = 0; iRow < YValues.Count; iRow++)
                    {
                        var scoreRow = ScoreRows[iRow];
                        var yValue = YValues[iRow];
                        for (int iCol = 0; iCol < XValues.Count; iCol++)
                        {
                            yield return new PointPair(XValues[iCol], yValue, scoreRow[iRow]);
                        }
                    }
                }
            }

            private class RowData
            {
                public RowData(IList<double> rowScores)
                {
                    SortedScoreIndexes = Enumerable.Range(0, rowScores.Count).OrderByDescending(i => rowScores[i])
                        .ToArray();
                }
                public int[] SortedScoreIndexes { get; }
                public int CurrentScoreIndex { get; set; }
            }
        }
    }
}
