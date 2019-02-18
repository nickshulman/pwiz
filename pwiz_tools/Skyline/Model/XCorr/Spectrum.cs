using System.Collections.Generic;

namespace pwiz.Skyline.Model.XCorr
{
    public class Spectrum
    {
        public Spectrum(double precursorMz, IList<double> masses, IList<float> intensities)
        {
            PrecursorMz = precursorMz;
            Masses = masses;
            Intensities = intensities;
        }
        public double PrecursorMz { get; private set; }
        public IList<double> Masses { get; private set; }
        public IList<float> Intensities { get; private set; }
    }
}
