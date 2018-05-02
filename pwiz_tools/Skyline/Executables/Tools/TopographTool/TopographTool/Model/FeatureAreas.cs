using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using pwiz.Common.Collections;

namespace TopographTool.Model
{
    public class FeatureAreas
    {
        public FeatureAreas(FeatureWeights featureWeights, IEnumerable<double?> areas)
        {
            FeatureWeights = featureWeights;
            Areas = ImmutableList.ValueOf(areas.Select(FilterNaN));
            if (Areas.Count != FeatureWeights.RowCount)
            {
                throw new ArgumentException();
            }
            LabelCounts = ImmutableList.ValueOf(FeatureWeights.LabelContribs.SelectMany(l=>l.LabelCounts).Distinct().OrderBy(i=>i));
        }
        public FeatureWeights FeatureWeights { get; private set; }
        public ImmutableList<double?> Areas { get; private set; }
        public ImmutableList<int> LabelCounts { get; private set; }

        public IDictionary<TransitionKey, double> GetTotalAreaByTransition()
        {
            var result = new Dictionary<TransitionKey, double>();
            foreach (var group in Enumerable.Range(0, FeatureWeights.RowCount)
                .ToLookup(i => FeatureWeights.TransitionKeys[i]))
            {
                var totalArea = group.Sum(row => Areas[row].GetValueOrDefault());
                result.Add(group.Key, totalArea);
            }
            return result;
        }

        public IList<double?> GetNormalizedAreas()
        {
            var totalAreaByTransition = GetTotalAreaByTransition();
            var result = new List<double?>();
            for (int iRow = 0; iRow < FeatureWeights.RowCount; iRow++)
            {
                var area = Areas[iRow];
                if (area.HasValue && area.Value > 0)
                {
                    area /= totalAreaByTransition[FeatureWeights.TransitionKeys[iRow]];
                }
                result.Add(area);
            }
            return result;
        }

        public IList<double> GetLabelAmounts()
        {
            var observations = Vector.Build.Dense(GetNormalizedAreas().OfType<double>().ToArray());
            var candidateVectors = new List<Vector<double>>();
            foreach (var labelCount in LabelCounts)
            {
                var values = new List<double>();
                for (int iRow = 0; iRow < FeatureWeights.RowCount; iRow++)
                {
                    if (!Areas[iRow].HasValue)
                    {
                        continue;
                    }
                    values.Add(FeatureWeights.LabelContribs[iRow].GetContribution(labelCount));
                }
                candidateVectors.Add(Vector.Build.Dense(values.ToArray()));
            }
            var turnoverCalculator = new TurnoverCalculator();
            return turnoverCalculator.FindBestCombinationFilterNegatives(observations, candidateVectors);
        }

        public static FeatureAreas GetFeatureAreas(FeatureWeights featureWeights, ResultFile replicate, DataSet dataSet)
        {
            var rowsByTransitionKeys = Enumerable.Range(0, featureWeights.TransitionKeys.Count)
                .ToLookup(i => featureWeights.TransitionKeys[i]);
            var transitionDatas = dataSet.TransitionsByFeature;
            var areas = new double?[featureWeights.RowCount];
            foreach (var grouping in rowsByTransitionKeys)
            {
                var transitionKey = grouping.Key;
                foreach (var rowIndex in grouping)
                {
                    var tuple = Tuple.Create(transitionKey, featureWeights.FeatureKeys[rowIndex]);
                    areas[rowIndex] = GetFeatureArea(replicate, transitionDatas[tuple]);
                }
            }
            return new FeatureAreas(featureWeights, areas);
        }

        private static double? GetFeatureArea(ResultFile replicate, IEnumerable<TransitionData> transitionDatas)
        {
            foreach (var transitionData in transitionDatas)
            {
                var result = transitionData.Transition.GetResult(replicate);
                if (result == null)
                {
                    return null;
                }
                return result.Area;
            }
            return null;
        }

        private static double? FilterNaN(double? value)
        {
            if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            {
                return null;
            }
            return value.Value;
        }
    }
}
