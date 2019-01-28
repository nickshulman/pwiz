using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.XCorr
{
    public class ArrayXCorrCalculator
    {
        // values from personal communication with J. Egertson
        const float lowResFragmentBinSize = 1.00045475f;
        const float lowResFragmentBinOffset = 0.4f;

        // set 50 to be the maximum value, see pp 982 bottom right
        const float primaryIonIntensity = 50.0f;
        const float neutralLossIntensity = primaryIonIntensity / 5;

        // offset defined figure legend on pp 980
        const int upperOffset = 75;
        const int lowerOffset = -upperOffset;

        // divide spectrum into 10 equal regions, see pp 982 bottom right
        const int groups = 10;

        // remove 10-u window around precursor, see pp 979 mid left
        static double precursorRemovalMargin = 5.0;

        private readonly SearchParameters searchParameters;
        private readonly byte charge;
        private readonly double precursorMz;
        private readonly float[] preprocessedSpectrum;

        /**
         * 
         * @param s assumes sqrted intensities!
         * @param precursorMz
         * @param charge
         * @param params
         */
        public ArrayXCorrCalculator(Spectrum s, double precursorMz, byte charge, SearchParameters searchParameters)
        {
            this.precursorMz = precursorMz;
            this.charge = charge;
            this.searchParameters=searchParameters;
            preprocessedSpectrum = preprocessSpectrum(normalize(s, precursorMz, charge, false, searchParameters));
        }

        public ArrayXCorrCalculator(PeptideDocNode peptide, double precursorMz, byte charge, SearchParameters searchParameters)
        {
            this.precursorMz = precursorMz;
            this.charge = charge;
            this.searchParameters=searchParameters;
            preprocessedSpectrum = preprocessSpectrum(getTheoreticalSpectrum(peptide, precursorMz, charge, searchParameters));
        }

        /**
         * 
         * @param s assumes sqrted intensities!
         * @return
         */
        public float score(Spectrum s)
        {
            float[] intensityBins = normalize(s);
            return score(intensityBins);
        }

        public float[] normalize(Spectrum s)
        {
            return normalize(s, precursorMz, charge, false, searchParameters);
        }

        public float score(PeptideDocNode modifiedSequence)
        {
            float[] intensityBins = getTheoreticalSpectrum(modifiedSequence, precursorMz, charge, searchParameters);
            return score(intensityBins);
        }

        /**
         * divide by 1e4 (personal communication with J Eng)
         * @param spectrum
         * @return
         */
        float score(float[] spectrum)
        {
            return dotProduct(preprocessedSpectrum, spectrum) / 1.0e4f;
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

        static float[] preprocessSpectrum(float[] spectrum)
        {
            float[] preprocessedSpectrum = new float[spectrum.Length];

            for (int offset = lowerOffset; offset < upperOffset; offset++)
            {
                if (offset == 0) continue;
                for (int i = 0; i < spectrum.Length; i++)
                {
                    int index = i + offset;
                    if (index >= 0 && index < preprocessedSpectrum.Length)
                    {
                        preprocessedSpectrum[i] -= spectrum[index];
                    }
                }
            }

            int denominator = upperOffset - lowerOffset;
            for (int i = 0; i < preprocessedSpectrum.Length; i++)
            {
                preprocessedSpectrum[i] = preprocessedSpectrum[i] / denominator;
            }

            for (int i = 0; i < spectrum.Length; i++)
            {
                preprocessedSpectrum[i] += spectrum[i];
            }
            return preprocessedSpectrum;
        }

        /**
         * see Eng et al, JASMS 1994
         * @param s
         * @param precursorMz
         * @return
         */
        static float[] normalize(Spectrum s, double precursorMz, byte charge, bool addIntensityToNeighboringBins, SearchParameters searchParameters)
        {
            double massPlusOne = precursorMz * charge - (charge - 1) * AminoAcidFormulas.ProtonMass;

            IList<double> masses = s.Masses;
            IList<float> intensities = s.Intensities;
            List<Peak> allPeaks = new List<Peak>();
            if (masses.Count == 0)
                return getIntensityArray(searchParameters, allPeaks, massPlusOne, addIntensityToNeighboringBins);
            if (masses.Count == 1)
            {
                allPeaks.Add(new Peak(masses[0], primaryIonIntensity));
                return getIntensityArray(searchParameters, allPeaks, massPlusOne, addIntensityToNeighboringBins);
            }

            double minimumPrecursorRemoved = precursorMz - precursorRemovalMargin;
            double maximumPrecursorRemoved = precursorMz + precursorRemovalMargin;

            double firstMass = masses[0];
            double lastMass = masses[masses.Count - 1];

            double increment = (lastMass - firstMass) / groups;
            double[] binMaxMass = new double[groups];
            for (int i = 0; i < groups - 1; i++)
            {
                binMaxMass[i] = increment * (i + 1);
            }
            binMaxMass[groups - 1] = Double.MaxValue;

            float[] binMaxIntensity = new float[groups];
            int currentIndex = 0;
            for (int i = 0; i < intensities.Count; i++)
            {
                if (masses[i] > minimumPrecursorRemoved && masses[i] < maximumPrecursorRemoved)
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

            binMaxIntensity = binMaxIntensity.Select(v => v / primaryIonIntensity).ToArray();

            currentIndex = 0;
            for (int i = 0; i < intensities.Count; i++)
            {
                if (masses[i] > minimumPrecursorRemoved && masses[i] < maximumPrecursorRemoved)
                {
                    continue;
                }

                while (masses[i] > binMaxMass[currentIndex])
                {
                    currentIndex++;
                }
                allPeaks.Add(new Peak(masses[i], intensities[i] / binMaxIntensity[currentIndex]));
            }

            return getIntensityArray(searchParameters, allPeaks, massPlusOne, addIntensityToNeighboringBins);
        }

        public static float[] getTheoreticalSpectrum(PeptideDocNode peptide, double precursorMz, byte charge, SearchParameters searchParameters)
        {
            double massPlusOne = precursorMz * charge - (charge - 1) * AminoAcidFormulas.ProtonMass;

            FragmentationType type =searchParameters.FragmentationType;

            List<Peak> allPeaks = new List<Peak>();
            switch (type)
            {
                case FragmentationType.HCD:
                    FragmentIon[] yIons = GetFragmentIons(peptide, IonType.y).ToArray();
                    allPeaks.AddRange(getPeaks(yIons, 0.0, primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(yIons, -MassConstants.nh3, neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(yIons, -MassConstants.oh2, neutralLossIntensity));
                    }
                    break;

                case FragmentationType.CID:
                    FragmentIon[] yIonsCID = GetFragmentIons(peptide, IonType.y).ToArray();
                    allPeaks.AddRange(getPeaks(yIonsCID, 0.0, primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(yIonsCID, -MassConstants.nh3, neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(yIonsCID, -MassConstants.oh2, neutralLossIntensity));
                    }

                    FragmentIon[] bIonsCID = GetFragmentIons(peptide, IonType.b).ToArray();
                    allPeaks.AddRange(getPeaks(bIonsCID, 0.0, primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(bIonsCID, -MassConstants.nh3, neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(bIonsCID, -MassConstants.oh2, neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(bIonsCID, -MassConstants.co, neutralLossIntensity));
                    }
                    break;

                case FragmentationType.ETD:
                    FragmentIon[] cIonsCID = GetFragmentIons(peptide, IonType.c).ToArray();
                    allPeaks.AddRange(getPeaks(cIonsCID, 0.0, primaryIonIntensity));
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(cIonsCID, -MassConstants.nh3, neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(cIonsCID, -MassConstants.oh2, neutralLossIntensity));
                    }

                    FragmentIon[] zIonsCID = GetFragmentIons(peptide, IonType.z).ToArray();
                    allPeaks.AddRange(getPeaks(zIonsCID, 0.0, primaryIonIntensity));
                    allPeaks.AddRange(getPeaks(zIonsCID, MassConstants.neutronMass, primaryIonIntensity)); // z+1
                    if (searchParameters.UseNLsForXCorr) {
                        allPeaks.AddRange(getPeaks(zIonsCID, -MassConstants.nh3, neutralLossIntensity));
                        allPeaks.AddRange(getPeaks(zIonsCID, -MassConstants.oh2, neutralLossIntensity));
                    }
                    break;

                default:
                    throw new ApplicationException("Unknown fragmentation type [" + type + "]");
            }

            return getIntensityArray(searchParameters, allPeaks, massPlusOne, true);
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

        private static float[] getIntensityArray(SearchParameters searchParameters, IEnumerable<Peak> peaks, double massPlusOne, bool addIntensityToNeighboringBins)
        {
            var allPeaks = peaks.OrderBy(p=>p.Mass).ToArray();

            // set tolerance to 2x the fragment tolerance of the highest fragment
            float fragmentBinSize = 2.0f * (float)searchParameters.FragmentTolerance.GetTolerance(massPlusOne);
            double offset;

            if (fragmentBinSize > 0.5f)
            {
                fragmentBinSize = lowResFragmentBinSize; // if tolerance is >0.25 Da, then jump to 1 Da to make use of the average amino acid mass defect
                offset = lowResFragmentBinOffset;
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
            int arraySize = (int)((massPlusOne + fragmentBinSize + 2.0) * inverseBinWidth);

            float[] binnedIntensityArray = new float[arraySize];
            int arraySizeMinusOne = arraySize - 1;
            foreach (Peak peak in allPeaks)
            {
                int massIndex = (int)((peak.Mass - offset) * inverseBinWidth);

                if (massIndex < 0) massIndex = 0;
                if (massIndex >= arraySize) massIndex = arraySize - 1;
                if (binnedIntensityArray[massIndex] < peak.Intensity)
                {
                    binnedIntensityArray[massIndex] = (float) peak.Intensity;
                }

                // don't do this for low res fragment ions bin boundaries aren't an issue with the 0.4 offset
                if (fragmentBinSize <= 0.5f && addIntensityToNeighboringBins)
                {
                    // neighboring intensities are 25 for b/y or 10 (the same) for neutral losses
                    float neighboringIntensity = (float) (peak.Intensity > neutralLossIntensity ? peak.Intensity / 2.0f : peak.Intensity);
                    if (massIndex > 0)
                    {
                        binnedIntensityArray[massIndex - 1] = neighboringIntensity;
                    }
                    if (massIndex < arraySizeMinusOne)
                    {
                        binnedIntensityArray[massIndex + 1] = neighboringIntensity;
                    }
                }
            }
            return binnedIntensityArray;
        }

        public static IEnumerable<FragmentIon> GetFragmentIons(PeptideDocNode peptideDocNode, IonType ionType)
        {
            var settings = SrmSettingsList.GetDefault();
            var transitionFilter =
                settings.TransitionSettings.Filter
                    .ChangePeptideProductCharges(ImmutableList.Singleton(Adduct.SINGLY_PROTONATED))
                    .ChangePeptideIonTypes(ImmutableList.Singleton(ionType))
                    .ChangeFragmentRangeFirstName(TransitionFilter.StartFragmentFinder.ION_1.Name)
                    .ChangeFragmentRangeLastName(@"last ion");
            settings = settings.ChangeTransitionSettings(settings.TransitionSettings.ChangeFilter(transitionFilter));
            var transitionGroup = new TransitionGroup(peptideDocNode.Peptide, Adduct.SINGLY_PROTONATED,
                IsotopeLabelType.light);
            var transitionGroupDocNode = new TransitionGroupDocNode(transitionGroup, Annotations.EMPTY, settings,
                peptideDocNode.ExplicitMods, null, null, null, new TransitionDocNode[0], false);
            var precursor =
                FragmentedMolecule.GetFragmentedMolecule(settings, peptideDocNode, transitionGroupDocNode, null)
                    .ChangeFragmentCharge(1);
            
            var fragmentedMoleculeSettings = FragmentedMolecule.Settings.DEFAULT;
            for (int ordinal = 1; ordinal <= peptideDocNode.Peptide.Sequence.Length; ordinal++)
            {
                var fragment = precursor.ChangeFragmentIon(ionType, ordinal).FragmentFormula;
                var monoMass = fragmentedMoleculeSettings.GetMonoMass(fragment, 0, 1);
                yield return new FragmentIon(monoMass, ordinal, ionType);
            }
        }
    }
}
