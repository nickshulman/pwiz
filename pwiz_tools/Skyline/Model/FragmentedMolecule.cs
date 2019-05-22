using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model
{
    public class FragmentedMolecule : Immutable
    {
        public static readonly FragmentedMolecule EMPTY = new FragmentedMolecule();
        public static readonly MassDistribution EMPTY_MASSDISTRIBUTION = new MassDistribution(.001, 0.00001);

        private static readonly IDictionary<char, Molecule> _aminoAcidFormulas =
            AminoAcidFormulas.DefaultFormulas.ToDictionary(kvp => kvp.Key, kvp => Molecule.Parse(kvp.Value));

        private FragmentedMolecule()
        {
            PrecursorFormula = FragmentFormula = Molecule.Empty;
            FragmentIonType = IonType.custom;
            FragmentOrdinal = 0;
            FragmentLosses = ImmutableList<FragmentLoss>.EMPTY;
        }
        public ModifiedSequence ModifiedSequence { get; private set; }

        public string UnmodifiedSequence
        {
            get { return ModifiedSequence == null ? null : ModifiedSequence.GetUnmodifiedSequence(); }
        }

        public FragmentedMolecule ChangeModifiedSequence(ModifiedSequence modifiedSequence)
        {
            return ChangeMoleculeProp(im => im.ModifiedSequence = modifiedSequence);
        }

        public int PrecursorCharge { get; private set; }

        public FragmentedMolecule ChangePrecursorCharge(int precursorCharge)
        {
            return ChangeMoleculeProp(im => im.PrecursorCharge = precursorCharge);
        }

        private FragmentedMolecule ChangeMoleculeProp(Action<FragmentedMolecule> action)
        {
            return ChangeProp(ImClone(this), im =>
            {
                action(im);
                im.UpdateMoleculeFormula();
                im.UpdateFragmentFormula();
            });
        }

        private FragmentedMolecule ChangeFragmentProp(Action<FragmentedMolecule> action)
        {
            return ChangeProp(ImClone(this), im =>
            {
                action(im);
                im.UpdateFragmentFormula();
            });
        }
        
        public Molecule PrecursorFormula { get; private set; }

        public FragmentedMolecule ChangePrecursorFormula(Molecule precursorFormula)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.PrecursorFormula = precursorFormula;
                im.ModifiedSequence = null;
            });
        }

        public double PrecursorMassShift { get; private set; }
        public MassType PrecursorMassType { get; private set; }

        public FragmentedMolecule ChangePrecursorMassShift(double precursorMassShift, MassType precursorMassType)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.PrecursorMassShift = precursorMassShift;
                im.PrecursorMassType = precursorMassType;
            });
        }
        public IonType FragmentIonType { get; private set; }

        public FragmentedMolecule ChangeFragmentIon(IonType ionType, int ordinal)
        {
            return ChangeFragmentProp(im =>
            {
                im.FragmentIonType = ionType;
                im.FragmentOrdinal = ordinal;
            });
        }
        public int FragmentOrdinal { get; private set; }
        public ImmutableList<FragmentLoss> FragmentLosses { get; private set; }

        public FragmentedMolecule ChangeFragmentLosses(IEnumerable<FragmentLoss> losses)
        {
            return ChangeMoleculeProp(im =>
            {
                im.FragmentLosses = ImmutableList.ValueOfOrEmpty(losses);
            });
        }

        public Molecule FragmentFormula { get; private set; }

        public FragmentedMolecule ChangeFragmentFormula(Molecule fragmentFormula)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.FragmentFormula = fragmentFormula;
                im.FragmentIonType = IonType.custom;
                im.FragmentOrdinal = 0;
            });
        }
        public int FragmentCharge { get; private set; }

        public FragmentedMolecule ChangeFragmentCharge(int charge)
        {
            return ChangeFragmentProp(im => im.FragmentCharge = charge);
        }
        public double FragmentMassShift { get; private set; }
        public MassType FragmentMassType { get; private set; }

        public FragmentedMolecule ChangeFragmentMassShift(double massShift, MassType massType)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.FragmentMassShift = massShift;
                im.FragmentMassType = massType;
            });
        }

        private void UpdateMoleculeFormula()
        {
            if (ModifiedSequence == null)
            {
                return;
            }
            double precursorMassShift;
            var precursorFormula = GetSequenceFormula(ModifiedSequence, PrecursorMassType, out precursorMassShift);
            SetFormulaForIonType(precursorFormula, IonType.precursor);
            SetCharge(precursorFormula, PrecursorCharge);
            PrecursorFormula = Molecule.FromDict(precursorFormula);
            PrecursorMassShift = precursorMassShift;
        }

        private void UpdateFragmentFormula()
        {
            if (ModifiedSequence == null)
            {
                return;
            }
            if (FragmentIonType == IonType.custom)
            {
                return;
            }
            if (FragmentIonType == IonType.precursor)
            {
                FragmentOrdinal = UnmodifiedSequence.Length;
                var fragmentFormula = PrecursorFormula.ToDictionary();
                RemoveCharge(fragmentFormula, PrecursorCharge);
                SetCharge(fragmentFormula, FragmentCharge);
                FragmentFormula = Molecule.FromDict(fragmentFormula);
                FragmentMassShift = PrecursorMassShift;
            }
            else
            {
                FragmentOrdinal = Math.Max(1, Math.Min(UnmodifiedSequence.Length, FragmentOrdinal));
                ModifiedSequence fragmentSequence = GetFragmentSequence(ModifiedSequence, FragmentIonType, FragmentOrdinal);
                double fragmentMassShift;
                var fragmentFormula = GetSequenceFormula(fragmentSequence, FragmentMassType, out fragmentMassShift);

                AddFragmentLosses(fragmentFormula, FragmentLosses, FragmentMassType, ref fragmentMassShift);
                SetFormulaForIonType(fragmentFormula, FragmentIonType);
                SetCharge(fragmentFormula, FragmentCharge);
                FragmentFormula = Molecule.FromDict(fragmentFormula);
                FragmentMassShift = fragmentMassShift;
            }
        }

        public FragmentedMolecule IncrementFragmentOrdinal()
        {
            int newFragmentOrdinal = FragmentOrdinal + 1;
            string unmodifiedSequence = ModifiedSequence.GetUnmodifiedSequence();
            int aaPosition;
            if (IsNTerminalIon(FragmentIonType))
            {
                aaPosition = newFragmentOrdinal - 1;
            }
            else
            {
                aaPosition = unmodifiedSequence.Length - newFragmentOrdinal;
            }

            var newExplicitMods = ModifiedSequence.GetModifications()
                .Where(mod => mod.IndexAA == aaPosition)
                .Select(mod => new ModifiedSequence.Modification(
                    new ExplicitMod(0, mod.StaticMod), mod.MonoisotopicMass, mod.AverageMass));
            var modifiedSequenceDiff = new ModifiedSequence(unmodifiedSequence.Substring(aaPosition, 1),
                newExplicitMods, MassType.Monoisotopic);
            var newFormula = FragmentFormula.ToDictionary();
            double fragmentMassShiftDiff;
            var formulaDiff = GetSequenceFormula(modifiedSequenceDiff, FragmentMassType, out fragmentMassShiftDiff);
            Add(newFormula, formulaDiff);
            return ChangeProp(ImClone(this), im =>
            {
                im.FragmentOrdinal = newFragmentOrdinal;
                im.FragmentFormula = Molecule.FromDict(newFormula);
                im.FragmentMassShift += fragmentMassShiftDiff;
            });
        }

        public IDictionary<double, double> GetFragmentDistribution(Settings settings, double? precursorMinMz, double? precursorMaxMz)
        {
            var fragmentDistribution = settings.GetMassDistribution(FragmentFormula, FragmentMassShift, FragmentCharge);
            var otherFragmentFormula = GetComplementaryProductFormula();
            var otherFragmentDistribution = settings.GetMassDistribution(otherFragmentFormula, PrecursorMassShift - FragmentMassShift, PrecursorCharge);
            var result = new Dictionary<double, double>();
            foreach (var entry in fragmentDistribution)
            {
                var fragmentPrecursorMz = entry.Key * FragmentCharge / PrecursorCharge;
                double? minOtherMz = precursorMinMz - fragmentPrecursorMz;
                double? maxOtherMz = precursorMaxMz - fragmentPrecursorMz;
                var otherFragmentAbundance = otherFragmentDistribution
                    .Where(oFrag => !minOtherMz.HasValue || oFrag.Key >= minOtherMz 
                        && !maxOtherMz.HasValue || oFrag.Key <= maxOtherMz).Sum(frag => frag.Value);
                if (otherFragmentAbundance > 0)
                {
                    result.Add(entry.Key, otherFragmentAbundance * entry.Value);
                }
            }
            return result;
        }

        public Molecule GetComplementaryProductFormula()
        {
            var difference = new Dictionary<string, int>(PrecursorFormula);
            foreach (var entry in FragmentFormula)
            {
                int count;
                difference.TryGetValue(entry.Key, out count);
                count -= entry.Value;
                difference[entry.Key] = count;
            }
            var negative = difference.FirstOrDefault(entry => entry.Value < 0);
            if (null != negative.Key)
            {
                string message = string.Format(
                    "Unable to calculate expected distribution because the fragment contains more '{0}' atoms than the precursor.",
                    negative.Key);
                throw new InvalidOperationException(message);
            }
            return Molecule.FromDict(difference);
        }

        private static Dictionary<string, int> GetSequenceFormula(ModifiedSequence modifiedSequence, MassType massType, out double unexplainedMassShift)
        {
            unexplainedMassShift = 0;
            string unmodifiedSequence = modifiedSequence.GetUnmodifiedSequence();
            var molecule = new Dictionary<string, int>();
            foreach (var aa in unmodifiedSequence)
            {
                Add(molecule, _aminoAcidFormulas[aa]);
            }
               
            var modifications = modifiedSequence.GetModifications().ToLookup(mod => mod.IndexAA);
            for (int i = 0; i < unmodifiedSequence.Length; i++)
            {
                foreach (var mod in modifications[i])
                {
                    string formula = mod.Formula;
                    if (formula == null)
                    {
                        var staticMod = mod.StaticMod;
                        var aa = unmodifiedSequence[i];
                        if ((staticMod.LabelAtoms & LabelAtoms.LabelsAA) != LabelAtoms.None && AminoAcid.IsAA(aa))
                        {
                            formula = SequenceMassCalc.GetHeavyFormula(aa, staticMod.LabelAtoms);
                        }
                    }
                    if (formula != null)
                    {
                        var modFormula = Molecule.ParseExpression(formula);
                        Add(molecule, modFormula);
                    }
                    else
                    {
                        unexplainedMassShift += massType.IsMonoisotopic() ? mod.MonoisotopicMass : mod.AverageMass;
                    }
                }
            }
            return molecule;
        }

        private static void Subtract(Dictionary<string, int> dict, IEnumerable<KeyValuePair<string, int>> delta)
        {
            Add(dict, delta.Select(kvp=>new KeyValuePair<string, int>(kvp.Key, -kvp.Value)));
        }

        private static void Add(Dictionary<string, int> dict, IEnumerable<KeyValuePair<string, int>> molecule)
        {
            foreach (var item in molecule)
            {
                int count;
                if (dict.TryGetValue(item.Key, out count))
                {
                    dict[item.Key] = count + item.Value;
                }
                else
                {
                    dict[item.Key] = item.Value;
                }
            }
        }

        private static void SetCharge(Dictionary<string, int> neutralFormula, int charge)
        {
            if (charge > 0)
            {
                Add(neutralFormula, new []{new KeyValuePair<string, int>("H", charge)});
            }
        }

        private static void RemoveCharge(Dictionary<string, int> chargedFormula, int charge)
        {
            if (charge > 0)
            {
                Add(chargedFormula, new[] {new KeyValuePair<string, int>(@"H", -charge)});
            }
        }

        public static ModifiedSequence GetFragmentSequence(ModifiedSequence modifiedSequence, IonType ionType,
            int ordinal)
        {
            string unmodifiedSequence = modifiedSequence.GetUnmodifiedSequence();
            if (IsNTerminalIon(ionType))
            {
                return new ModifiedSequence(unmodifiedSequence.Substring(0, ordinal),
                    modifiedSequence.GetModifications().Where(mod => mod.IndexAA < ordinal),
                    MassType.Monoisotopic);
            }

            int offset = unmodifiedSequence.Length - ordinal;
            string fragmentSequence = unmodifiedSequence.Substring(offset);
            var newModifications = modifiedSequence.GetModifications()
                .Where(mod => mod.IndexAA >= offset)
                .Select(mod => mod.ChangeIndexAa(mod.IndexAA - offset));

            return new ModifiedSequence(fragmentSequence, newModifications, MassType.Monoisotopic);
        }

        private static bool IsNTerminalIon(IonType ionType)
        {
            switch (ionType)
            {
                case IonType.a:
                case IonType.b:
                case IonType.c:
                    return true;
                default:
                    return false;
            }
        }

        public static void SetFormulaForIonType(Dictionary<string, int> molecule, IonType ionType)
        {
            IList<KeyValuePair<string, int>> deltas;
            switch (ionType)
            {
                case IonType.precursor:
                    deltas = new[] {new KeyValuePair<string, int>("H", 2), new KeyValuePair<string, int>("O", 1)};
                    break;
                case IonType.a:
                    deltas = new[] { new KeyValuePair<string, int>("C", -1), new KeyValuePair<string, int>("O", -1)};
                    break;
                case IonType.b:
                    deltas = new KeyValuePair<string, int>[0];
                    break;
                case IonType.c:
                    deltas = new[] { new KeyValuePair<string, int>("H", 3), new KeyValuePair<string, int>("N", 1)};
                    break;
                case IonType.x:
                    deltas = new[] { new KeyValuePair<string, int>("O", 2), new KeyValuePair<string, int>("C", 1)};
                    break;
                case IonType.y:
                    deltas = new[] { new KeyValuePair<string, int>("H", 2), new KeyValuePair<string, int>("O", 1)};
                    break;
                case IonType.z:
                    deltas = new[] { new KeyValuePair<string, int>("H", -1), new KeyValuePair<string, int>("O", 1), new KeyValuePair<string, int>("N", -1)};
                    break;
                default:
                    throw new ArgumentException();
            }

            Add(molecule, deltas);
        }

        public static void AddFragmentLosses(Dictionary<string, int> molecule, IList<FragmentLoss> fragmentLosses, 
            MassType massType, ref double unexplainedMass)
        {
            foreach (var fragmentLoss in fragmentLosses)
            {
                if (string.IsNullOrEmpty(fragmentLoss.Formula))
                {
                    unexplainedMass += massType.IsMonoisotopic() ? fragmentLoss.MonoisotopicMass : fragmentLoss.AverageMass;
                    continue;
                }
                Molecule lossFormula;
                int ichMinus = fragmentLoss.Formula.IndexOf('-');
                if (ichMinus < 0)
                {
                    lossFormula = Molecule.Parse(fragmentLoss.Formula);
                }
                else
                {
                    lossFormula = Molecule.Parse(fragmentLoss.Formula.Substring(0, ichMinus));
                    lossFormula = lossFormula.Difference(Molecule.Parse(fragmentLoss.Formula.Substring(ichMinus + 1)));
                }
                Subtract(molecule, lossFormula);
            }
        }

        public static FragmentedMolecule GetFragmentedMolecule(SrmSettings settings, PeptideDocNode peptideDocNode,
            TransitionGroupDocNode transitionGroupDocNode, TransitionDocNode transitionDocNode)
        {

            FragmentedMolecule fragmentedMolecule = EMPTY
                .ChangePrecursorMassShift(0, settings.TransitionSettings.Prediction.PrecursorMassType)
                .ChangeFragmentMassShift(0, settings.TransitionSettings.Prediction.FragmentMassType);
            if (peptideDocNode == null)
            {
                return fragmentedMolecule;
            }
            var labelType = transitionGroupDocNode == null
                ? IsotopeLabelType.light
                : transitionGroupDocNode.TransitionGroup.LabelType;
            if (peptideDocNode.IsProteomic)
            {
                fragmentedMolecule = fragmentedMolecule.ChangeModifiedSequence(
                    ModifiedSequence.GetModifiedSequence(settings, peptideDocNode, labelType));
                if (transitionGroupDocNode != null)
                {
                    fragmentedMolecule = fragmentedMolecule
                        .ChangePrecursorCharge(transitionGroupDocNode.PrecursorCharge);
                }
                if (transitionDocNode == null || transitionDocNode.IsMs1)
                {
                    return fragmentedMolecule;
                }
                var transition = transitionDocNode.Transition;
                fragmentedMolecule = fragmentedMolecule
                    .ChangeFragmentIon(transition.IonType, transition.Ordinal)
                    .ChangeFragmentCharge(transition.Charge);
                var transitionLosses = transitionDocNode.Losses;
                if (transitionLosses != null)
                {
                    var fragmentLosses = transitionLosses.Losses.Select(transitionLoss => transitionLoss.Loss);
                    fragmentedMolecule = fragmentedMolecule.ChangeFragmentLosses(fragmentLosses);
                }
                return fragmentedMolecule;
            }
            if (transitionGroupDocNode == null)
            {
                return fragmentedMolecule
                    .ChangePrecursorFormula(
                        Molecule.Parse(peptideDocNode.CustomMolecule.Formula ?? string.Empty));
            }
            var customMolecule = transitionGroupDocNode.CustomMolecule;
            fragmentedMolecule =
                fragmentedMolecule.ChangePrecursorCharge(transitionGroupDocNode.TransitionGroup
                    .PrecursorCharge);
            if (customMolecule.Formula != null)
            {
                var ionInfo = new IonInfo(customMolecule.Formula,
                    transitionGroupDocNode.PrecursorAdduct);
                fragmentedMolecule = fragmentedMolecule
                    .ChangePrecursorFormula(Molecule.Parse(ionInfo.FormulaWithAdductApplied));
            }
            else
            {

                fragmentedMolecule = fragmentedMolecule.ChangePrecursorMassShift(
                    transitionGroupDocNode.PrecursorAdduct.MassFromMz(
                        transitionGroupDocNode.PrecursorMz, transitionGroupDocNode.PrecursorMzMassType), 
                        transitionGroupDocNode.PrecursorMzMassType);
            }
            if (transitionDocNode == null || transitionDocNode.IsMs1)
            {
                return fragmentedMolecule;
            }
            var customIon = transitionDocNode.Transition.CustomIon;
            if (customIon.Formula != null)
            {
                fragmentedMolecule = fragmentedMolecule.ChangeFragmentFormula(
                    Molecule.Parse(customIon.FormulaWithAdductApplied));
            }
            else
            {
                fragmentedMolecule = fragmentedMolecule.ChangeFragmentMassShift(
                    transitionDocNode.Transition.Adduct.MassFromMz(
                        transitionDocNode.Mz, transitionDocNode.MzMassType), 
                        transitionDocNode.MzMassType);
            }
            fragmentedMolecule = fragmentedMolecule
                .ChangeFragmentCharge(transitionDocNode.Transition.Charge);
            return fragmentedMolecule;
        }

        public class Settings : Immutable
        {
            public static readonly Settings DEFAULT = new Settings().ChangeMassResolution(.01).ChangeMinAbundance(.00001)
                .ChangeIsotopeAbundances(IsotopeEnrichmentsList.DEFAULT.IsotopeAbundances);

            public static Settings FromSrmSettings(SrmSettings srmSettings)
            {
                return DEFAULT.ChangeIsotopeAbundances(srmSettings.TransitionSettings.FullScan.IsotopeAbundances);
            }
            public double MassResolution { get; private set; }

            public Settings ChangeMassResolution(double massResolution)
            {
                return ChangeProp(ImClone(this), im => im.MassResolution = massResolution);
            }
            public double MinAbundance { get; private set; }

            public Settings ChangeMinAbundance(double minAbundance)
            {
                return ChangeProp(ImClone(this), im => im.MinAbundance = minAbundance);
            }
            public IsotopeAbundances IsotopeAbundances { get; private set; }

            public Settings ChangeIsotopeAbundances(IsotopeAbundances isotopeAbundances)
            {
                return ChangeProp(ImClone(this), im => im.IsotopeAbundances = isotopeAbundances ?? DEFAULT.IsotopeAbundances);
            }

            public MassDistribution GetMassDistribution(Molecule molecule, double massShift, int charge)
            {
                var massDistribution = new MassDistribution(MassResolution, MinAbundance);
                foreach (var entry in molecule)
                {
                    massDistribution = massDistribution.Add(IsotopeAbundances[entry.Key].Multiply(entry.Value));
                }
                if (charge != 0)
                {
                    massDistribution = massDistribution.OffsetAndDivide(massShift - charge * BioMassCalc.MassElectron,
                        Math.Abs(charge));
                }
                return massDistribution;
            }

            public double GetMonoMass(Molecule molecule, double massShift, int charge)
            {
                var massDistribution = ChangeIsotopeAbundances(GetMonoisotopicAbundances(IsotopeAbundances))
                    .GetMassDistribution(molecule, massShift, charge);
                return massDistribution.MostAbundanceMass;
            }

            protected bool Equals(Settings other)
            {
                return MassResolution.Equals(other.MassResolution) && MinAbundance.Equals(other.MinAbundance) &&
                       Equals(IsotopeAbundances, other.IsotopeAbundances);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Settings) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = MassResolution.GetHashCode();
                    hashCode = (hashCode * 397) ^ MinAbundance.GetHashCode();
                    hashCode = (hashCode * 397) ^ (IsotopeAbundances != null ? IsotopeAbundances.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public Settings MakeMonoisotopic()
            {
                return ChangeIsotopeAbundances(GetMonoisotopicAbundances(IsotopeAbundances));
            }
        }

        private static IsotopeAbundances GetMonoisotopicAbundances(IsotopeAbundances isotopeAbundances)
        {
            var newAbundances = new Dictionary<string, MassDistribution>();
            foreach (var entry in isotopeAbundances)
            {
                newAbundances.Add(entry.Key, new MassDistribution(entry.Value.MassResolution, entry.Value.MinimumAbundance)
                    .SetAbundance(entry.Value.MostAbundanceMass, 1));
            }
            return isotopeAbundances.SetAbundances(newAbundances);
        }

        private static Molecule Intern(Molecule molecule)
        {
            return Molecule.FromDict(molecule.ToDictionary(kvp=>string.Intern(kvp.Key), kvp=>kvp.Value));
        }
    }
}
