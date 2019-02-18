using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.XCorr
{
    public class SparseXCorrSpectrum
    {
        private readonly float fragmentBinSize;
        private readonly int[] indices;
        private readonly double[] masses;
        private readonly float[] intensities;
        private readonly int length;
        private readonly double precursorMz;

    	public SparseXCorrSpectrum(SparseIndexMap map, double precursorMz, float fragmentBinSize, int length)
        {
            this.precursorMz = precursorMz;
            this.fragmentBinSize = fragmentBinSize;
            this.length = length;
            
            List<Tuple<int, double, float>> peaks= new List<Tuple<int, double, float>>();
            peaks.AddRange(map.Select(entry=>Tuple.Create(entry.Key, entry.Value.Mass, (float) entry.Value.Intensity)));
            peaks.Sort();
		
		indices=new int[peaks.Count];
		masses=new double[peaks.Count];
		intensities=new float[peaks.Count];
		for (int i=0; i<indices.Length; i++) {
			var tuple = peaks[i];
        indices[i]=tuple.Item1;
			masses[i]=tuple.Item2;
			intensities[i]=tuple.Item3;
		}
}
    public float getScanStartTime()
{
    return 0;
}
    public String getSpectrumName()
{
    return "Precursor MZ: " + precursorMz;
}
    public float getTIC()
    {
        return intensities.Sum();
    }

public double getPrecursorMZ()
{
    return precursorMz;
}

public float getFragmentBinSize()
{
    return fragmentBinSize;
}

public int[] getIndices()
{
    return indices;
}

public float[] getIntensityArray()
{
    return intensities;
}

public double[] getMassArray()
{
    return masses;
}

public int Length
{
    get { return length; }
}

public float[] toArray()
{
    float[] array = new float[length];
    for (int i = 0; i < indices.Length; i++)
    {
        array[indices[i]] = intensities[i];
    }
    return array;
}


	public float dotProduct(SparseXCorrSpectrum spectrum)
{
    int i = 0;
    int j = 0;
    float dotProduct = 0.0f;
    while (i < indices.Length && j < spectrum.indices.Length)
    {
        if (indices[i] == spectrum.indices[j])
        {
            dotProduct += intensities[i] * spectrum.intensities[j];
            i++;
            j++;
        }
        else if (indices[i] > spectrum.indices[j])
        {
            j++;
        }
        else
        {
            i++;
        }
    }
    return dotProduct;
}

public float dotProduct(SparseXCorrSpectrum spectrum, int offset)
{
    int i = 0;
    int j = 0;
    float dotProduct = 0.0f;
    while (i < indices.Length && j < spectrum.indices.Length)
    {
        int spectrumIndex = spectrum.indices[j] + offset;
        if (indices[i] == spectrumIndex)
        {
            dotProduct += intensities[i] * spectrum.intensities[j];
            i++;
            j++;
        }
        else if (indices[i] > spectrumIndex)
        {
            j++;
        }
        else
        {
            i++;
        }
    }
    return dotProduct;
}

    }
}
