using System.Collections.Generic;

namespace pwiz.Skyline.Model.XCorr
{
    public struct Peak
    {
        public Peak(double mass, double intensity)
        {
            Mass = mass;
            Intensity = intensity;
        }
        public double Mass { get; private set; }
        public double Intensity { get; private set; }

        public static readonly IComparer<Peak> MASS_COMPARER =
            Comparer<Peak>.Create((p1, p2) => p1.Mass.CompareTo(p2.Mass));
    }
}
