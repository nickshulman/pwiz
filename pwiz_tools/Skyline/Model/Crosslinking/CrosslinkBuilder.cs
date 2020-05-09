﻿using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Crosslinking
{
    public class CrosslinkBuilder
    {
        private IDictionary<ModificationSite, CrosslinkBuilder> _childBuilders = new Dictionary<ModificationSite, CrosslinkBuilder>();
        private MassDistribution _precursorMassDistribution;
        public CrosslinkBuilder(SrmSettings settings, Peptide peptide, ExplicitMods explicitMods, IsotopeLabelType labelType)
        {
            Settings = settings;
            Peptide = peptide;
            ExplicitMods = explicitMods;
            LabelType = labelType;
        }

        public SrmSettings Settings { get; private set; }
        public Peptide Peptide { get; private set; }
        public ExplicitMods ExplicitMods { get; private set; }
        public IsotopeLabelType LabelType { get; private set; }

        public TransitionDocNode MakeTransitionDocNode(ComplexFragmentIon complexFragmentIon, IsotopeDistInfo isotopeDist = null)
        {
            return MakeTransitionDocNode(complexFragmentIon, isotopeDist, Annotations.EMPTY,
                TransitionDocNode.TransitionQuantInfo.DEFAULT, ExplicitTransitionValues.EMPTY, null);
        }

        public TransitionDocNode MakeTransitionDocNode(ComplexFragmentIon complexFragmentIon,
            IsotopeDistInfo isotopeDist, 
            Annotations annotations,
            TransitionDocNode.TransitionQuantInfo transitionQuantInfo,
            ExplicitTransitionValues explicitTransitionValues,
            Results<TransitionChromInfo> results)
        {
            var neutralFormula = GetNeutralFormula(complexFragmentIon);
            var productMass = GetFragmentMassFromFormula(Settings, neutralFormula);
            if (complexFragmentIon.Children.Count > 0)
            {
                complexFragmentIon = complexFragmentIon.CloneTransition();
            }

            if (complexFragmentIon.IsMs1 && Settings.TransitionSettings.FullScan.IsHighResPrecursor)
            {
                isotopeDist = isotopeDist ?? GetPrecursorIsotopeDistInfo(complexFragmentIon.Transition.Adduct, 0);
                productMass = isotopeDist.GetMassI(complexFragmentIon.Transition.MassIndex, complexFragmentIon.Transition.DecoyMassShift);
                transitionQuantInfo = transitionQuantInfo.ChangeIsotopeDistInfo(new TransitionIsotopeDistInfo(
                    isotopeDist.GetRankI(complexFragmentIon.Transition.MassIndex), isotopeDist.GetProportionI(complexFragmentIon.Transition.MassIndex)));
            }
            return new TransitionDocNode(complexFragmentIon, annotations, productMass, transitionQuantInfo, explicitTransitionValues, results);
        }

        public MoleculeMassOffset GetNeutralFormula(ComplexFragmentIon complexFragmentIon)
        {
            var result = GetSimpleFragmentFormula(complexFragmentIon);
            foreach (var child in complexFragmentIon.Children)
            {
                var childBuilder = GetChildBuilder(child.Key);
                result = result.Plus(childBuilder.GetNeutralFormula(child.Value));
            }

            return result;
        }

        /// <summary>
        /// Returns the chemical formula for this fragment and none of its children.
        /// </summary>
        private MoleculeMassOffset GetSimpleFragmentFormula(ComplexFragmentIon complexFragmentIon)
        {
            if (complexFragmentIon.IsOrphan)
            {
                return MoleculeMassOffset.EMPTY;
            }
            var fragmentedMolecule = GetPrecursorMolecule().ChangeFragmentIon(complexFragmentIon.Transition.IonType, complexFragmentIon.Transition.Ordinal);
            if (null != complexFragmentIon.TransitionLosses)
            {
                fragmentedMolecule = fragmentedMolecule.ChangeFragmentLosses(complexFragmentIon.TransitionLosses.Losses.Select(loss => loss.Loss));
            }
            return new MoleculeMassOffset(fragmentedMolecule.FragmentFormula, 0, 0);
        }

        private FragmentedMolecule _precursorMolecule;
        public FragmentedMolecule GetPrecursorMolecule()
        {
            if (_precursorMolecule == null)
            {
                var modifiedSequence = ModifiedSequence.GetModifiedSequence(Settings, Peptide.Sequence, ExplicitMods, LabelType)
                    .SeverCrosslinks();
                _precursorMolecule = FragmentedMolecule.EMPTY.ChangeModifiedSequence(modifiedSequence);
            }

            return _precursorMolecule;
        }

        public TypedMass GetFragmentMass(ComplexFragmentIon complexFragmentIon)
        {
            var neutralFormula = GetNeutralFormula(complexFragmentIon);
            return GetFragmentMassFromFormula(Settings, neutralFormula);
        }

        public static TypedMass GetFragmentMassFromFormula(SrmSettings settings, MoleculeMassOffset formula)
        {
            var fragmentedMoleculeSettings = FragmentedMolecule.Settings.FromSrmSettings(settings);
            MassType massType = settings.TransitionSettings.Prediction.FragmentMassType;
            if (massType.IsMonoisotopic())
            {
                return new TypedMass(fragmentedMoleculeSettings.GetMonoMass(formula.Molecule) + formula.MonoMassOffset + BioMassCalc.MassProton, MassType.MonoisotopicMassH);
            }
            else
            {
                return new TypedMass(fragmentedMoleculeSettings.GetAverageMass(formula.Molecule) + formula.AverageMassOffset + BioMassCalc.MassProton, MassType.AverageMassH);
            }
        }

        public IsotopeDistInfo GetPrecursorIsotopeDistInfo(Adduct adduct, double decoyMassShift)
        {
            var massDistribution = GetPrecursorMassDistribution();
            var mzDistribution = massDistribution.OffsetAndDivide(
                adduct.AdductCharge * (BioMassCalc.MassProton + decoyMassShift), adduct.AdductCharge);

            return IsotopeDistInfo.MakeIsotopeDistInfo(mzDistribution, GetPrecursorMass(MassType.Monoisotopic), adduct, Settings.TransitionSettings.FullScan);
        }

        public TypedMass GetPrecursorMass(MassType massType)
        {
            var formula = GetPrecursorMolecule().PrecursorFormula;
            var fragmentedMoleculeSettings = GetFragmentedMoleculeSettings();
            double mass = massType.IsMonoisotopic()
                ? fragmentedMoleculeSettings.GetMonoMass(formula)
                : fragmentedMoleculeSettings.GetAverageMass(formula);

            return new TypedMass(mass + BioMassCalc.MassProton, massType | MassType.bMassH);
        }

        public MassDistribution GetPrecursorMassDistribution()
        {
            if (_precursorMassDistribution == null)
            {
                var fragmentedMoleculeSettings = FragmentedMolecule.Settings.FromSrmSettings(Settings);
                _precursorMassDistribution = fragmentedMoleculeSettings
                    .GetMassDistribution(GetPrecursorMolecule().PrecursorFormula, 0, 0);
            }

            return _precursorMassDistribution;
        }

        public FragmentedMolecule.Settings GetFragmentedMoleculeSettings()
        {
            return FragmentedMolecule.Settings.FromSrmSettings(Settings);
        }

        private CrosslinkBuilder GetChildBuilder(ModificationSite modificationSite)
        {
            CrosslinkBuilder childBuilder;
            if (!_childBuilders.TryGetValue(modificationSite, out childBuilder))
            {
                LinkedPeptide linkedPeptide;
                ExplicitMods.Crosslinks.TryGetValue(modificationSite, out linkedPeptide);
                childBuilder = new CrosslinkBuilder(Settings, linkedPeptide.Peptide, linkedPeptide.ExplicitMods, LabelType);
                _childBuilders.Add(modificationSite, childBuilder);
            }

            return childBuilder;
        }

        public IEnumerable<TransitionDocNode> GetComplexTransitions(
            TransitionGroup transitionGroup,
            IsotopeDistInfo isotopeDist,
            IEnumerable<TransitionDocNode> simpleTransitions, 
            bool useFilter)
        {
            var startingFragmentIons = new List<ComplexFragmentIon>();
            var productAdducts = Settings.TransitionSettings.Filter.PeptideProductCharges.ToHashSet();

            foreach (var simpleTransition in simpleTransitions)
            {
                var startingFragmentIon = simpleTransition.ComplexFragmentIon
                    .ChangeCrosslinkStructure(ExplicitMods.Crosslinks);
                startingFragmentIons.Add(startingFragmentIon);
            }

            IEnumerable<Adduct> allProductAdducts;
            if (useFilter)
            {
                allProductAdducts = Settings.TransitionSettings.Filter.PeptideProductCharges;
            }
            else
            {
                allProductAdducts = Settings.TransitionSettings.Filter.PeptideProductCharges
                    .Concat(Transition.DEFAULT_PEPTIDE_CHARGES);
            }

            allProductAdducts = allProductAdducts.Append(transitionGroup.PrecursorAdduct);

            // Add ions representing the precursor waiting to be joined with a crosslinked peptide
            foreach (var productAdduct in allProductAdducts.Distinct())
            {
                if (productAdduct.IsValidProductAdduct(transitionGroup.PrecursorAdduct, null))
                {
                    var precursorTransition = new Transition(transitionGroup, IonType.precursor,
                        Peptide.Sequence.Length - 1, 0, productAdduct);

                    startingFragmentIons.Add(new ComplexFragmentIon(precursorTransition, null, ExplicitMods.Crosslinks, true));
                    startingFragmentIons.Add(new ComplexFragmentIon(precursorTransition, null, ExplicitMods.Crosslinks));
                }
            }

            foreach (var complexFragmentIon in LinkedPeptide.PermuteComplexFragmentIons(ExplicitMods, Settings,
                Settings.PeptideSettings.Modifications.MaxNeutralLosses, useFilter, startingFragmentIons.Distinct()))
            {
                bool isMs1 = complexFragmentIon.IsMs1;
                if (isMs1)
                {
                    if (!transitionGroup.PrecursorAdduct.Equals(complexFragmentIon.Transition.Adduct))
                    {
                        continue;
                    }
                }
                else
                {
                    if (complexFragmentIon.Transition.MassIndex != 0)
                    {
                        continue;
                    }
                    if (useFilter)
                    {
                        if (!productAdducts.Contains(complexFragmentIon.Transition.Adduct))
                        {
                            continue;
                        }
                    }
                }

                var complexTransitionDocNode = MakeTransitionDocNode(complexFragmentIon, isotopeDist);
                yield return complexTransitionDocNode;
            }
        }

        public IEnumerable<TransitionDocNode> ExpandPrecursorIsotopes(TransitionDocNode transitionNode, IsotopeDistInfo isotopeDist, bool useFilter)
        {
            var fullScan = Settings.TransitionSettings.FullScan;
            foreach (int massIndex in fullScan.SelectMassIndices(isotopeDist, useFilter))
            {
                var complexFragmentIon = transitionNode.ComplexFragmentIon.ChangeMassIndex(massIndex);
                yield return MakeTransitionDocNode(complexFragmentIon, isotopeDist);
            }
        }

        public IEnumerable<TransitionDocNode> FilterTransitions(Dictionary<double, LibraryRankedSpectrumInfo.RankedMI> transitionRanks, IEnumerable<TransitionDocNode> transitions)
        {
            if (transitionRanks == null || transitionRanks.Count == 0)
            {
                return transitions;
            }

            var pick = Settings.TransitionSettings.Libraries.Pick;
            if (pick != TransitionLibraryPick.all && pick != TransitionLibraryPick.filter)
            {
                return transitions;
            }

            var tranRanks = new List<Tuple<LibraryRankedSpectrumInfo.RankedMI, TransitionDocNode>>();
            foreach (var transition in transitions)
            {
                LibraryRankedSpectrumInfo.RankedMI rankedMI;
                if (!transitionRanks.TryGetValue(transition.Mz, out rankedMI))
                {
                    continue;
                }

                var complexIonName = transition.ComplexFragmentIon.GetName();
                var matchedIon =
                    rankedMI.MatchedIons.FirstOrDefault(ion => Equals(complexIonName, ion.ComplexFragmentIonName));
                if (matchedIon == null)
                {
                    continue;
                }
                tranRanks.Add(Tuple.Create(rankedMI, transition));
            }

            int ionCount = Settings.TransitionSettings.Libraries.IonCount;
            if (ionCount < tranRanks.Count)
            {
                var rankValues = tranRanks.Select(tuple => tuple.Item1.Rank).ToList();
                rankValues.Sort();
                int cutoff = rankValues[ionCount];
                tranRanks = tranRanks.Where(tuple => tuple.Item1.Rank < cutoff).ToList();
            }

            return tranRanks.Select(tuple => tuple.Item2);
        }


    }
}
