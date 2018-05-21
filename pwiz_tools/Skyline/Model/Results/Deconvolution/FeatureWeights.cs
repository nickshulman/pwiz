using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class FeatureWeights : Immutable
    {
        public FeatureWeights(IEnumerable<PrecursorClass> precursorClasses)
        {
            PrecursorClasses = ImmutableList.ValueOf(precursorClasses);
            FeatureKeys = ImmutableList<FeatureKey>.EMPTY;
            PrecursorContributions = ImmutableList<ImmutableList<double>>.EMPTY;
            TransitionKeys = ImmutableList<PeptideDocNode.TransitionKey>.EMPTY;
        }

        public ImmutableList<PrecursorClass> PrecursorClasses { get; private set; }
        public ImmutableList<PeptideDocNode.TransitionKey> TransitionKeys { get; private set; }
        public ImmutableList<FeatureKey> FeatureKeys { get; private set; }
        public ImmutableList<ImmutableList<double>> PrecursorContributions { get; private set; }

        public FeatureWeights AddFeatureWeights(
            PeptideDocNode.TransitionKey transitionKey, 
            FeatureKey featureKey,
            IEnumerable<double> contributions)
        {
            var lstContributions = ImmutableList.ValueOf(contributions);
            if (lstContributions.Count != PrecursorClasses.Count)
            {
                throw new ArgumentException();
            }
            return ChangeProp(ImClone(this), im =>
            {
                im.TransitionKeys = ImmutableList.ValueOf(TransitionKeys.Concat(new []{transitionKey}));
                im.FeatureKeys = ImmutableList.ValueOf(FeatureKeys.Concat(new[] {featureKey}));
                im.PrecursorContributions = ImmutableList.ValueOf(PrecursorContributions.Concat(new[]{lstContributions}));
            });
        }

        public IList<TimeIntensities> DeconvoluteChromatograms(TransitionSettings transitionSettings, ChromatogramCollection chromatogramCollection)
        {
            if (FeatureKeys.Count == 0)
            {
                return null;
            }
            IList<TimeIntensities> chromatograms = FeatureKeys.Select(fk=>chromatogramCollection.GetChromatogram(transitionSettings, fk)).ToArray();
            var featureWeights = this;
            if (chromatograms.Contains(null))
            {
                var indexes = Enumerable.Range(0, chromatograms.Count).Where(i => null != chromatograms[i]).ToArray();
                if (indexes.Length == 0)
                {
                    return null;
                }
                featureWeights = ChangeProp(ImClone(this), im =>
                {
                    im.TransitionKeys = ImmutableList.ValueOf(indexes.Select(i=>TransitionKeys[i]));
                    im.FeatureKeys = ImmutableList.ValueOf(indexes.Select(i => FeatureKeys[i]));
                    im.PrecursorContributions = ImmutableList.ValueOf(indexes.Select(i => PrecursorContributions[i]));
                });
                chromatograms = indexes.Select(i => chromatograms[i]).ToArray();
            }
            return featureWeights.DeconvoluteChromatograms(chromatograms);
        }

        public IList<TimeIntensities> DeconvoluteChromatograms(IList<TimeIntensities> chromatograms)
        {
            var mergedTimes = ImmutableList.ValueOf(
                chromatograms.Where(c => c != null)
                    .SelectMany(c => c.Times)
                    .Distinct().OrderBy(t => t));
            chromatograms = chromatograms.Select(c => c.Interpolate(mergedTimes, false)).ToArray();
            if (TransitionKeys.Distinct().Count() > 1)
            {
                chromatograms = NormalizeChromatorams(chromatograms);
            }
            var turnoverCalculator = new TurnoverCalculator();
            var candidateVectors = new List<Vector<double>>();
            var resultIntensities = new List<List<float>>();
            for (int iPrecursor = 0; iPrecursor < PrecursorClasses.Count; iPrecursor++)
            {
                candidateVectors.Add(Vector.Build.Dense(PrecursorContributions.Select(pc => pc[iPrecursor]).ToArray()));
                resultIntensities.Add(new List<float>(mergedTimes.Count));
            }
            for (int iTime = 0; iTime < mergedTimes.Count; iTime++)
            {
                var observations = Vector.Build.Dense(chromatograms.Select(c => (double) c.Intensities[iTime]).ToArray());
                var values = turnoverCalculator.FindBestCombinationFilterNegatives(observations, candidateVectors);
                if (values == null)
                {
                    values = Vector.Build.Dense(PrecursorClasses.Count);
                }
                for (int iPrecursor = 0; iPrecursor < PrecursorClasses.Count; iPrecursor++)
                {
                    resultIntensities[iPrecursor].Add((float) values[iPrecursor]);
                }
            }
            ImmutableList<int> mergedScanIds = null;
            if (mergedTimes.Count == chromatograms[0].Times.Count)
            {
                mergedScanIds = chromatograms[0].ScanIds;
            }
            return resultIntensities.Select(intensities => new TimeIntensities(mergedTimes, intensities, null, mergedScanIds)).ToArray();
        }

        public IList<TimeIntensities> NormalizeChromatorams(IList<TimeIntensities> chromatograms)
        {
            var indexesByTransition = Enumerable.Range(0, TransitionKeys.Count).ToLookup(i => TransitionKeys[i]);
            var times = chromatograms[0].Times;
            var resultIntensities = chromatograms.Select(c => new List<float>()).ToArray();
            for (int iTime = 0; iTime < times.Count; iTime++)
            {
                foreach (var grouping in indexesByTransition)
                {
                    var totalIntensity = grouping.Select(iChrom => chromatograms[iChrom].Intensities[iTime]).Sum();
                    foreach (int iChrom in grouping)
                    {
                        resultIntensities[iChrom].Add(totalIntensity > 0 ? chromatograms[iChrom].Intensities[iTime] / totalIntensity : 0);
                    }
                }
            }
            var result = new TimeIntensities[chromatograms.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new TimeIntensities(times, resultIntensities[i], chromatograms[i].MassErrors,
                    chromatograms[i].ScanIds);
            }
            return result;
        }
    }
}
