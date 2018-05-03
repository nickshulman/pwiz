using pwiz.Common.SystemUtil;

namespace pwiz.Common.Chemistry
{
    public class DistributionSettings : Immutable
    {
        public static readonly DistributionSettings DEFAULT 
            = new DistributionSettings(0.001, 0.0001, IsotopeAbundances.Default);
        public DistributionSettings(double massResolution, double minAbundance, IsotopeAbundances isotopeAbundances)
        {
            MassResolution = massResolution;
            MinAbundance = minAbundance;
            IsotopeAbundances = isotopeAbundances;
            MassElectron = 0.00054857990946;
        }
        public double MinAbundance { get; private set; }

        public DistributionSettings ChangeMinAbundance(double minAbundance)
        {
            return ChangeProp(ImClone(this), im => im.MinAbundance = minAbundance);
        }
        public double MassResolution { get; private set; }

        public DistributionSettings ChangeMassResolution(double massResolution)
        {
            return ChangeProp(ImClone(this), im => im.MassResolution = massResolution);
        }
        public IsotopeAbundances IsotopeAbundances { get; private set; }

        public DistributionSettings ChangeIsotopeAbundances(IsotopeAbundances isotopeAbundances)
        {
            return ChangeProp(ImClone(this), im => im.IsotopeAbundances = isotopeAbundances ?? DEFAULT.IsotopeAbundances);
        }
        public double MassElectron { get; private set; }

        public DistributionSettings ChangeMassElectron(double massElectron)
        {
            return ChangeProp(ImClone(this), im => im.MassElectron = massElectron);
        }

        public MassDistribution GetMassDistribution(Molecule molecule)
        {
            var massDistribution = new MassDistribution(MassResolution, MinAbundance);
            foreach (var entry in molecule)
            {
                massDistribution = massDistribution.Add(IsotopeAbundances[entry.Key].Multiply(entry.Value));
            }
            return massDistribution;
        }
    }
}
