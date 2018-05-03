﻿using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class ChromatogramCollection
    {
        public ChromatogramCollection(IEnumerable<ChromatogramGroupInfo> chromatogramGroups)
        {
            ChromatogramGroups = ImmutableList.ValueOf(chromatogramGroups);
        }

        public ImmutableList<ChromatogramGroupInfo> ChromatogramGroups { get; private set; }

        public IEnumerable<FeatureKey> GetFeatureKeys()
        {
            var featureKeys = new HashSet<FeatureKey>();
            foreach (var chromatogramGroup in ChromatogramGroups)
            {
                for (int iTransition = 0; iTransition < chromatogramGroup.NumTransitions; iTransition++)
                {
                    var chromTransition = chromatogramGroup.GetChromTransitionLocal(iTransition);
                    if (chromTransition.Source == ChromSource.ms1)
                    {
                        featureKeys.Add(new FeatureKey(null, chromTransition.Product));
                        continue;
                    }
                    var timeIntensities = chromatogramGroup.TimeIntensitiesGroup.TransitionTimeIntensities[iTransition];
                    var scanInfos = chromatogramGroup.ScanInfos;
                    var isolationWindows = timeIntensities.ScanIds.SelectMany(id => scanInfos[id].ScanType.IsolationWindows).Distinct();
                    foreach (var window in isolationWindows)
                    {
                        featureKeys.Add(new FeatureKey(window, chromTransition.Product));
                    }
                }
            }
            return featureKeys;
        }

        public TimeIntensities GetChromatogram(FeatureKey featureKey)
        {
            var chromSource = featureKey.Window == null ? ChromSource.ms1 : ChromSource.fragment;
            foreach (var chromatogramGroup in ChromatogramGroups)
            {
                for (int iTransition = 0; iTransition < chromatogramGroup.NumTransitions; iTransition++)
                {
                    var chromTransition = chromatogramGroup.GetChromTransitionLocal(iTransition);
                    if (chromTransition.Source != chromSource)
                    {
                        continue;
                    }
                    var timeIntensities = chromatogramGroup.TimeIntensitiesGroup.TransitionTimeIntensities[iTransition];
                    if (featureKey.Window != null)
                    {
                        timeIntensities = Filter(timeIntensities, featureKey.Window, chromatogramGroup.ScanInfos);
                    }
                    if (timeIntensities.NumPoints > 0)
                    {
                        return timeIntensities;
                    }
                }
            }
            return null;
        }

        public TimeIntensities Filter(TimeIntensities timeIntensities, ScanInfo.IsolationWindow isolationWindow, IList<ScanInfo> scanInfos)
        {
            var newIndexes = new List<int>();
            for (int i = 0; i < timeIntensities.NumPoints; i++)
            {
                var scanInfo = scanInfos[timeIntensities.ScanIds[i]];
                if (!scanInfo.ScanType.IsolationWindows.Contains(isolationWindow))
                {
                    continue;
                }
                newIndexes.Add(i);
            }
            if (newIndexes.Count == timeIntensities.NumPoints)
            {
                return timeIntensities;
            }
            return new TimeIntensities(
                new IndexedSubList<float>(timeIntensities.Times, newIndexes), 
                new IndexedSubList<float>(timeIntensities.Intensities, newIndexes), 
                new IndexedSubList<float>(timeIntensities.MassErrors, newIndexes), 
                new IndexedSubList<int>(timeIntensities.ScanIds, newIndexes));
        }
    }
}
