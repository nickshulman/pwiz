using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.XCorr
{
    public class SparseXCorrCalculator
    {
        public const double biggestFragmentMass = 2000.0;

        private readonly SearchParameters searchParameters;
        private readonly SparseXCorrSpectrum preprocessedSpectrum;
	
	    /**
	     * 
	     * @param s assumes sqrted intensities!
	     * @param precursorMz
	     * @param charge
	     * @param searchParameters
	     */
	    public SparseXCorrCalculator(Spectrum s, Tuple<double, double> precursorMz, SearchParameters searchParameters) 
	        : this(normalize(s, precursorMz, false, searchParameters), searchParameters)
        {
        }

        public SparseXCorrCalculator(PeptideDocNode modifiedSequence, double precursorMz, byte precursorCharge, SearchParameters searchParameters)
            : this(getTheoreticalSpectrum(modifiedSequence, precursorMz, precursorCharge, searchParameters), searchParameters)
        {
        }

        public SparseXCorrCalculator(SparseXCorrSpectrum spectrum, SearchParameters searchParameters)
        {
            this.searchParameters=searchParameters;
            preprocessedSpectrum = preprocessSpectrum(spectrum);
        }

        /**
         * 
         * @param s assumes sqrted intensities!
         * @return
         */
        public float score(Spectrum s, Tuple<double, double> precursorMz)
        {
            SparseXCorrSpectrum intensityBins = normalize(s, precursorMz);
            return score(intensityBins);
        }

        public SparseXCorrSpectrum normalize(Spectrum s, Tuple<double, double> precursorMz)
        {
            return normalize(s, precursorMz, false, searchParameters);
        }

        public float score(PeptideDocNode modifiedSequence, double precursorMz, byte precursorCharge)
        {
            SparseXCorrSpectrum intensityBins = getTheoreticalSpectrum(modifiedSequence, precursorMz, precursorCharge, searchParameters);
            return score(intensityBins);
        }

        public float score(SparseXCorrSpectrum spectrum)
        {
            // divide by 1e4 (personal communication with J Eng)
            return preprocessedSpectrum.dotProduct(spectrum) / 1.0e4f;
        }

        static float dotProduct(float[] preprocessedSpectrum, float[] spectrum)
        {
            float sum = 0.0f;
            for (int i = 0; i < spectrum.Length; i++)
            {
                if (i >= preprocessedSpectrum.Length) break;
                sum += spectrum[i] * preprocessedSpectrum[i];
            }
            return sum;
        }

        static float dotProduct(float[] preprocessedSpectrum, float[] spectrum, int offset)
        {
            float sum = 0.0f;
            for (int i = 0; i < spectrum.Length; i++)
            {
                int index = i + offset;
                if (index < 0 || index >= preprocessedSpectrum.Length) continue;
                sum += spectrum[i] * preprocessedSpectrum[index];
            }
            return sum;
        }

        static SparseXCorrSpectrum preprocessSpectrum(SparseXCorrSpectrum spectrum)
        {
            RandomSparseIndexMap preprocessedSpectrum = new RandomSparseIndexMap();

            int length = spectrum.Length;
            int[] indicies = spectrum.getIndices();
            double[] masses = spectrum.getMassArray();
            float[] intensities = spectrum.getIntensityArray();
            float[] negativeIntensities = new float[intensities.Length];
            for (int i = 0; i < negativeIntensities.Length; i++)
            {
                negativeIntensities[i] = -intensities[i];
            }

            for (int offset = ArrayXCorrCalculator.lowerOffset; offset < ArrayXCorrCalculator.upperOffset; offset++)
            {
                if (offset == 0) continue;
                for (int i = 0; i < indicies.Length; i++)
                {
                    int index = indicies[i] + offset;

                    if (index >= 0 && index < length)
                    {
                        preprocessedSpectrum.AdjustOrPutValue(index, masses[i] + offset * spectrum.getFragmentBinSize(), negativeIntensities[i]);
                    }
                }
            }

            int denominator = ArrayXCorrCalculator.upperOffset - ArrayXCorrCalculator.lowerOffset;
            preprocessedSpectrum.MultiplyAllValues(1.0f / denominator);

            for (int i = 0; i < indicies.Length; i++)
            {
                preprocessedSpectrum.AdjustOrPutValue(indicies[i], masses[i], intensities[i]);
            }

            return new SparseXCorrSpectrum(preprocessedSpectrum, spectrum.getPrecursorMZ(), spectrum.getFragmentBinSize(), length);
        }

        /**
         * see Eng et al, JASMS 1994
         * @param s
         * @param precursorMz
         * @return
         */
        public static SparseXCorrSpectrum normalize(Spectrum s, Tuple<double, double> precursorMz, bool addIntensityToNeighboringBins, SearchParameters searchParameters)
        {

            var masses = s.Masses;
            var intensities = s.Intensities;
            List<Peak> allPeaks = new List<Peak>();
            double avgPrecursorMz = (precursorMz.Item1 + precursorMz.Item2) / 2;
            if (masses.Count == 0)
                return getIntensityArray(searchParameters, allPeaks, avgPrecursorMz, addIntensityToNeighboringBins);
            if (masses.Count == 1)
            {
                allPeaks.Add(new Peak(masses[0], ArrayXCorrCalculator.primaryIonIntensity));
                return getIntensityArray(searchParameters, allPeaks, avgPrecursorMz, addIntensityToNeighboringBins);
            }

            double minimumPrecursorRemoved = precursorMz.Item1;
            double maximumPrecursorRemoved = precursorMz.Item2;

            double firstMass = masses[0];
            double lastMass = masses[masses.Count - 1];

            double increment = (lastMass - firstMass) / ArrayXCorrCalculator.groups;
            double[] binMaxMass = new double[ArrayXCorrCalculator.groups];
            for (int i = 0; i < ArrayXCorrCalculator.groups - 1; i++)
            {
                binMaxMass[i] = increment * (i + 1);
            }
            binMaxMass[ArrayXCorrCalculator.groups - 1] = Double.MaxValue;

            float[] binMaxIntensity = new float[ArrayXCorrCalculator.groups];
            int currentIndex = 0;
            for (int i = 0; i < intensities.Count; i++)
            {
                if (intensities[i] <= 0.0f || masses[i] > minimumPrecursorRemoved && masses[i] < maximumPrecursorRemoved)
                {
                    continue;
                }

                while (masses[i] > binMaxMass[currentIndex])
                {
                    currentIndex++;
                }

                if (intensities[i] > binMaxIntensity[currentIndex])
                {
                    binMaxIntensity[currentIndex] = intensities[i];
                }
            }

            binMaxIntensity = binMaxIntensity.Select(x => x / ArrayXCorrCalculator.primaryIonIntensity).ToArray();

            currentIndex = 0;
            for (int i = 0; i < intensities.Count; i++)
            {
                if (intensities[i] <= 0.0f || (masses[i] > minimumPrecursorRemoved && masses[i] < maximumPrecursorRemoved))
                {
                    continue;
                }

                while (masses[i] > binMaxMass[currentIndex])
                {
                    currentIndex++;
                }
                allPeaks.Add(new Peak(masses[i], intensities[i] / binMaxIntensity[currentIndex]));
            }

            return getIntensityArray(searchParameters, allPeaks, avgPrecursorMz, addIntensityToNeighboringBins);
        }

        public static SparseXCorrSpectrum getTheoreticalSpectrum(PeptideDocNode modifiedSequence, double precursorMz, byte precursorCharge, SearchParameters searchParameters)
        {

            FragmentationType type =searchParameters.FragmentationType;

            List<Peak> allPeaks = new List<Peak>();
            switch (type)
            {
                case FragmentationType.HCD:
                    FragmentIon[] yIons = ArrayXCorrCalculator.GetFragmentIons(modifiedSequence, IonType.y).ToArray();
                    allPeaks.AddRange(getPeaks(yIons, 0.0, ArrayXCorrCalculator.primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(yIons, -MassConstants.nh3, ArrayXCorrCalculator.neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(yIons, -MassConstants.oh2, ArrayXCorrCalculator.neutralLossIntensity));
                    }
                    break;

                case FragmentationType.CID:
                    FragmentIon[] yIonsCID = ArrayXCorrCalculator.GetFragmentIons(modifiedSequence, IonType.y).ToArray();
                    allPeaks.AddRange(getPeaks(yIonsCID, 0.0, ArrayXCorrCalculator.primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(yIonsCID, -MassConstants.nh3, ArrayXCorrCalculator.neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(yIonsCID, -MassConstants.oh2, ArrayXCorrCalculator.neutralLossIntensity));
                    }

                    FragmentIon[] bIonsCID = ArrayXCorrCalculator.GetFragmentIons(modifiedSequence, IonType.b).ToArray();
                    allPeaks.AddRange(getPeaks(bIonsCID, 0.0, ArrayXCorrCalculator.primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(bIonsCID, -MassConstants.nh3, ArrayXCorrCalculator.neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(bIonsCID, -MassConstants.oh2, ArrayXCorrCalculator.neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(bIonsCID, -MassConstants.co, ArrayXCorrCalculator.neutralLossIntensity));
                    }
                    break;

                case FragmentationType.ETD:
                    FragmentIon[] cIonsCID = ArrayXCorrCalculator.GetFragmentIons(modifiedSequence, IonType.c).ToArray();
                    allPeaks.AddRange(getPeaks(cIonsCID, 0.0, ArrayXCorrCalculator.primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(cIonsCID, -MassConstants.nh3, ArrayXCorrCalculator.neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(cIonsCID, -MassConstants.oh2, ArrayXCorrCalculator.neutralLossIntensity));
                    }

                    FragmentIon[] zIonsCID = ArrayXCorrCalculator.GetFragmentIons(modifiedSequence, IonType.z).ToArray();
                    allPeaks.AddRange(getPeaks(zIonsCID, 0.0, ArrayXCorrCalculator.primaryIonIntensity));
                    allPeaks.AddRange(getPeaks(zIonsCID, MassConstants.neutronMass, ArrayXCorrCalculator.primaryIonIntensity)); // z+1
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(zIonsCID, -MassConstants.nh3, ArrayXCorrCalculator.neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(zIonsCID, -MassConstants.oh2, ArrayXCorrCalculator.neutralLossIntensity));
                    }
                    break;

                default:
                    throw new ApplicationException("Unknown fragmentation type [" + type + "]");
            }

            return getIntensityArray(searchParameters, allPeaks, precursorMz, true);
        }

        private static List<Peak> getPeaks(FragmentIon[] ions, double delta, float intensity)
        {
            List<Peak> peaks = new List<Peak>();
            for (int i = 0; i < ions.Length; i++)
            {
                peaks.Add(new Peak(ions[i].Mass + delta, intensity));
            }
            return peaks;
        }

        private static SparseXCorrSpectrum getIntensityArray(SearchParameters searchParameters, IEnumerable<Peak> peaks, double precursorMz, bool addIntensityToNeighboringBins)
        {
            var allPeaks = CollectionUtil.EnsureSorted(peaks, Peak.MASS_COMPARER);

            // set tolerance to 2x the fragment tolerance of the highest fragment
            float fragmentBinSize = 2.0f * (float)searchParameters.FragmentTolerance.GetTolerance(biggestFragmentMass);
            double offset;

            if (fragmentBinSize > 0.5f)
            {
                fragmentBinSize = ArrayXCorrCalculator.lowResFragmentBinSize; // if tolerance is >0.25 Da, then jump to 1 Da to make use of the average amino acid mass defect
                offset = ArrayXCorrCalculator.lowResFragmentBinOffset;
            }
            else if (fragmentBinSize < 0.01f)
            {
                fragmentBinSize = 0.01f;
                offset = 0.0;
            }
            else
            {
                offset = 0.0;
            }

            float inverseBinWidth = 1.0f / fragmentBinSize;
            int arraySize = (int)((biggestFragmentMass + fragmentBinSize + 2.0) * inverseBinWidth);

            OrderedSparseIndexMap binnedIntensityArray = new OrderedSparseIndexMap(allPeaks.Length);
            int arraySizeMinusOne = arraySize - 1;
            foreach (Peak peak in allPeaks)
            {
                int massIndex = (int)((peak.Mass - offset) * inverseBinWidth);

                if (massIndex < 0) massIndex = 0;
                if (massIndex >= arraySize) massIndex = arraySize - 1;

                // don't do this for low res fragment ions bin boundaries aren't an issue with the 0.4 offset
                bool addToNeighbors = fragmentBinSize <= 0.5f && addIntensityToNeighboringBins;
                float neighboringIntensity = (float)(peak.Intensity > ArrayXCorrCalculator.neutralLossIntensity ? peak.Intensity / 2.0f : peak.Intensity);
                if (addToNeighbors)
                {
                    if (massIndex > 0)
                    {
                        binnedIntensityArray.PutIfGreater(massIndex - 1, peak.Mass - fragmentBinSize, neighboringIntensity);
                    }
                }
                binnedIntensityArray.PutIfGreater(massIndex, peak.Mass, peak.Intensity);
                if (addToNeighbors)
                {
                    if (massIndex < arraySizeMinusOne)
                    {
                        binnedIntensityArray.PutIfGreater(massIndex + 1, peak.Mass + fragmentBinSize, neighboringIntensity);
                    }
                }
            }
            return new SparseXCorrSpectrum(binnedIntensityArray, precursorMz, fragmentBinSize, arraySize);
        }

    }
}
