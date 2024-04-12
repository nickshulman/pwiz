using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Util;

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
                var scores = EnumerateDiagonalScores().ToArray();
                MaxScore = scores.Max();
                MedianScore = new Statistics(scores).Median();
            }

            public SimilarityGrid Grid { get; }
            public int XStart { get; }
            public int XCount { get; }
            public int YStart { get; }
            public int YCount { get; }

            public double MedianScore { get; }
            public double MaxScore { get; }

            public double CalculateAverageScore()
            {
                return EnumerateDiagonalScores().Average();
            }

            private IEnumerable<double> EnumerateDiagonalScores(bool includeDownwardsDiagonal = true)
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

                var quadrants = EnumerateQuadrants().OrderByDescending(q => q.MaxScore).ToList();
                if (quadrants.Count <= 2)
                {
                    return quadrants.Take(1);
                }

                double minMedian = quadrants.Take(2).Min(q => q.MedianScore);
                return quadrants.Where(q => q.MaxScore >= minMedian).Take(3);
            }

            public IEnumerable<Quadrant> FindBestQuadrants()
            {
                var parallelProcessor = new ParallelProcessor();
                return parallelProcessor.FindBestQuadrants(this);
            }
        }

        public Quadrant ToQuadrant()
        {
            return new Quadrant(this, 0, XEntries.Count, 0, YEntries.Count);
        }

        public IEnumerable<Tuple<SpectrumSummary, SpectrumSummary>> FindBestPoints()
        {
            var parallelProcessor = new ParallelProcessor();
            var results = parallelProcessor.FindBestQuadrants(ToQuadrant());
            return results.Select(q => Tuple.Create(XEntries[q.XStart], YEntries[q.YStart]));
        }

        class ParallelProcessor
        {
            private List<Quadrant> _results = new List<Quadrant>();
            private int _totalItemCount;
            private int _completedItemCount;
            private QueueWorker<Quadrant> _queue;
            private List<Exception> _exceptions = new List<Exception>();

            public List<Quadrant> FindBestQuadrants(Quadrant start)
            {
                _queue = new QueueWorker<Quadrant>(null, Consume);
                try
                {
                    _queue.RunAsync(ParallelEx.GetThreadCount(), @"SimilarityGrid");
                    Enqueue(start);
                    while (true)
                    {
                        lock (this)
                        {
                            if (_exceptions.Any())
                            {
                                throw new AggregateException(_exceptions);
                            }

                            if (_completedItemCount == _totalItemCount)
                            {
                                return _results;
                            }

                            Monitor.Wait(this);
                        }
                    }
                }
                finally
                {
                    _queue.Dispose();
                }
            }

            private void Consume(Quadrant quadrant, int threadIndex)
            {
                try
                {
                    foreach (var q in quadrant.EnumerateBestQuadrants())
                    {
                        Enqueue(q);
                    }
                }
                catch (Exception e)
                {
                    lock (this)
                    {
                        _exceptions.Add(e);
                    }
                }
                finally
                {
                    lock (this)
                    {
                        _completedItemCount++;
                        Monitor.PulseAll(this);
                    }
                }
            }

            

            private void Enqueue(Quadrant quadrant)
            {
                lock (this)
                {
                    if (quadrant.XCount == 1 && quadrant.YCount == 1)
                    {
                        _results.Add(quadrant);
                    }
                    else
                    {
                        _queue.Add(quadrant);
                        _totalItemCount++;
                    }
                }
            }
        }
    }
}
