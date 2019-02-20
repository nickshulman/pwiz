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

        public SparseXCorrSpectrum(ISparseIndexMap map, double precursorMz, float fragmentBinSize, int length)
        {
            this.precursorMz = precursorMz;
            this.fragmentBinSize = fragmentBinSize;
            this.length = length;

            indices = new int[map.Count];
            masses = new double[map.Count];
            intensities = new float[map.Count];
            int index = 0;
            foreach (var entry in map.OrderedEnumerable)
            {
                indices[index] = entry.Key;
                masses[index] = entry.Value.Mass;
                intensities[index] = (float) entry.Value.Intensity;
                index++;
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
