﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Crosslinking
{
    public class ComplexFragmentIon : Immutable
    {
        public static readonly ComplexFragmentIon EMPTY = new ComplexFragmentIon(ImmutableList.Empty<Transition>(), null);
        private static CustomMolecule EMPTY_MOLECULE = new CustomMolecule(
            new TypedMass(CustomMolecule.MIN_MASS, MassType.Monoisotopic),
            new TypedMass(CustomMolecule.MIN_MASS, MassType.Average));
        public ComplexFragmentIon(IEnumerable<Transition> transitions, TransitionLosses losses)
        {
            Transitions = ImmutableList.ValueOf(transitions);
            Losses = losses;
        }

        public ComplexFragmentIon Concat(ComplexFragmentIon child)
        {
            var newLosses = TransitionLosses;
            if (child.TransitionLosses != null)
            {
                if (newLosses == null)
                {
                    newLosses = child.TransitionLosses;
                }
                else
                {
                    newLosses = new TransitionLosses(newLosses.Losses.Concat(child.TransitionLosses.Losses).ToList(),
                        newLosses.MassType);
                }
            }
            return new ComplexFragmentIon(Transitions.Concat(child.Transitions), newLosses);
        }

        public ImmutableList<Transition> Transitions { get; private set; }

        public Transition PrimaryTransition
        {
            get { return Transitions.FirstOrDefault(); }
        }

        public TransitionLosses Losses { get; private set; }

        public TransitionLosses TransitionLosses
        {
            get { return Losses; }
        }

        public static bool IsEmptyTransition(Transition transition)
        {
            return transition.IonType == IonType.custom;
        }

        public static ComplexFragmentIon EmptyTransition(TransitionGroup transitionGroup)
        {
            return Simple(new Transition(transitionGroup, Adduct.SINGLY_PROTONATED, null, EMPTY_MOLECULE), null);
        }

        public bool? IncludesSite(CrosslinkSite site)
        {
            if (site.PeptideIndex >= Transitions.Count)
            {
                return null;
            }
            var transition = Transitions[site.PeptideIndex];
            if (IsEmptyTransition(transition))
            {
                return false;
            }
            return transition.IncludesAaIndex(site.AaIndex);
        }

        public ComplexFragmentIon CloneTransition()
        {
            return ChangeProp(ImClone(this), im => im.Transitions = im.Transitions.ReplaceAt(0, (Transition)Transitions[0].Copy()));
        }

        public static ComplexFragmentIon Simple(Transition transition, TransitionLosses losses)
        {
            return new ComplexFragmentIon(ImmutableList.Singleton(transition), losses);
        }

        public bool IsMs1
        {
            get
            {
                return IsIonTypePrecursor && Losses == null;
            }
        }

        public bool IsIonTypePrecursor
        {
            get
            {
                return Transitions[0].IsPrecursor();
            }
        }

        public bool IsCrosslinked
        {
            get { return Transitions.Count > 1; }
        }

        public string GetFragmentIonName()
        {
            return GetLabel(false);
        }

        public string GetTargetsTreeLabel()
        {
            return GetLabel(true) + Transition.GetMassIndexText(PrimaryTransition.MassIndex);
        }

        public ComplexFragmentIonKey GetName()
        {
            return new ComplexFragmentIonKey(Transitions.Select(t=>t.IonType), Transitions.Select(t=>t.Ordinal));
        }


        /// <summary>
        /// Returns the text that should be displayed for this in the Targets tree.
        /// </summary>
        private string GetLabel(bool includeResidues)
        {
            if (IsIonTypePrecursor)
            {
                return IonTypeExtension.GetLocalizedString(IonType.precursor) + GetTransitionLossesText();
            }

            StringBuilder stringBuilder = new StringBuilder();
            // Simple case of two peptides linked together
            if (includeResidues)
            {
                var firstTransition = Transitions[0];
                if (!IsEmptyTransition(firstTransition) && firstTransition.IonType != IonType.precursor)
                {
                    stringBuilder.Append(firstTransition.AA);
                    stringBuilder.Append(@" ");
                }
            }

            string strHyphen = string.Empty;
            stringBuilder.Append(@"[");
            foreach (var transition in Transitions)
            {
                stringBuilder.Append(strHyphen);
                strHyphen = @"-";
                if (IsEmptyTransition(transition))
                {
                    stringBuilder.Append(@"*");
                }
                else
                {
                    stringBuilder.Append(transition.IonType);
                    stringBuilder.Append(transition.Ordinal);
                }
            }
            stringBuilder.Append(GetTransitionLossesText());

            stringBuilder.Append(@"]");
            if (includeResidues && Transitions.Count > 1)
            {
                var lastTransition = Transitions[Transitions.Count - 1];
                if (!IsEmptyTransition(lastTransition))
                {
                    stringBuilder.Append(@" ");
                    stringBuilder.Append(lastTransition.AA);
                }
            }
            return stringBuilder.ToString();
        }
        private string GetTransitionLossesText()
        {
            if (TransitionLosses == null)
            {
                return string.Empty;
            }

            return @" -" + Math.Round(TransitionLosses.Mass, 1);
        }

        public int CountFragmentationEvents()
        {
            int result = 0;
            if (Losses != null)
            {
                result += Losses.Losses.Count;
            }

            foreach (var transition in Transitions)
            {
                switch (transition.IonType)
                {
                    case IonType.precursor:
                    case IonType.custom:
                        break;
                    default:
                        result++;
                        break;
                }
            }

            return result;
        }

        public bool IsAllowed(PeptideStructure peptideStructure)
        {
            int countIncluded = 0;
            int countExcluded = 0;
            foreach (var crosslink in peptideStructure.Crosslinks)
            {
                foreach (var site in crosslink.Sites)
                {
                    switch (IncludesSite(site))
                    {
                        case true:
                            countIncluded++;
                            break;
                        case false:
                            countExcluded++;
                            break;
                    }
                }
            }

            if (countIncluded != 0 && countExcluded != 0)
            {
                return false;
            }

            return true;
        }

        public ComplexFragmentIon ChangeMassIndex(int massIndex)
        {
            var transition = new Transition(PrimaryTransition.Group, PrimaryTransition.IonType, PrimaryTransition.CleavageOffset, massIndex,
                PrimaryTransition.Adduct, PrimaryTransition.DecoyMassShift);
            return ChangePrimaryTransition(transition);
        }

        public ComplexFragmentIon ChangeAdduct(Adduct adduct)
        {
            return ChangePrimaryTransition(new Transition(PrimaryTransition.Group, PrimaryTransition.IonType,
                PrimaryTransition.CleavageOffset,
                PrimaryTransition.MassIndex, adduct, PrimaryTransition.DecoyMassShift));
        }

        private ComplexFragmentIon ChangePrimaryTransition(Transition transition)
        {
            return ChangeProp(ImClone(this), im => im.Transitions = im.Transitions.ReplaceAt(0, transition));
        }

        public CrosslinkBuilder GetCrosslinkBuilder(SrmSettings settings, PeptideStructure peptideStructure)
        {
            return new CrosslinkBuilder(settings, peptideStructure, PrimaryTransition.Group.LabelType);
        }


        public TypedMass GetFragmentMass(SrmSettings settings, PeptideStructure peptideStructure)
        {
            return GetCrosslinkBuilder(settings, peptideStructure).GetFragmentMass(this);
        }

    }
}
