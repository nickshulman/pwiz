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
using pwiz.Common.DataBinding;
using pwiz.Common.Spectra;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Alignment;
using ZedGraph;

namespace pwiz.Skyline.Model.Results.Spectra
{
    public class SpectrumMetadataList
    {
        private readonly Array[] _columnValues;
        private readonly ImmutableList<int> _ms1SpectrumIndices;
        private readonly ImmutableSortedList<double, ImmutableList<int>> _spectrumIndicesByPrecursor;
        private readonly Dictionary<string, int> _columnIndices;
        public SpectrumMetadataList(IEnumerable<DigestedSpectrumMetadata> spectrumMetadatas, IEnumerable<SpectrumClassColumn> columns)
        {
            AllSpectra = ImmutableList.ValueOfOrEmpty(spectrumMetadatas);
            Columns = ImmutableList.ValueOf(columns);
            _columnIndices = Enumerable.Range(0, Columns.Count).ToDictionary(i => Columns[i].ColumnName);
            var valueCache = new ValueCache();
            _columnValues = Columns.Select(col => GetColumnValues(valueCache, col, AllSpectra)).ToArray();

            _ms1SpectrumIndices = ImmutableList.ValueOf(Enumerable.Range(0, AllSpectra.Count).Where(i => AllSpectra[i].SpectrumMetadata.MsLevel == 1));
            var spectraByPrecursor = new List<KeyValuePair<double, ImmutableList<int>>>();
            var precursorGroups = AllRows
                .SelectMany(row=> row.SpectrumMetadata.SpectrumMetadata.GetPrecursors(1).Select(p => Tuple.Create(p.PrecursorMz, row)))
                .GroupBy(tuple => tuple.Item1, tuple => tuple.Item2.RowIndex);
            foreach (var group in precursorGroups)
            {
                spectraByPrecursor.Add(new KeyValuePair<double, ImmutableList<int>>(group.Key.RawValue, ImmutableList.ValueOf(group)));
            }

            _spectrumIndicesByPrecursor = ImmutableSortedList.FromValues(spectraByPrecursor);
        }

        public static SpectrumMetadataList Ms2Only(IEnumerable<DigestedSpectrumMetadata> spectrumMetadatas, IEnumerable<SpectrumClassColumn> columns)
        {
            return new SpectrumMetadataList(spectrumMetadatas.Where(s => s.SpectrumMetadata.MsLevel > 1), columns);
        }

        public ImmutableList<SpectrumClassColumn> Columns { get; }

        public int IndexOfColumn(SpectrumClassColumn column)
        {
            if (_columnIndices.TryGetValue(column.ColumnName, out int index))
            {
                return index;
            }

            return -1;
        }

        public int IndexOfColumn(PropertyPath propertyPath)
        {
            if (!propertyPath.IsProperty || true != propertyPath.Parent?.IsRoot)
            {
                return -1;
            }
            if (_columnIndices.TryGetValue(propertyPath.Name, out int index))
            {
                return index;
            }

            return -1;
        }

        public ImmutableList<DigestedSpectrumMetadata> AllSpectra { get; private set; }

        public IList<Row> AllRows
        {
            get
            {
                return ReadOnlyList.Create(AllSpectra.Count, GetRow);
            }
        }

        public Row GetRow(int index)
        {
            return new Row(this, index);
        }

        public IEnumerable<Row> Ms1Spectra
        {
            get { return _ms1SpectrumIndices.Select(GetRow); }
        }

        public IEnumerable<KeyValuePair<double, IEnumerable<Row>>> SpectraByPrecursor
        {
            get
            {
                return _spectrumIndicesByPrecursor.Select(entry =>
                    new KeyValuePair<double, IEnumerable<Row>>(entry.Key, entry.Value.Select(GetRow)));
            }
        }

        public IEnumerable<object> GetColumnValues(int columnIndex, IEnumerable<Row> rows)
        {
            return rows.Select(row => _columnValues[columnIndex].GetValue(row.RowIndex));
        }
        public struct Row
        {
            public Row(SpectrumMetadataList spectrumMetadataList, int index)
            {
                SpectrumMetadataList = spectrumMetadataList;
                RowIndex = index;
            }

            public SpectrumMetadataList SpectrumMetadataList { get; }
            public int RowIndex { get; }
            public DigestedSpectrumMetadata SpectrumMetadata
            {
                get { return SpectrumMetadataList.AllSpectra[RowIndex]; }
            }

            public IEnumerable<KeyValuePair<SpectrumClassColumn, object>> ColumnValuePairs
            {
                get
                {
                    return SpectrumMetadataList.Columns.Zip(SpectrumMetadataList._columnValues,
                        (column, value) => new KeyValuePair<SpectrumClassColumn, object>(column, value));
                }
            }

            public object GetColumnValue(int columnIndex)
            {
                return SpectrumMetadataList._columnValues[columnIndex].GetValue(RowIndex);
            }
        }

        public static Array GetColumnValues(ValueCache valueCache, SpectrumClassColumn column, IList<DigestedSpectrumMetadata> spectrumMetadatas)
        {
            bool isValueType = column.ValueType.IsValueType;
            var array = Array.CreateInstance(column.ValueType, spectrumMetadatas.Count);
            for (int i = 0; i < spectrumMetadatas.Count; i++)
            {
                var value = column.GetValue(spectrumMetadatas[i].SpectrumMetadata);
                if (!isValueType)
                {
                    value = valueCache.CacheValue(value);
                }
                array.SetValue(value, i);
            }

            return array;
        }

        public IEnumerable<KeyValuePair<int, double>> GetSimilarityVector(DigestedSpectrumMetadata spectrum)
        {
            for (int i = 0; i < AllSpectra.Count; i++)
            {
                if (!AreCompatible(spectrum.SpectrumMetadata, AllSpectra[i].SpectrumMetadata))
                {
                    continue;
                }

                var similarity = spectrum.Digest.SimilarityScore(AllSpectra[i].Digest);
                if (similarity.HasValue)
                {
                    yield return new KeyValuePair<int, double>(i, similarity.Value);
                }
            }
        }

        public SimilarityMatrix GetSimilarityMatrix(IProgressMonitor progressMonitor, IProgressStatus status, SpectrumMetadataList that)
        {
            int completedCount = 0;
            var lists = new List<PointPair>[that.AllSpectra.Count];
            ParallelEx.For(0, that.AllSpectra.Count, i =>
            {
                var list = new List<PointPair>();
                var thatSpectrum = that.AllSpectra[i];
                var y = thatSpectrum.SpectrumMetadata.RetentionTime;
                foreach (var entry in GetSimilarityVector(thatSpectrum))
                {
                    if (progressMonitor.IsCanceled)
                    {
                        break;
                    }
                    list.Add(new PointPair(AllSpectra[entry.Key].RetentionTime, y, entry.Value));
                }

                lists[i] = list;
                lock (lists)
                {
                    completedCount++;
                    status = status.ChangePercentComplete(completedCount * 100 / lists.Length);
                    progressMonitor.UpdateProgress(status);
                }
            });
            return new SimilarityMatrix(lists.SelectMany(list => list));
        }

        public static bool AreCompatible(SpectrumMetadata metadata1, SpectrumMetadata metadata2)
        {
            return metadata1.GetPrecursors(0).Equals(metadata2.GetPrecursors(0));
        }
    }
}
