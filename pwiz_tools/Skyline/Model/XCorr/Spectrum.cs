using System;

namespace pwiz.Skyline.Model.XCorr
{
    public interface Spectrum
    {
        String getSpectrumName();
        float getScanStartTime();
        double getPrecursorMZ();
        double[] getMassArray();
        float[] getIntensityArray();
        float getTIC();
    }
}
