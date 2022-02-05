﻿using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public class ScoreQValueMap
    {
        public static readonly ScoreQValueMap EMPTY = new ScoreQValueMap(ImmutableSortedList<double, double>.EMPTY);
        private readonly ImmutableSortedList<double, double> _sortedList;
        public ScoreQValueMap(ImmutableSortedList<double, double> sortedList)
        {
            _sortedList = sortedList;
        }

        public double? GetQValue(double? zScore)
        {
            if (_sortedList.Count == 0 || !zScore.HasValue)
            {
                return null;
            }
            int index = _sortedList.BinarySearch(zScore.Value, true);
            if (index >= 0)
            {
                return _sortedList.Values[index];
            }

            index = ~index;
            if (index <= 0)
            {
                return 1;
            }

            if (index >= _sortedList.Count)
            {
                return _sortedList.Values[_sortedList.Count - 1];
            }

            double leftDifference = zScore.Value - _sortedList.Keys[index - 1];
            double rightDifference = _sortedList.Keys[index] - zScore.Value;
            double totalDifference = _sortedList.Keys[index] - _sortedList.Keys[index - 1];
            if (totalDifference == 0)
            {
                return _sortedList.Keys[index];
            }

            return (leftDifference * _sortedList.Values[index] + rightDifference * _sortedList.Values[index - 1]) /
                   totalDifference;
        }

        public static ScoreQValueMap FromMoleculeGroups(IEnumerable<PeptideGroupDocNode> moleculeGroups)
        {
            return FromTransitionGroups(moleculeGroups.SelectMany(moleculeGroup => moleculeGroup.Molecules)
                .SelectMany(molecule => molecule.TransitionGroups));
}

        public static ScoreQValueMap FromDocument(SrmDocument document)
        {
            return FromTransitionGroups(document.MoleculeTransitionGroups);
        }

        private static ScoreQValueMap FromTransitionGroups(IEnumerable<TransitionGroupDocNode> transitionGroups)
        {
            var uniqueScores = new HashSet<double>();
            var entries = new List<KeyValuePair<double, double>>();
            foreach (var transitionGroupDocNode in transitionGroups)
            {
                if (transitionGroupDocNode.Results == null)
                {
                    continue;
                }

                foreach (var chromInfoList in transitionGroupDocNode.Results)
                {
                    foreach (var chromInfo in chromInfoList)
                    {
                        if (chromInfo.UserSet == UserSet.TRUE || !chromInfo.QValue.HasValue || !chromInfo.ZScore.HasValue)
                        {
                            continue;
                        }

                        if (!uniqueScores.Add(chromInfo.ZScore.Value))
                        {
                            continue;
                        }
                        entries.Add(new KeyValuePair<double, double>(chromInfo.ZScore.Value, chromInfo.QValue.Value));
                    }
                }
            }

            return new ScoreQValueMap(ImmutableSortedList.FromValues(entries));
        }

        public static ScoreQValueMap FromScoreQValues(IEnumerable<KeyValuePair<double, double>> entries)
        {
            return new ScoreQValueMap(ImmutableSortedList.FromValues(UniqueEntries(entries)));
        }

        private static IEnumerable<KeyValuePair<double, double>> UniqueEntries(
            IEnumerable<KeyValuePair<double, double>> entries)
        {
            var uniqueScores = new HashSet<double>();
            foreach (var entry in entries)
            {
                if (uniqueScores.Add(entry.Key))
                {
                    yield return entry;
                }
            }
        }

        protected bool Equals(ScoreQValueMap other)
        {
            return Equals(_sortedList, other._sortedList);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScoreQValueMap) obj);
        }

        public override int GetHashCode()
        {
            return (_sortedList != null ? _sortedList.GetHashCode() : 0);
        }
    }
}
