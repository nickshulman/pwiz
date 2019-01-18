using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.XCorr
{
    public class MassConstants
    {
        public const double neutronMass = 1.0086649158849;
        public const double protonMass = 1.00727646681290;
        public const double hydrogenMass = 1.007825032071;
        public const double carbonMass = 12.0000000000000;
        public const double oxygenMass = 15.9949146195616;
        public const double nitrogenMass = 14.00307400486;
        public const double oh2 = oxygenMass + 2 * hydrogenMass;
        public const double nh3 = nitrogenMass + 3 * hydrogenMass;
        public const double co = carbonMass + oxygenMass;

        public static double getPeptideMass(double chargedMass, byte charge)
        {
            return chargedMass * charge - protonMass * charge;
        }

        public static double getChargedIsotopeMass(double precursorMz, byte charge, byte isotope)
        {
            return precursorMz + (isotope * neutronMass / charge);
        }
    }
}
