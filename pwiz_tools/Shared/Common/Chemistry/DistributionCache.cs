using System;
using System.Collections.Generic;

namespace pwiz.Common.Chemistry
{
    public class DistributionCache
    {
        private readonly IDictionary<Molecule, MassDistribution> _massDistributions 
            = new Dictionary<Molecule, MassDistribution>();
        private readonly IDictionary<Molecule, double> _monoMasses 
            = new Dictionary<Molecule, double>();

        private readonly IDictionary<Tuple<string, int>, MassDistribution> _elementDistributions 
            = new Dictionary<Tuple<string, int>, MassDistribution>();

        private readonly DistributionSettings _monoDistributionSettings;

        public DistributionCache(DistributionSettings distributionSettings)
        {
            Settings = distributionSettings;
            _monoDistributionSettings = Settings.ChangeIsotopeAbundances(
                GetMonoisotopicAbundances(Settings.IsotopeAbundances));
        }

        public DistributionSettings Settings { get; private set; }

        public MassDistribution GetMzDistribution(Molecule formula, double massShift, int charge)
        {
            var massDistribution = GetMassDistribution(formula);
            if (charge == 0)
            {
                if (massShift != 0)
                {
                    massDistribution = massDistribution.OffsetAndDivide(massShift, 1);
                }
            }
            else
            {
                massDistribution =
                    massDistribution.OffsetAndDivide(massShift - charge * Settings.MassElectron, Math.Abs(charge));
            }
            return massDistribution;
        }

        public MassDistribution GetMassDistribution(Molecule formula)
        {
            MassDistribution massDistribution;
            lock (_massDistributions)
            {
                if (_massDistributions.TryGetValue(formula, out massDistribution))
                {
                    return massDistribution;
                }
            }
            massDistribution = Settings.EmptyDistribution;
            foreach (var entry in formula)
            {
                massDistribution = massDistribution.Add(GetElementDistribution(entry.Key, entry.Value));
            }
            lock (_massDistributions)
            {
                _massDistributions[formula] = massDistribution;
            }
            return massDistribution;
        }

        private MassDistribution GetElementDistribution(string element, int count)
        {
            if (count == 0)
            {
                return Settings.EmptyDistribution;
            }
            var key = Tuple.Create(element, count);
            MassDistribution result;
            lock (_elementDistributions)
            {
                if (_elementDistributions.TryGetValue(key, out result))
                {
                    return result;
                }
            }
            if (count == 1)
            {
                result = Settings.EmptyDistribution.Add(Settings.IsotopeAbundances[element]);
            }
            else
            {
                result = Settings.EmptyDistribution.Add(GetElementDistribution(element, count / 2)).Multiply(2);
                if (count % 2 != 0)
                {
                    result = result.Add(GetElementDistribution(element, 1));
                }
            }
            lock (_elementDistributions)
            {
                _elementDistributions[key] = result;
            }
            return result;
        }

        public double GetMonoMass(Molecule formula)
        {
            double monoMass;
            lock (_monoMasses)
            {
                if (_monoMasses.TryGetValue(formula, out monoMass))
                {
                    return monoMass;
                }
            }
            monoMass = _monoDistributionSettings.GetMassDistribution(formula).MostAbundanceMass;
            lock (_monoMasses)
            {
                _monoMasses[formula] = monoMass;
            }
            return monoMass;
        }

        public double GetMonoMz(Molecule formula, double massShift, int charge)
        {
            double mass = GetMonoMass(formula);
            mass += massShift;
            if (charge != 0)
            {
                mass -= charge * Settings.MassElectron;
                mass /= Math.Abs(charge);
            }
            return mass;
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

    }
}
