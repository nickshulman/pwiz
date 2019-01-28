using System;
using System.Collections.Generic;

namespace pwiz.Skyline.Model.XCorr
{
    public class Spectrum
    {
        public Spectrum(IList<double> masses, IList<float> intensities)
        {
            Masses = masses;
            Intensities = intensities;
        }
        public IList<double> Masses { get; private set; }
        public IList<float> Intensities { get; private set; }
    }
}
