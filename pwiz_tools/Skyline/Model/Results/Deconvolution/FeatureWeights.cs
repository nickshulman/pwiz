using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class FeatureWeights : Immutable
    {
        public FeatureWeights(IEnumerable<PrecursorClass> precursorClasses)
        {
            PrecursorClasses = ImmutableList.ValueOf(precursorClasses);
            FeatureKeys = ImmutableList<FeatureKey>.EMPTY;
            PrecursorContributions = ImmutableList<ImmutableList<double>>.EMPTY;
        }

        public ImmutableList<PrecursorClass> PrecursorClasses { get; private set; }
        public ImmutableList<FeatureKey> FeatureKeys { get; private set; }
        public ImmutableList<ImmutableList<double>> PrecursorContributions { get; private set; }

        public FeatureWeights AddFeatureWeights(FeatureKey featureKey,
            IEnumerable<double> contributions)
        {
            var lstContributions = ImmutableList.ValueOf(contributions);
            if (lstContributions.Count != PrecursorClasses.Count)
            {
                throw new ArgumentException();
            }
            return ChangeProp(ImClone(this), im =>
            {
                im.FeatureKeys = ImmutableList.ValueOf(FeatureKeys.Concat(new[] {featureKey}));
                im.PrecursorContributions = ImmutableList.ValueOf(PrecursorContributions.Concat(new[]{lstContributions}));
            });
        }

        public IList<TimeIntensities> DeconvoluteChromatograms(ChromatogramCollection chromatogramCollection)
        {
            var chromatograms = FeatureKeys.Select(chromatogramCollection.GetChromatogram).ToArray();
            var featureWeights = this;
            if (chromatograms.Contains(null))
            {
                var indexes = Enumerable.Range(0, chromatograms.Length).Where(i => null != chromatograms[i]).ToArray();
                if (indexes.Length == 0)
                {
                    return null;
                }
                featureWeights = ChangeProp(ImClone(this), im =>
                {
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
            return resultIntensities.Select(intensities => new TimeIntensities(mergedTimes, intensities, null, null)).ToArray();
        }
    }
}
