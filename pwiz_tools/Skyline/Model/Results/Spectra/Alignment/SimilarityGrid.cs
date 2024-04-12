using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    public class SimilarityGrid
    {
        public SimilarityGrid(IEnumerable<SpectrumSummary> xEntries, IEnumerable<SpectrumSummary> yEntries)
        {
            XEntries = ImmutableList.ValueOf(xEntries);
            YEntries = ImmutableList.ValueOf(yEntries);
        }

        public ImmutableList<SpectrumSummary> XEntries { get; }
        public ImmutableList<SpectrumSummary> YEntries { get; }

        public class Quadrant
        {
            public Quadrant(SimilarityGrid grid, int xStart, int xCount, int yStart, int yCount)
            {
                Grid = grid;
                XStart = xStart;
                XCount = xCount;
                YStart = yStart;
                YCount = yCount;
            }

            public SimilarityGrid Grid { get; }
            public int XStart { get; }
            public int XCount { get; }
            public int YStart { get; }
            public int YCount { get; }

            public double CalculateAverageScore()
            {
                int count = Math.Max(XCount, YCount);
                double total = 0;
                for (int i = 0; i < count; i++)
                {
                    int x = i * XCount / count;
                    int y = i * YCount / count;
                    total += Grid.XEntries[XStart + x].SimilarityScore(Grid.YEntries[YStart + y]) ?? 0;
//                    total += Grid.XEntries[XStart + XCount - x - 1].SimilarityScore(Grid.YEntries[YStart + y]) ?? 0;
                }

                return total / count;
            }

            public IEnumerable<Quadrant> EnumerateQuadrants()
            {
                foreach ((int xStart, int xCount) in new[]
                             { (XStart, XCount / 2), (XStart + XCount / 2, XCount - XCount / 2) })
                {
                    foreach ((int yStart, int yCount) in new[]
                             {
                                 (YStart, YCount / 2), (YStart + YCount / 2, YCount - YCount / 2)
                             })
                    {
                        if (xCount > 0 && yCount > 0)
                        {
                            yield return new Quadrant(Grid, xStart, xCount, yStart, yCount);
                        }
                    }
                }
            }

            public IEnumerable<Quadrant> FindBestQuadrants(bool includeQuestionable = true)
            {
                if (XCount <= 1 && YCount <= 1)
                {
                    return new[] { this };
                }

                var quadrants = EnumerateQuadrants().OrderByDescending(q => q.CalculateAverageScore()).ToList();
                var result = Enumerable.Empty<Quadrant>();
                int count = Math.Min(includeQuestionable ? 3 : 2, quadrants.Count - 1);

                for (int i = 0; i < count; i++)
                {
                    var q = quadrants[i];
                    result = result.Concat(q.FindBestQuadrants(includeQuestionable && i != 2));
                }

                return result;
            }
        }

        public Quadrant ToQuadrant()
        {
            return new Quadrant(this, 0, XEntries.Count, 0, YEntries.Count);
        }

        public IEnumerable<Tuple<SpectrumSummary, SpectrumSummary>> FindBestPoints()
        {
            return ToQuadrant().FindBestQuadrants().Select(q => Tuple.Create(XEntries[q.XStart], YEntries[q.YStart]));
        }
    }
}
