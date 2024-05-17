using System;
using System.Collections.Generic;
using System.Linq;

namespace pwiz.Skyline.Model.RetentionTimes
{
    public class MultiSequenceLcs<TItem>
    {
        public static List<TItem> GetLongestCommonSubsequence(List<List<TItem>> sequences)
        {
            if (sequences == null || sequences.Count == 0)
                return new List<TItem>();

            int numSequences = sequences.Count;
            var lengths = sequences.Select(seq => seq.Count).ToArray();

            // Create a multi-dimensional array to store the lengths of LCS
            var lcsMatrix = new int[lengths.Max() + 1, lengths.Max() + 1, lengths.Max() + 1];

            // Fill the matrix
            for (int i = 1; i <= lengths[0]; i++)
            {
                for (int j = 1; j <= lengths[1]; j++)
                {
                    for (int k = 1; k <= lengths[2]; k++)
                    {
                        // Count how many sequences have the same element at the current positions
                        var currentItem = sequences[0][i - 1];
                        var matchCount = 0;
                        if (i <= lengths[0] && sequences[0][i - 1].Equals(currentItem)) matchCount++;
                        if (j <= lengths[1] && sequences[1][j - 1].Equals(currentItem)) matchCount++;
                        if (k <= lengths[2] && sequences[2][k - 1].Equals(currentItem)) matchCount++;

                        if (matchCount >= numSequences / 2)
                        {
                            lcsMatrix[i, j, k] = lcsMatrix[i - 1, j - 1, k - 1] + 1;
                        }
                        else
                        {
                            lcsMatrix[i, j, k] = Math.Max(Math.Max(lcsMatrix[i - 1, j, k], lcsMatrix[i, j - 1, k]), lcsMatrix[i, j, k - 1]);
                        }
                    }
                }
            }

            // Traceback to find the LCS
            var lcs = new List<TItem>();
            int x = lengths[0], y = lengths[1], z = lengths[2];
            while (x > 0 && y > 0 && z > 0)
            {
                var currentItem = sequences[0][x - 1];
                var matchCount = 0;
                if (x <= lengths[0] && sequences[0][x - 1].Equals(currentItem)) matchCount++;
                if (y <= lengths[1] && sequences[1][y - 1].Equals(currentItem)) matchCount++;
                if (z <= lengths[2] && sequences[2][z - 1].Equals(currentItem)) matchCount++;

                if (matchCount >= numSequences / 2)
                {
                    lcs.Add(currentItem);
                    x--; y--; z--;
                }
                else if (lcsMatrix[x - 1, y, z] >= lcsMatrix[x, y - 1, z] && lcsMatrix[x - 1, y, z] >= lcsMatrix[x, y, z - 1])
                {
                    x--;
                }
                else if (lcsMatrix[x, y - 1, z] >= lcsMatrix[x - 1, y, z] && lcsMatrix[x, y - 1, z] >= lcsMatrix[x, y, z - 1])
                {
                    y--;
                }
                else
                {
                    z--;
                }
            }

            lcs.Reverse();
            return lcs;
        }


        public static List<TItem> GreedyMultiSequenceLCS(List<List<TItem>> sequences)
        {
            if (sequences == null || sequences.Count == 0)
                return new List<TItem>();

            int numSequences = sequences.Count;
            var indices = new int[numSequences];
            var result = new List<TItem>();

            while (true)
            {
                // Find the next common element in all sequences
                var candidates = new Dictionary<TItem, int>();

                for (int i = 0; i < numSequences; i++)
                {
                    if (indices[i] < sequences[i].Count)
                    {
                        var item = sequences[i][indices[i]];
                        if (!candidates.ContainsKey(item))
                            candidates[item] = 0;
                        candidates[item]++;
                    }
                }

                // Select the element that appears in the majority of sequences
                var selectedItem = default(TItem);
                var maxCount = 0;

                foreach (var candidate in candidates)
                {
                    if (candidate.Value > maxCount)
                    {
                        selectedItem = candidate.Key;
                        maxCount = candidate.Value;
                    }
                }

                if (maxCount == 0)
                    break;

                // Add the selected element to the result and advance the indices
                result.Add(selectedItem);
                for (int i = 0; i < numSequences; i++)
                {
                    while (indices[i] < sequences[i].Count && !sequences[i][indices[i]].Equals(selectedItem))
                        indices[i]++;
                    if (indices[i] < sequences[i].Count)
                        indices[i]++;
                }
            }

            return result;
        }

    }
}
