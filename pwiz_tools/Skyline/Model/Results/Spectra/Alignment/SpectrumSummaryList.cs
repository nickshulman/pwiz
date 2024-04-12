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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Criterion;
using pwiz.Common.Collections;
using pwiz.Common.Spectra;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    public class SpectrumSummaryList : IReadOnlyList<SpectrumSummary>
    {
        private ImmutableList<SpectrumSummary> _summaries;

        public SpectrumSummaryList(IEnumerable<SpectrumSummary> summaries)
        {
            _summaries = ImmutableList.ValueOfOrEmpty(summaries);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<SpectrumSummary> GetEnumerator()
        {
            return _summaries.GetEnumerator();
        }

        public int Count => _summaries.Count;

        public SpectrumSummary this[int index] => _summaries[index];

        private static DigestKey GetSpectrumDigestKey(
            SpectrumSummary spectrumSummary)
        {
            if (spectrumSummary.SummaryValueLength == 0 || spectrumSummary.SummaryValue.All(v=>0 == v))
            {
                return null;
            }

            if (!spectrumSummary.SpectrumMetadata.ScanWindowLowerLimit.HasValue ||
                !spectrumSummary.SpectrumMetadata.ScanWindowUpperLimit.HasValue)
            {
                return null;
            }

            var precursorsByMsLevel = ImmutableList.ValueOf(Enumerable
                .Range(1, spectrumSummary.SpectrumMetadata.MsLevel - 1)
                .Select(level => spectrumSummary.SpectrumMetadata.GetPrecursors(level)));
            return new DigestKey(spectrumSummary.SpectrumMetadata.ScanWindowLowerLimit.Value,
                spectrumSummary.SpectrumMetadata.ScanWindowUpperLimit.Value, spectrumSummary.SummaryValueLength,
                precursorsByMsLevel);
        }

        /// <summary>
        /// Minimum number of a spectra with the same key (i.e. same MS Level and Precursor)
        /// to do an alignment between. (That is, it does not make sense to try to align to
        /// DDA spectra which happen to have the same precursor m/z-- the same precursor
        /// needs to have been sampled several times)
        /// </summary>
        private const int MIN_SPECTRA_FOR_ALIGNMENT = 20;

        public SimilarityMatrix GetSimilarityMatrix(
            IProgressMonitor progressMonitor,
            IProgressStatus status,
            IEnumerable<SpectrumSummary> spectrumSummaries)
        {
            var thatByDigestKey = spectrumSummaries.GroupBy(GetSpectrumDigestKey)
                .Where(grouping => null != grouping.Key).ToDictionary(grouping => grouping.Key, ImmutableList.ValueOf);
            var myIndicesByDigestKey = Enumerable.Range(0, Count).GroupBy(i => GetSpectrumDigestKey(this[i]))
                .Where(grouping => null != grouping.Key).ToDictionary(grouping => grouping.Key, ImmutableList.ValueOf);
            var scoreLists = new List<double>[Count];
            int completedCount = 0;
            ParallelEx.For(0, Count, index =>
            {
                var spectrum = this[index];
                var key = GetSpectrumDigestKey(spectrum);
                if (key != null && myIndicesByDigestKey[key].Count > MIN_SPECTRA_FOR_ALIGNMENT && thatByDigestKey[key].Count > MIN_SPECTRA_FOR_ALIGNMENT)
                {
                    var scores = new List<double>();
                    foreach (var otherSpectrum in thatByDigestKey[key])
                    {
                        if (true == progressMonitor?.IsCanceled)
                        {
                            break;
                        }

                        scores.Add(spectrum.SimilarityScore(otherSpectrum) ?? double.MinValue);
                    }

                    scoreLists[index] = scores;
                }

                if (progressMonitor != null)
                {
                    lock (progressMonitor)
                    {
                        completedCount++;
                        int progressValue = completedCount * 100 / Count;
                        progressMonitor.UpdateProgress(status = status.ChangePercentComplete(progressValue));
                    }
                }
            });
            var scoreMatrices = new List<SimilarityMatrix.SubMatrix>();
            foreach (var entry in myIndicesByDigestKey)
            {
                var scoreColumns = entry.Value.Select(i => scoreLists[i]).ToList();
                var xValues = ImmutableList.ValueOf(entry.Value.Select(i => this[i].RetentionTime));
                var yValues = ImmutableList.ValueOf(thatByDigestKey[entry.Key].Select(spectrum => spectrum.RetentionTime));
                var scoreRows = new List<IEnumerable<double>>();
                for (int iRow = 0; iRow < yValues.Count; iRow++)
                {
                    scoreRows.Add(scoreColumns.Select(col => col[iRow]).ToList());
                }
                scoreMatrices.Add(new SimilarityMatrix.SubMatrix(xValues, yValues, scoreRows));
            }

            return new SimilarityMatrix(scoreMatrices);
        }
        
        public IEnumerable<SpectrumMetadata> SpectrumMetadatas
        {
            get
            {
                return this.Select(summary => summary.SpectrumMetadata);
            }
        }

        protected bool Equals(SpectrumSummaryList other)
        {
            return _summaries.Equals(other._summaries);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SpectrumSummaryList)obj);
        }

        public override int GetHashCode()
        {
            return _summaries.GetHashCode();
        }

        private class DigestKey
        {
            public DigestKey(double scanWindowLowerLimit, double scanWindowUpperLimit, int summaryValueLength, ImmutableList<ImmutableList<SpectrumPrecursor>> precursors)
            {
                ScanWindowLowerLimit = scanWindowLowerLimit;
                ScanWindowUpperLimit = scanWindowUpperLimit;
                SummaryValueLength = summaryValueLength;
                Precursors = precursors;

            }

            public double ScanWindowLowerLimit { get; }
            public double ScanWindowUpperLimit { get; }
            public int SummaryValueLength { get; }
            public ImmutableList<ImmutableList<SpectrumPrecursor>> Precursors { get; }

            protected bool Equals(DigestKey other)
            {
                return ScanWindowLowerLimit.Equals(other.ScanWindowLowerLimit) &&
                       ScanWindowUpperLimit.Equals(other.ScanWindowUpperLimit) && Precursors.Equals(other.Precursors);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((DigestKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = ScanWindowLowerLimit.GetHashCode();
                    hashCode = (hashCode * 397) ^ ScanWindowUpperLimit.GetHashCode();
                    hashCode = (hashCode * 397) ^ Precursors.GetHashCode();
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// Remove all of the spectra whose scan window or precursors are not common enough to do
        /// alignment on
        /// </summary>
        /// <returns></returns>
        public SpectrumSummaryList RemoveRareSpectra()
        {
            var digestKeysToKeep = this.GroupBy(GetSpectrumDigestKey)
                .Where(group => group.Count() >= MIN_SPECTRA_FOR_ALIGNMENT).Select(group => group.Key).ToHashSet();
            return new SpectrumSummaryList(this.Where(spectrum =>
                digestKeysToKeep.Contains(GetSpectrumDigestKey(spectrum))));
        }

        public SimilarityGrid GetSimilarityGrid(SpectrumSummaryList that)
        {
            var thisByDigestKey = this.ToLookup(GetSpectrumDigestKey);
            var thatByDigestKey = that.ToLookup(GetSpectrumDigestKey);
            int bestCount = 0;
            DigestKey bestDigestKey = null;
            foreach (var group in thisByDigestKey)
            {
                if (group.Key == null)
                {
                    continue;
                }

                var count = group.Count() * thatByDigestKey[group.Key].Count();
                if (count > bestCount)
                {
                    bestCount = count;
                    bestDigestKey = group.Key;
                }
            }

            if (bestCount == 0)
            {
                return null;
            }

            return new SimilarityGrid(thisByDigestKey[bestDigestKey], thatByDigestKey[bestDigestKey]);
        }
    }
}
