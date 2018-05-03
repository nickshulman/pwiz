using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class TurnoverCalculator
    {
        public bool ErrOnSideOfLowerAbundance { get; set; }
        /// <summary>
        /// Find the combination of linear combinations of the candidate vectors which results in 
        /// the least squares match of the target vectors.
        /// This uses the algorithm outlined in:
        /// <para>
        /// Least Squares Analysis and Simplification of Multi-Isotope Mass Spectra.
        /// J. I. Brauman
        /// Analytical Chemistry 1966 38 (4), 607-610
        /// </para>
        /// </summary>
        // ReSharper disable InconsistentNaming
        private static Vector<double> FindBestCombination(Vector<double> targetVector, params Vector<double>[] candidateVectors)
        {
            Matrix matrixA = new DenseMatrix(targetVector.Count, candidateVectors.Length);
            for (int j = 0; j < candidateVectors.Length; j++)
            {
                matrixA.SetColumn(j, candidateVectors[j]);
            }
            var matrixP = targetVector.ToColumnMatrix();
            var matrixAT = matrixA.Transpose();
            var matrixATA = matrixAT.Multiply(matrixA);
            if (matrixATA.Determinant() == 0)
            {
                return null;
            }
            var matrixATAInv = matrixATA.Inverse();
            var matrixResult = matrixATAInv.Multiply(matrixAT).Multiply(matrixP);
            return matrixResult.Column(0);
        }
        // ReSharper restore InconsistentNaming

        private Vector<double> FindBestCombination(Vector<double> targetVector, Vector<double>[] candidateVectors, bool errOnSideOfLowerAbundance)
        {
            var result = FindBestCombination(targetVector, candidateVectors);
            if (!errOnSideOfLowerAbundance)
            {
                return result;
            }
            var newTargetVector = new DenseVector(targetVector.Count);
            for (int i = 0; i < newTargetVector.Count; i++)
            {
                var totalCandidate = 0.0;
                for (int iVector = 0; iVector < candidateVectors.Length; iVector++)
                {
                    totalCandidate += candidateVectors[iVector][i] * result[iVector];
                }
                newTargetVector[i] = Math.Min(totalCandidate, targetVector[i]);
            }
            return FindBestCombination(newTargetVector, candidateVectors);
        }

        public Vector<double> FindBestCombinationFilterNegatives(Vector<double> observedIntensities, IList<Vector<double>> candidates, Func<int, bool> excludeFunc)
        {
            Vector<double>[] filteredCandidates = new Vector<double>[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                filteredCandidates[i] = FilterVector(candidates[i], excludeFunc);
            }
            return FindBestCombinationFilterNegatives(FilterVector(observedIntensities, excludeFunc), filteredCandidates);
        }

        public Vector<double> FindBestCombinationFilterNegatives(Vector<double> observedIntensities, IList<Vector<double>> candidates)
        {
            List<int> remaining = new List<int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                remaining.Add(i);
            }
            Vector<double> filteredResult;
            while (true)
            {
                Vector<double>[] curCandidates = new Vector<double>[remaining.Count];
                for (int i = 0; i < remaining.Count; i++)
                {
                    curCandidates[i] = candidates[remaining[i]];
                }
                filteredResult = FindBestCombination(observedIntensities, curCandidates, ErrOnSideOfLowerAbundance);
                if (filteredResult == null)
                {
                    return null;
                }
                if (filteredResult.Min() >= 0)
                {
                    break;
                }
                List<int> newRemaining = new List<int>();
                for (int i = 0; i < remaining.Count; i++)
                {
                    if (filteredResult[i] >= 0)
                    {
                        newRemaining.Add(remaining[i]);
                    }
                }
                remaining = newRemaining;
                if (remaining.Count == 0)
                {
                    return null;
                }
            }
            Vector<double> result = new DenseVector(candidates.Count);
            for (int i = 0; i < remaining.Count; i++)
            {
                result[remaining[i]] = filteredResult[i];
            }
            return result;
        }

        private static Vector<double> FilterVector(IList<double> list, Func<int, bool> isExcludedFunc)
        {
            List<double> filtered = new List<double>();
            for (int i = 0; i < list.Count; i++)
            {
                if (isExcludedFunc(i))
                {
                    continue;
                }
                filtered.Add(list[i]);
            }
            return new DenseVector(filtered.ToArray());
        }
    }
}
