using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics.Models.Regression.Fitting;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Model.Crosslinking;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{
    public class IsotopeDeconvoluter
    {
        public IsotopeDeconvoluter(IEnumerable<MassDistribution> massDistributions)
        {
            MassDistributions = ImmutableList.ValueOf(massDistributions);
        }

        public ImmutableList<MassDistribution> MassDistributions { get; }

        public IList<TimeIntensities> Deconvolute(IList<Tuple<MzRange, TimeIntensities>> chromatogramChannels)
        {
            var candidateVectors = MassDistributions.Select(massDistribution =>
                GetCandidateVector(chromatogramChannels.Select(channel => channel.Item1), massDistribution)).ToArray();
            var timeIntensitiesList = MergeTimes(chromatogramChannels.Select(channel => channel.Item2));
            return Deconvolute(candidateVectors, timeIntensitiesList);
        }

        private List<TimeIntensities> Deconvolute(double[][] candidateVectors,
            IList<TimeIntensities> timeIntensitiesList)
        {
            var intensityLists = candidateVectors.Select(vector => new List<float>()).ToList();
            var firstTimeIntensities = timeIntensitiesList[0];
            for (int i = 0; i < firstTimeIntensities.Times.Count; i++)
            {
                var nonNegativeLeastSquares = new NonNegativeLeastSquares()
                {
                    MaxIterations = 100
                };
                var observedValues = timeIntensitiesList.Select(timeIntensities => (double) timeIntensities.Intensities[i])
                    .ToArray();
                var regression = nonNegativeLeastSquares.Learn(candidateVectors, observedValues);
                for (int iCandidate = 0; iCandidate < intensityLists.Count; iCandidate++)
                {
                    intensityLists[iCandidate].Add((float) regression.Weights[iCandidate]);
                }
            }

            return intensityLists.Select(intensityList =>
                    new TimeIntensities(firstTimeIntensities.Times, intensityList, null, firstTimeIntensities.ScanIds))
                .ToList();
        }

        public static List<TimeIntensities> MergeTimes(
            IEnumerable<TimeIntensities> timeIntensitiesEnumerable)
        {
            var list = timeIntensitiesEnumerable.ToList();
            var allTimes = ImmutableList.ValueOf(list.SelectMany(timeIntensities=> timeIntensities.Times).Distinct()
                .OrderBy(time => time));
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = list[i].Interpolate(allTimes, false);
            }

            return list;
        }

        public static double[] GetCandidateVector(IEnumerable<MzRange> mzRanges, MassDistribution massDistribution)
        {
            return mzRanges.Select(mzRange => SumRange(mzRange, massDistribution)).ToArray();
        }

        public static double SumRange(MzRange mzRange, MassDistribution massDistribution)
        {
            int i = CollectionUtil.BinarySearch(massDistribution.Keys, mzRange.Min);
            if (i < 0)
            {
                i = ~i;
            }

            double total = 0;
            for (; i < massDistribution.Keys.Count; i++)
            {
                if (massDistribution.Keys[i] > mzRange.Max)
                {
                    break;
                }

                total += massDistribution.Values[i];
            }

            return total;
        }

        public static MassDistribution GetMzDistribution(SrmSettings settings, PeptideDocNode peptideDocNode,
            TransitionGroupDocNode transitionGroupDocNode)
        {
            var moleculeMassOffset = GetPrecursorFormula(settings, peptideDocNode, transitionGroupDocNode);
            return settings.GetDefaultPrecursorCalc().GetMZDistribution(moleculeMassOffset,
                transitionGroupDocNode.PrecursorAdduct, settings.TransitionSettings.FullScan.IsotopeAbundances);
        }

        private static MoleculeMassOffset GetPrecursorFormula(SrmSettings settings, PeptideDocNode peptideDocNode, TransitionGroupDocNode transitionGroupDocNode)
        {
            if (transitionGroupDocNode.CustomMolecule != null)
            {
                return transitionGroupDocNode.CustomMolecule.ParsedMolecule.GetMoleculeMassOffset();
            }
            else
            {
                var crosslinkBuilder = new CrosslinkBuilder(settings, peptideDocNode.Peptide,
                    peptideDocNode.ExplicitMods, transitionGroupDocNode.LabelType);
                return crosslinkBuilder.GetPrecursorFormula();
            }
        }


#if false
public ImmutableList<float> MergeAllTimeLists(IList<ImmutableList<float>> lists, int start, int end)
        {
            
        }

        private static ImmutableList<float> MergeTimeLists(ImmutableList<float> left, ImmutableList<float> right)
        {
            if (Equals(left, right))
            {
                return left;
            }

            var mergedTimes = left.Concat(right).Distinct().ToList();
            if (mergedTimes.Count == left.Count)
            {
                return left;
            }

            if (mergedTimes.Count == right.Count)
            {
                return right;
            }

            return ImmutableList.ValueOf(mergedTimes.OrderBy(time => time));
        }
#endif
    }
}
