using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class PeptideFeatureSet : Immutable
    {
        public const double MzMatchTolerance = 0.001;
        public const double ProductMassResolution = .1;
        private ImmutableList<FragmentedMolecule> _precursorFragmentMolecules;
        
        public PeptideFeatureSet(SrmSettings settings, PeptideDocNode peptide, IEnumerable<FeatureKey> featureKeys)
        {
            Settings = settings;
            var distributionSettings = DistributionSettings.DEFAULT
                .ChangeIsotopeAbundances(settings.TransitionSettings.FullScan.IsotopeAbundances)
                .ChangeMassElectron(BioMassCalc.MassElectron)
                .ChangeMinAbundance(0.001)
                .ChangeMassResolution(0.001);
            DistributionCache = new DistributionCache(distributionSettings);
            Peptide = peptide;
            _precursorFragmentMolecules = ImmutableList.ValueOf(Peptide.TransitionGroups.Select(tg=>FragmentedMolecule.GetFragmentedMolecule(Settings, Peptide, tg, null)));
            AllFeatureKeys = ImmutableSortedList.FromValues(featureKeys.Distinct()
                .Select(key => new KeyValuePair<double, FeatureKey>(key.Mz, key)));
            AllIsolationWindows = ImmutableList.ValueOf(AllFeatureKeys.Values
                .Where(fk => null != fk.Window)
                .Select(fk => fk.Window)
                .Distinct());
            TransitionFeatureWeights = ImmutableList.ValueOf(peptide.TransitionGroups.SelectMany(GetTransitionFeatureWeights));
            PrecursorClasses = ImmutableList.ValueOf(TransitionFeatureWeights
                .Select(tfw=>tfw.PrecursorClass).Distinct().OrderBy(pc=>pc));
        }
        public SrmSettings Settings { get; private set; }
        public DistributionCache DistributionCache { get; private set; }
        public PeptideDocNode Peptide { get; private set; }
        public ImmutableSortedList<double, FeatureKey> AllFeatureKeys { get; private set; }
        public ImmutableList<ScanInfo.IsolationWindow> AllIsolationWindows { get; private set; }
        public ImmutableList<PrecursorClass> PrecursorClasses { get; private set; }
        public ImmutableList<TransitionFeatureWeight> TransitionFeatureWeights { get; private set; }
        public PrecursorClass GetPrecursorClass(TransitionGroupDocNode transitionGroupDocNode)
        {
            var fragmentedMolecule = GetFragmentedMolecule(transitionGroupDocNode, null).ChangePrecursorCharge(0);
            double monoMass = DistributionCache.GetMonoMass(fragmentedMolecule.PrecursorFormula);
            return new PrecursorClass(monoMass);
        }

        public IEnumerable<PeptideDocNode.TransitionKey> GetAllTransitionKeys()
        {
            return Peptide.TransitionGroups
                .SelectMany(tg => tg.Transitions.Select(t => GetTransitionKey(tg, t)))
                .Distinct();
        }

        private static bool _alwaysSingleWeight = false;

        private IEnumerable<TransitionFeatureWeight> GetTransitionFeatureWeights(
            TransitionGroupDocNode transitionGroupDocNode)
        {
            var precursorClass = GetPrecursorClass(transitionGroupDocNode);
            foreach (var transition in transitionGroupDocNode.Transitions)
            {
                var transitionKey = GetTransitionKey(transitionGroupDocNode, transition);
                foreach (var feature in GetFeatures(transitionGroupDocNode, transition))
                {
                    yield return new TransitionFeatureWeight(precursorClass, transitionKey, transitionGroupDocNode,
                        transition, feature.Item1, _alwaysSingleWeight ? 1 : feature.Item2);
                }
            }
        }

        public static PeptideDocNode.TransitionKey GetTransitionKey(TransitionGroupDocNode transitionGroup,
            TransitionDocNode transition)
        {
            var transitionLossKey = new TransitionLossKey(transitionGroup, transition, transition.Losses);
            return new PeptideDocNode.TransitionKey(
                transitionGroup, transitionLossKey, IsotopeLabelType.light);
                
        }

        private IEnumerable<Tuple<FeatureKey, double>> GetFeatures(
            TransitionGroupDocNode transitionGroup,
            TransitionDocNode transition)
        {
            var list = new List<Tuple<FeatureKey, double>>();
            if (transition.IsMs1)
            {
                if (transition.Transition.MassIndex != 0)
                {
                    return list;
                }
                return GetMs1Features(transitionGroup);
            }
            var fragmentedMolecule = GetFragmentedMolecule(transitionGroup, transition);
            foreach (var isolationWindow in AllIsolationWindows)
            {
                var actualIsolationWindow = isolationWindow.ApplyIsolationScheme(Settings.TransitionSettings);
                var distribution = fragmentedMolecule.GetFragmentDistribution(
                    DistributionCache,
                    actualIsolationWindow.TargetMz - actualIsolationWindow.LowerOffset,
                    actualIsolationWindow.TargetMz + actualIsolationWindow.UpperOffset);
                foreach (var entry in distribution)
                {
                    foreach (var featureKey in FeatureKeysWithMz(entry.Key)
                        .Where(fk => Equals(fk.Window, isolationWindow)))
                    {
                        list.Add(Tuple.Create(featureKey, entry.Value));
                    }
                }
            }
            return list;
        }

        private IEnumerable<Tuple<FeatureKey, double>> GetMs1Features(TransitionGroupDocNode transitionGroup)
        {
            var fragmentedMolecule = GetFragmentedMolecule(transitionGroup, null);
            var mzDistribution = DistributionCache.GetMzDistribution(fragmentedMolecule.PrecursorFormula,
                fragmentedMolecule.PrecursorMassShift, fragmentedMolecule.PrecursorCharge);
            foreach (var entry in mzDistribution)
            {
                foreach (var featureKey in FeatureKeysWithMz(entry.Key).Where(fk => fk.Window == null))
                {
                    yield return Tuple.Create(featureKey, entry.Value);
                }
            }
        }

        private IEnumerable<FeatureKey> FeatureKeysWithMz(double mz)
        {
            for (int i = AllFeatureKeys.BinarySearch(mz - MzMatchTolerance).Start; i < AllFeatureKeys.Count; i++)
            {
                if (AllFeatureKeys.Keys[i] > mz + MzMatchTolerance)
                {
                    break;
                }
                yield return AllFeatureKeys.Values[i];
            }
        }

        public FeatureWeights GetFeatureWeights(IEnumerable<PeptideDocNode.TransitionKey> transitionKeys)
        {
            var featureWeights = new FeatureWeights(PrecursorClasses);
            foreach (var transitionKey in transitionKeys)
            {
                var tfws = TransitionFeatureWeights.Where(tfw => Equals(transitionKey, tfw.TransitionKey)).ToArray();
                var featureKeys = tfws.Select(tfw => tfw.FeatureKey).Distinct().ToArray();
                var conflicts = featureKeys.SelectMany(EnumerateConflicts)
                    .Where(tfw => !Equals(transitionKey, tfw.TransitionKey)).ToArray();
                if (conflicts.Any())
                {
                    continue;
                }
                var byPrecursorClass = tfws.ToLookup(t => t.PrecursorClass);
                foreach (var featureKey in featureKeys)
                {
                    var labelContribs = new List<double>();
                    foreach (var precursorClass in featureWeights.PrecursorClasses)
                    {
                        double contrib = 0;
                        foreach (var tfw in byPrecursorClass[precursorClass])
                        {
                            if (Equals(featureKey, tfw.FeatureKey))
                            {
                                contrib += tfw.Weight;
                            }
                        }
                        if (contrib <= 0)
                        {
                            labelContribs.Add(0);
                            continue;
                        }
                        double transitionGroupCount = byPrecursorClass[precursorClass].Select(tfw => tfw.TransitionGroup)
                            .Distinct().Count();
                        labelContribs.Add(contrib / transitionGroupCount);
                    }
                    if (labelContribs.Any(v => v != 0))
                    {
                        featureWeights = featureWeights.AddFeatureWeights(transitionKey, featureKey, labelContribs);
                    }
                }
            }
            return featureWeights;
        }

        public IEnumerable<TransitionFeatureWeight> EnumerateConflicts(FeatureKey featureKey)
        {
            return TransitionFeatureWeights.Where(t => HasConflict(featureKey, t.FeatureKey));
        }

        public bool HasConflict(FeatureKey featureKey1, FeatureKey featureKey2)
        {
            if (!Equals(featureKey1.Window, featureKey2.Window))
            {
                return false;
            }
            if (Math.Abs(featureKey1.Mz - featureKey2.Mz) <= ProductMassResolution)
            {
                return true;
            }
            return false;
        }

        public FragmentedMolecule GetFragmentedMolecule(TransitionGroupDocNode transitionGroupDocNode,
            TransitionDocNode transitionDocNode)
        {
            var nodeIndex = Peptide.FindNodeIndex(transitionGroupDocNode.Id);
            if (nodeIndex >= 0)
            {
                var fragmentedMolecule = _precursorFragmentMolecules[nodeIndex];
                if (transitionDocNode != null)
                {
                    fragmentedMolecule = fragmentedMolecule.ChangeTransition(transitionDocNode);
                }
                return fragmentedMolecule;
            }
            return FragmentedMolecule.GetFragmentedMolecule(Settings, Peptide, transitionGroupDocNode, transitionDocNode);
        }

        public ChromatogramGroupInfo DeconvoluteChromatogram(TransitionGroupDocNode transitionGroupDocNode,
            ChromatogramCollection chromatogramCollection)
        {
            var chromatogramGroupInfo = chromatogramCollection.ChromatogramGroups.FirstOrDefault(
                cg => Equals(cg.PrecursorMz, transitionGroupDocNode.PrecursorMz));
            if (chromatogramGroupInfo == null)
            {
                return null;
            }
            IList<ChromTransition> chromTransitions = new List<ChromTransition>();
            IList<TimeIntensities> deconvolutedChromatograms = new List<TimeIntensities>();
            int precursorIndex = PrecursorClasses
                .IndexOf(GetPrecursorClass(transitionGroupDocNode));
            if (precursorIndex < 0)
            {
                return null;
            }
            foreach (var transitionDocNode in transitionGroupDocNode.Transitions)
            {
                var transitionKey = GetTransitionKey(transitionGroupDocNode, transitionDocNode);
                var featureWeights = GetFeatureWeights(new[] { transitionKey });
                var deconvoluted = featureWeights.DeconvoluteChromatograms(Settings.TransitionSettings, chromatogramCollection);
                if (deconvoluted == null || deconvoluted[precursorIndex] == null)
                {
                    continue;
                }
                deconvolutedChromatograms.Add(deconvoluted[precursorIndex]);
                var closestChromTransition = FindClosestChromTransition(chromatogramGroupInfo, transitionDocNode).GetValueOrDefault();
                var chromTransition = new ChromTransition(transitionDocNode.Mz, closestChromTransition.ExtractionWidth, closestChromTransition.IonMobilityValue, closestChromTransition.IonMobilityExtractionWidth,
                    transitionDocNode.IsMs1 ? ChromSource.ms1 : ChromSource.fragment);
                chromTransitions.Add(chromTransition);
            }
            var oldHeader = chromatogramGroupInfo.Header;
            var chromGroupHeaderInfo = new ChromGroupHeaderInfo(transitionGroupDocNode.PrecursorMz, oldHeader.FileIndex,
                chromTransitions.Count, 0, oldHeader.NumPeaks, oldHeader.StartPeakIndex, oldHeader.StartScoreIndex, oldHeader.MaxPeakIndex, 0, 0, 0, 0, oldHeader.Flags, 0, 0, oldHeader.StartTime, oldHeader.EndTime, oldHeader.CollisionalCrossSection, oldHeader.IonMobilityUnits);
            InterpolationParams interpolationParams;
            if (chromatogramGroupInfo.TimeIntensitiesGroup is RawTimeIntensities)
            {
                interpolationParams = ((RawTimeIntensities) chromatogramGroupInfo.TimeIntensitiesGroup)
                    .InterpolationParams;
            }
            else
            {
                float minTime = chromatogramGroupInfo.TimeIntensitiesGroup.MinTime;
                float maxTime = chromatogramGroupInfo.TimeIntensitiesGroup.MaxTime;
                int numPoints = chromatogramGroupInfo.TimeIntensitiesGroup.NumInterpolatedPoints;
                interpolationParams = new InterpolationParams(minTime,
                    maxTime,
                    numPoints, (maxTime - minTime) / (numPoints - 1));                
            }
            var timeIntensitiesGroup = new RawTimeIntensities(deconvolutedChromatograms, interpolationParams);
            return new DeconvolutedChromatogram(chromGroupHeaderInfo, chromatogramGroupInfo._scoreTypeIndices,
                chromatogramGroupInfo._allFiles, chromTransitions.ToArray(), chromatogramGroupInfo._allPeaks,
                chromatogramGroupInfo._allScores)
            {
                TimeIntensitiesGroup = timeIntensitiesGroup
            };
        }

        private ChromTransition? FindClosestChromTransition(ChromatogramGroupInfo chromatogramGroupInfo,
            TransitionDocNode transition)
        {
            double bestDistance = double.MaxValue;
            ChromTransition? closest = null;
            var source = transition.IsMs1 ? ChromSource.ms1 : ChromSource.fragment;
            for (int i = 0; i < chromatogramGroupInfo.NumTransitions; i++)
            {
                var chromTransition = chromatogramGroupInfo.GetChromTransitionLocal(i);
                if (chromTransition.Source != source)
                {
                    continue;
                }
                double distance = Math.Abs(transition.Mz - chromTransition.Product);
                if (closest == null || distance < bestDistance)
                {
                    closest = chromTransition;
                    bestDistance = distance;
                }
            }
            return closest;
        }
    }
}

