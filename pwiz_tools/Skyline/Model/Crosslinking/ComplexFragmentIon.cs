﻿using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Crosslinking
{
    public class ComplexFragmentIon : Immutable
    {
        public ComplexFragmentIon(Transition transition, TransitionLosses transitionLosses)
        {
            Transition = transition;
            Children = ImmutableSortedList<ModificationSite, ComplexFragmentIon>.EMPTY;
        }

        public Transition Transition { get; private set; }

        public TransitionLosses TransitionLosses { get; private set; }
        public ImmutableSortedList<ModificationSite, ComplexFragmentIon> Children { get; private set; }

        public IsotopeLabelType LabelType
        {
            get { return Transition.Group.LabelType; }
        }

        public ComplexFragmentIon ChangeChildren(IEnumerable<KeyValuePair<ModificationSite, ComplexFragmentIon>> children)
        {
            return ChangeProp(ImClone(this), im => im.Children = ImmutableSortedList.FromValues(children));
        }

        public int GetFragmentationEventCount()
        {
            int count = 0;
            if (!Transition.IsPrecursor())
            {
                count++;
            }

            count += TransitionLosses.Losses.Count;
            count += Children.Values.Sum(child => child.GetFragmentationEventCount());
            return count;
        }

        public bool IncludesAaIndex(int aaIndex)
        {
            switch (Transition.IonType)
            {
                case IonType.precursor:
                    return true;
                case IonType.a:
                case IonType.b:
                case IonType.c:
                    return Transition.CleavageOffset >= aaIndex;
                case IonType.x:
                case IonType.y:
                case IonType.z:
                    return Transition.CleavageOffset <= aaIndex;
                default:
                    return true;
            }
        }

        public MoleculeMassOffset GetNeutralFormula(SrmSettings settings, ExplicitMods explicitMods)
        {
            var result = GetSimpleFragmentFormula(settings, explicitMods);
            foreach (var crosslinkMod in explicitMods.CrosslinkMods)
            {
                result = result.Plus(GetCrosslinkFormula(settings, crosslinkMod));
            }

            return result;
        }

        public MoleculeMassOffset GetSimpleFragmentFormula(SrmSettings settings, ExplicitMods mods)
        {
            var modifiedSequence =
                ModifiedSequence.GetModifiedSequence(settings, Transition.Group.Peptide.Sequence, mods, LabelType);
            var fragmentedMolecule = FragmentedMolecule.EMPTY.ChangeModifiedSequence(modifiedSequence);
            fragmentedMolecule = fragmentedMolecule.ChangeFragmentIon(Transition.IonType, Transition.Ordinal);
            fragmentedMolecule = fragmentedMolecule.ChangeFragmentLosses(TransitionLosses.Losses.Select(loss => loss.Loss));
            return new MoleculeMassOffset(fragmentedMolecule.FragmentFormula, 0);
        }



        public MoleculeMassOffset GetCrosslinkFormula(SrmSettings settings, CrosslinkMod crosslinkMod)
        {
            var children = GetChildrenAtSite(crosslinkMod.ModificationSite).ToList();
            if (children.Count == 0)
            {
                return MoleculeMassOffset.EMPTY;
            }

            if (children.Count != crosslinkMod.LinkedPeptides.Count)
            {
                throw new ArgumentException();
            }
            var result =
                crosslinkMod.CrosslinkerDef.FormulaMass.GetMoleculeMassOffset(GetMassType(settings));
            for (int iChild = 0; iChild < children.Count; iChild++)
            {
                var linkedPeptide = crosslinkMod.LinkedPeptides[iChild];
                var childFragmentIon = children[iChild];
                var childFormula =
                    childFragmentIon.GetNeutralFormula(settings, linkedPeptide.ExplicitMods);
                result = result.Plus(childFormula);
            }

            return result;
        }

        private MassType GetMassType(SrmSettings settings)
        {
            return settings.TransitionSettings.Prediction.FragmentMassType;
        }

        private IEnumerable<ComplexFragmentIon> GetChildrenAtSite(ModificationSite site)
        {
            return Children.Where(child => child.Key.Equals(site)).Select(child => child.Value);
        }

        public TransitionDocNode MakeTransitionDocNode(SrmSettings settings, ExplicitMods explicitMods)
        {
            return MakeTransitionDocNode(settings, explicitMods, Annotations.EMPTY, TransitionDocNode.TransitionQuantInfo.DEFAULT, ExplicitTransitionValues.EMPTY, null);
        }

        public TransitionDocNode MakeTransitionDocNode(SrmSettings settings, ExplicitMods explicitMods,
            Annotations annotations,
            TransitionDocNode.TransitionQuantInfo transitionQuantInfo,
            ExplicitTransitionValues explicitTransitionValues,
            Results<TransitionChromInfo> results)
        {
            var neutralFormula = GetNeutralFormula(settings, explicitMods);
            var productMass = GetFragmentMass(settings, neutralFormula);
            return new TransitionDocNode(this, annotations, productMass, transitionQuantInfo, explicitTransitionValues, results);
        }

        public static TypedMass GetFragmentMass(SrmSettings settings, MoleculeMassOffset formula)
        {
            MassType massType = settings.TransitionSettings.Prediction.FragmentMassType;
            var massDistribution = GetMassDistribution(settings, formula.Molecule);
            double mass = massType.IsMonoisotopic() ? massDistribution.MostAbundanceMass : massDistribution.AverageMass;
            return new TypedMass(mass + BioMassCalc.MassProton, massType | MassType.bMassH);
        }

        public static MassDistribution GetMassDistribution(SrmSettings settings, Molecule molecule)
        {
            var precursorCalc = settings.GetPrecursorCalc(IsotopeLabelType.light, ExplicitMods.EMPTY);
            return precursorCalc.GetMZDistributionFromFormula(molecule.ToString(), Adduct.EMPTY,
                settings.TransitionSettings.FullScan.IsotopeAbundances ?? BioMassCalc.DEFAULT_ABUNDANCES);
        }

        public static MassDistribution GetMzDistribution(SrmSettings settings, MoleculeMassOffset moleculeMassOffset, Adduct adduct)
        {
            var massDistribution = GetMassDistribution(settings, moleculeMassOffset.Molecule);
            double massOffset = moleculeMassOffset.MassOffset;
            if (adduct.IsProtonated && adduct.GetMassMultiplier() == 1)
            {
                massOffset += adduct.AdductCharge * BioMassCalc.MassProton;
            }
            else
            {
                massOffset -= adduct.AdductCharge * BioMassCalc.MassElectron;
            }
            return massDistribution.OffsetAndDivide(massOffset, adduct.AdductCharge);
        }
    }
}
