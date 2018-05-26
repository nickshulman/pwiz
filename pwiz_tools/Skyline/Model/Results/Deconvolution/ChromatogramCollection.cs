using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class ChromatogramCollection
    {
        public ChromatogramCollection(IEnumerable<ChromatogramGroupInfo> chromatogramGroups)
        {
            ChromatogramGroups = ImmutableList.ValueOf(chromatogramGroups);
        }
        public ImmutableList<ChromatogramGroupInfo> ChromatogramGroups { get; private set; }

        public IEnumerable<FeatureKey> GetFeatureKeys(TransitionSettings transitionSettings)
        {
            var featureKeys = new HashSet<FeatureKey>();
            foreach (var chromatogramGroup in ChromatogramGroups)
            {
                IList<ScanInfo.IsolationWindow> isolationWindows = null;
                for (int iTransition = 0; iTransition < chromatogramGroup.NumTransitions; iTransition++)
                {
                    var chromTransition = chromatogramGroup.GetChromTransitionLocal(iTransition);
                    if (chromTransition.Source == ChromSource.ms1)
                    {
                        featureKeys.Add(new FeatureKey(null, chromTransition.Product));
                        continue;
                    }
                    var timeIntensities = chromatogramGroup.TimeIntensitiesGroup.TransitionTimeIntensities[iTransition];
                    if (isolationWindows == null)
                    {
                        var scanInfos = chromatogramGroup.ScanInfos;
                        if (scanInfos.Count == 0)
                        {
                            continue;
                        }
                        isolationWindows = timeIntensities.ScanIds.Select(id => scanInfos[id].ScanType)
                            .Where(scanType => 2 == scanType.MsLevel)
                            .Distinct()
                            .SelectMany(scanType =>
                                scanType.IsolationWindows).Distinct().ToArray();

                    }
                    foreach (var window in isolationWindows)
                    {
                        featureKeys.Add(new FeatureKey(window, chromTransition.Product));
                    }
                }
            }
            return featureKeys;
        }

        public TimeIntensities GetChromatogram(TransitionSettings transitionSettings, FeatureKey featureKey)
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
                    if (Math.Abs(featureKey.Mz - chromTransition.Product) > .001)
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
