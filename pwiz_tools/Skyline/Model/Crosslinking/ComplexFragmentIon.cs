﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2020 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Crosslinking
{
    /// <summary>
    /// Represents a set of Transitions from different peptides linked together by crosslinked modifications.
    /// </summary>
    public class ComplexFragmentIon : Immutable, IComparable<ComplexFragmentIon>
    {
        public ComplexFragmentIon(Transition transition, TransitionLosses transitionLosses, ImmutableSortedList<ModificationSite, LinkedPeptide> crosslinkStructure, bool isOrphan = false)
        {
            Transition = transition;
            Children = ImmutableSortedList<ModificationSite, ComplexFragmentIon>.EMPTY;
            TransitionLosses = transitionLosses;
            IsOrphan = isOrphan;
            CrosslinkStructure = crosslinkStructure ?? LinkedPeptide.EMPTY_CROSSLINK_STRUCTURE;
        }

        /// <summary>
        /// Creates a ComplexFragmentIon representing something which has no amino acids from the parent peptide.
        /// </summary>
        public static ComplexFragmentIon NewOrphanFragmentIon(TransitionGroup transitionGroup, ExplicitMods explicitMods, Adduct adduct)
        {
            var transition = new Transition(transitionGroup, IonType.precursor,
                transitionGroup.Peptide.Sequence.Length - 1, 0, adduct);
            return new ComplexFragmentIon(transition, null, explicitMods?.Crosslinks, true);
        }

        public Transition Transition { get; private set; }

        /// <summary>
        /// If true, this ion includes no amino acids from the parent peptide.
        /// </summary>
        public bool IsOrphan { get; private set; }

        /// <summary>
        /// Whether this ion has no amino acids from the parent peptide, or any of the children either.
        /// </summary>
        public bool IsEmptyOrphan
        {
            get { return IsOrphan && Children.Count == 0; }
        }

        [CanBeNull]
        public TransitionLosses TransitionLosses { get; private set; }
        public ImmutableSortedList<ModificationSite, ComplexFragmentIon> Children { get; private set; }

        public ImmutableSortedList<ModificationSite, LinkedPeptide> CrosslinkStructure { get; private set; }

        public ComplexFragmentIon ChangeCrosslinkStructure(
            ImmutableSortedList<ModificationSite, LinkedPeptide> crosslinkStructure)
        {
            return ChangeProp(ImClone(this), im => im.CrosslinkStructure = crosslinkStructure);
        }

        public IsotopeLabelType LabelType
        {
            get { return Transition.Group.LabelType; }
        }

        public ComplexFragmentIon AddChild(ModificationSite modificationSite, ComplexFragmentIon child)
        {
            if (IsOrphan && !IsEmptyOrphan)
            {
                throw new InvalidOperationException(string.Format(@"Cannot add {0} to {1}.", child, this));
            }

            if (child.Transition.MassIndex != 0)
            {
                throw new InvalidOperationException(string.Format(@"{0} cannot be a child fragment ion transition.", child.Transition));
            }

            return ChangeProp(ImClone(this), im => im.Children =
                ImmutableSortedList.FromValues(Children.Append(
                    new KeyValuePair<ModificationSite, ComplexFragmentIon>(
                        modificationSite, child))));
        }

        public ComplexFragmentIon ChangeMassIndex(int massIndex)
        {
            var transition = new Transition(Transition.Group, Transition.IonType, Transition.CleavageOffset, massIndex,
                Transition.Adduct, Transition.DecoyMassShift);
            return ChangeProp(ImClone(this), im => im.Transition = transition);
        }

        public int GetFragmentationEventCount()
        {
            int count = 0;
            if (!IsOrphan && !Transition.IsPrecursor())
            {
                count++;
            }

            if (null != TransitionLosses)
            {
                count += TransitionLosses.Losses.Count;
            }
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
                    return Transition.CleavageOffset < aaIndex;
                default:
                    return true;
            }
        }

        public MoleculeMassOffset GetNeutralFormula(SrmSettings settings, ExplicitMods explicitMods)
        {
            var result = GetSimpleFragmentFormula(settings, explicitMods);
            if (explicitMods != null)
            {
                foreach (var explicitMod in explicitMods.StaticModifications)
                {
                    if (explicitMod.LinkedPeptide != null)
                    {
                        result = result.Plus(GetCrosslinkFormula(settings, explicitMod));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the chemical formula for this fragment and none of its children.
        /// </summary>
        private MoleculeMassOffset GetSimpleFragmentFormula(SrmSettings settings, ExplicitMods mods)
        {
            if (IsOrphan)
            {
                return MoleculeMassOffset.EMPTY;
            }
            var modifiedSequence = ModifiedSequence.GetModifiedSequence(settings, Transition.Group.Peptide.Sequence, mods, LabelType)
                .SeverCrosslinks();
            var fragmentedMolecule = FragmentedMolecule.EMPTY.ChangeModifiedSequence(modifiedSequence);
            fragmentedMolecule = fragmentedMolecule.ChangeFragmentIon(Transition.IonType, Transition.Ordinal);
            if (null != TransitionLosses)
            {
                fragmentedMolecule = fragmentedMolecule.ChangeFragmentLosses(TransitionLosses.Losses.Select(loss => loss.Loss));
            }
            return new MoleculeMassOffset(fragmentedMolecule.FragmentFormula, 0, 0);
        }

        /// <summary>
        /// Returns the chemical formula of a the fragment ion linked to a particular crosslink modifacation.
        /// </summary>
        private MoleculeMassOffset GetCrosslinkFormula(SrmSettings settings, ExplicitMod explicitMod)
        {
            ComplexFragmentIon childFragmentIon;
            if (!Children.TryGetValue(explicitMod.ModificationSite, out childFragmentIon))
            {
                return MoleculeMassOffset.EMPTY;
            }
            var result = MoleculeMassOffset.EMPTY;
            var linkedPeptide = explicitMod.LinkedPeptide;
            var childFormula =
                childFragmentIon.GetNeutralFormula(settings, linkedPeptide.ExplicitMods);
            result = result.Plus(childFormula);
            return result;
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
            var complexFragmentIon = this;
            if (Children.Count > 0)
            {
                complexFragmentIon = ChangeProp(ImClone(complexFragmentIon),
                    im => im.Transition = (Transition) im.Transition.Copy());
            }

            if (IsMs1 && settings.TransitionSettings.FullScan.IsHighResPrecursor)
            {
                var massDistribution = FragmentedMolecule.Settings.FromSrmSettings(settings).GetMassDistribution(neutralFormula.Molecule, neutralFormula.MonoMassOffset, 0);
                var mzDistribution = massDistribution.OffsetAndDivide(
                    Transition.Adduct.AdductCharge * BioMassCalc.MassProton, Transition.Adduct.AdductCharge);
                var isotopeDist = IsotopeDistInfo.MakeIsotopeDistInfo(mzDistribution, productMass, Transition.Adduct, settings.TransitionSettings.FullScan);
                productMass = isotopeDist.GetMassI(Transition.MassIndex, Transition.DecoyMassShift);
                transitionQuantInfo = transitionQuantInfo.ChangeIsotopeDistInfo(new TransitionIsotopeDistInfo(
                    isotopeDist.GetRankI(Transition.MassIndex), isotopeDist.GetProportionI(Transition.MassIndex)));
            }
            // TODO: TransitionQuantInfo is probably wrong since it did not know the correct mass.
            return new TransitionDocNode(complexFragmentIon, annotations, productMass, transitionQuantInfo, explicitTransitionValues, results);
        }

        public static TypedMass GetFragmentMass(SrmSettings settings, MoleculeMassOffset formula)
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

        /// <summary>
        /// Returns a ComplexFragmentIonName object representing this ComplexFragmentIon
        /// </summary>
        public ComplexFragmentIonName GetName()
        {
            ComplexFragmentIonName name;
            if (IsOrphan)
            {
                name = ComplexFragmentIonName.ORPHAN;
            }
            else
            {
                name = new ComplexFragmentIonName(Transition.IonType, Transition.Ordinal);
            }

            foreach (var child in Children)
            {
                name = name.AddChild(child.Key, child.Value.GetName());
            }

            if (null != TransitionLosses)
            {
                foreach (var loss in TransitionLosses.Losses)
                {
                    name = name.AddLoss(null, loss.ToString());
                }
            }

            return name;
        }

        public override string ToString()
        {
            return GetName() + Transition.GetChargeIndicator(Transition.Adduct);
        }

        public bool IsMs1
        {
            get
            {
                if (IsOrphan)
                {
                    return false;
                }
                return Transition.IsPrecursor() && null == TransitionLosses &&
                       Children.Values.All(child => child.IsMs1);
            }
        }

        public int CompareTo(ComplexFragmentIon other)
        {
            if (0 == GetFragmentationEventCount())
            {
                if (0 != other.GetFragmentationEventCount())
                {
                    return -1;
                }
            }
            else if (0 == other.GetFragmentationEventCount())
            {
                return 1;
            }
            int result = IsOrphan.CompareTo(other.IsOrphan);
            if (result == 0)
            {
                result = TransitionGroup.CompareTransitionIds(Transition, other.Transition);
            }
                
            if (result == 0)
            {
                result = Comparer<double?>.Default.Compare(TransitionLosses?.Mass, other.TransitionLosses?.Mass);
            }

            if (result != 0)
            {
                return result;
            }
            for (int i = 0; i < Children.Count && i < other.Children.Count; i++)
            {
                result = Children[i].Key.CompareTo(other.Children[i].Key);
                if (result == 0)
                {
                    result = Children[i].Value.CompareTo(other.Children[i].Value);
                }

                if (result != 0)
                {
                    return result;
                }
            }

            return Children.Count.CompareTo(other.Children.Count);
        }

        /// <summary>
        /// Returns the text that should be displayed for this in the Targets tree.
        /// </summary>
        public string GetTargetsTreeLabel()
        {
            StringBuilder stringBuilder = new StringBuilder();
            // Simple case of two peptides linked together
            if (CrosslinkStructure.Count == 1 && CrosslinkStructure.Values[0].CrosslinkStructure.Count == 0)
            {
                var child = Children.Values.FirstOrDefault();
                if (!IsOrphan && Transition.IonType != IonType.precursor)
                {
                    stringBuilder.Append(Transition.AA);
                    stringBuilder.Append(@" ");
                }

                stringBuilder.Append(@"[");
                if (!IsOrphan)
                {
                    if (Transition.IonType == IonType.precursor)
                    {
                        stringBuilder.Append(@"p");
                    }
                    else
                    {
                        stringBuilder.Append(Transition.IonType);
                        stringBuilder.Append(Transition.Ordinal);
                    }
                }

                stringBuilder.Append(@"-");
                if (child != null)
                {
                    if (child.Transition.IonType == IonType.precursor)
                    {
                        stringBuilder.Append(@"p");
                    }
                    else
                    {
                        stringBuilder.Append(child.Transition.IonType);
                        stringBuilder.Append(child.Transition.Ordinal);
                    }
                }

                stringBuilder.Append(Transition.GetMassIndexText(Transition.MassIndex));
                stringBuilder.Append(@"]");
                if (child != null && child.Transition.IonType != IonType.precursor)
                {
                    stringBuilder.Append(@" ");
                    stringBuilder.Append(child.Transition.AA);
                }

                return stringBuilder.ToString();
            }

            return @"[" + GetName() + Transition.GetMassIndexText(Transition.MassIndex) + @"]";
        }
    }
}
