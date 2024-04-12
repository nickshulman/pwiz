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
                int coordinateCount = Math.Max(XCount, YCount);
                double total = 0;
                for (int i = 0; i < coordinateCount; i++)
                {
                    int x = i * XCount / coordinateCount;
                    int y = i * YCount / coordinateCount;
                    total += Grid.XEntries[XStart + x].SimilarityScore(Grid.YEntries[YStart + y]) ?? 0;
                    //                    total += Grid.XEntries[XStart + XCount - x - 1].SimilarityScore(Grid.YEntries[YStart + y]) ?? 0;
                }

                return total / coordinateCount;
            }

            public IEnumerable<double> EnumerateDiagonalScores(bool includeDownwardsDiagonal = true)
            {
                int coordinateCount = Math.Max(XCount, YCount);
                for (int i = 0; i < coordinateCount; i++)
                {
                    int x = i * XCount / coordinateCount;
                    int y = i * YCount / coordinateCount;
                    yield return Grid.XEntries[XStart + x].SimilarityScore(Grid.YEntries[YStart + y]) ?? 0;
                    if (includeDownwardsDiagonal)
                    {
                        yield return Grid.XEntries[XStart + XCount - x - 1]
                            .SimilarityScore(Grid.YEntries[YStart + y]) ?? 0;
                    }
                }
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

            public IEnumerable<Quadrant> EnumerateBestQuadrants()
            {
                if (XCount <= 1 && YCount <= 1)
                {
                    return new[] { this };
                }

                var quadrants = EnumerateQuadrants().ToList();
                var quadrantScores = quadrants.SelectMany((q, index) =>
                        q.EnumerateDiagonalScores().Select(score => Tuple.Create(score, index)))
                    .OrderByDescending(tuple => tuple.Item1).ToList();
                var indexesToReturn = new HashSet<int>();
                for (int i = 0; i < quadrantScores.Count; i++)
                {
                    if (indexesToReturn.Count == quadrants.Count - 1)
                    {
                        break;
                    }

                    if (indexesToReturn.Count >= quadrants.Count / 2 && i > quadrantScores.Count / 16)
                    {
                        break;
                    }

                    indexesToReturn.Add(quadrantScores[i].Item2);
                }
                return indexesToReturn.Select(i => quadrants[i]);
            }

            public IEnumerable<Quadrant> FindBestQuadrants()
            {
                if (XCount <= 1 && YCount <= 1)
                {
                    return new[] { this };
                }

                return EnumerateBestQuadrants().SelectMany(q => q.FindBestQuadrants());
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
