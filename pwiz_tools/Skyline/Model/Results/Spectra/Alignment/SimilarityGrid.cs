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
            private Dictionary<KeyValuePair<int, int>, double> _similarityScores;
            public Quadrant(SimilarityGrid grid, int xStart, int xCount, int yStart, int yCount, IEnumerable<KeyValuePair<KeyValuePair<int, int>, double>> scores)
            {
                Grid = grid;
                XStart = xStart;
                XCount = xCount;
                YStart = yStart;
                YCount = yCount;
                if (scores != null)
                {
                    _similarityScores = scores.Where(kvp => kvp.Key.Key >= xStart && kvp.Key.Key < xStart + xCount
                                                                             && kvp.Key.Value >= yStart &&
                                                                             kvp.Key.Value < yStart + yCount)
                        .ToDictionary(kvp=>kvp.Key, kvp=>kvp.Value);
                    var diagonalScores = EnumerateDiagonalScores().ToArray();
                    MaxScore = diagonalScores.Max();
                    MedianScore = new Statistics(diagonalScores).Median();
                }
            }

            public SimilarityGrid Grid { get; }
            public int XStart { get; }
            public int XCount { get; }
            public int YStart { get; }
            public int YCount { get; }

            public double MaxScore { get; }
            public double MedianScore { get; }

            private IEnumerable<double> EnumerateDiagonalScores()
            {
                int coordinateCount = Math.Max(XCount, YCount);
                for (int i = 0; i < coordinateCount; i++)
                {
                    int x = i * XCount / coordinateCount;
                    int y = i * YCount / coordinateCount;
                    yield return CalcScore(XStart + x, YStart + y);
                    yield return CalcScore(XStart + XCount - x - 1, YStart + y);
                }
            }

            private double CalcScore(int x, int y)
            {
                var key = new KeyValuePair<int, int>(x, y);
                if (true == _similarityScores?.TryGetValue(key, out var value))
                {
                    return value;
                }

                value = Grid.XEntries[x].SimilarityScore(Grid.YEntries[y]) ?? 0;
                _similarityScores?.Add(key, value);
                return value;
            }

            public IEnumerable<Quadrant> EnumerateQuadrants(bool calcScores)
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
                            yield return new Quadrant(Grid, xStart, xCount, yStart, yCount, calcScores ? SimilarityScoreCache : null);
                        }
                    }
                }
            }

            private IEnumerable<KeyValuePair<KeyValuePair<int, int>, double>> SimilarityScoreCache
            {
                get
                {
                    if (_similarityScores != null)
                    {
                        return _similarityScores;
                    }

                    return Array.Empty<KeyValuePair<KeyValuePair<int, int>, double>>();
                }
            }

            public IEnumerable<Quadrant> EnumerateBestQuadrants()
            {
                if (XCount <= 1 && YCount <= 1)
                {
                    return new[] { this };
                }

                var quadrants = EnumerateQuadrants(true).OrderByDescending(q => q.MaxScore).ToList();
                var minMedian = quadrants.Take(2).Min(q => q.MedianScore);
                return quadrants.Where(q => q.MaxScore >= minMedian).Take(3);
                // return quadrants.Take(quadrants.Count - 1);
                // if (quadrants.Count <= 2)
                // {
                //     return quadrants.Take(1);
                // }
                //
                // double minMedian = quadrants.Take(2).Min(q => q.MedianScore);
                // return quadrants.Where(q => q.MaxScore >= minMedian).Take(3);
            }

            public IEnumerable<Point> EnumeratePoints()
            {
                for (int x = 0; x < XCount; x++)
                {
                    for (int y = 0; y < YCount; y++)
                    {
                        var score = CalcScore(XStart + x, YStart + y);
                        yield return new Point(Grid, x + XStart, y + YStart, score);
                    }
                }
            }
        }

        public Quadrant ToQuadrant()
        {
            return new Quadrant(this, 0, XEntries.Count, 0, YEntries.Count, null);
        }

        public IEnumerable<Quadrant> ToQuadrants(int levels)
        {
            var quadrants = new List<Quadrant> { ToQuadrant() };
            for (int i = 0; i < levels; i++)
            {
                quadrants = quadrants.SelectMany(q => q.EnumerateQuadrants(false)).ToList();
            }

            return quadrants;
        }

        public IEnumerable<Point> FindBestPoints()
        {
            var parallelProcessor = new ParallelProcessor();
            var results = parallelProcessor.FindBestPoints(ToQuadrants(3));
            return results;
        }

        class ParallelProcessor
        {
            private List<Point> _results = new List<Point>();
            private int _totalItemCount;
            private int _completedItemCount;
            private QueueWorker<Quadrant> _queue;
            private List<Exception> _exceptions = new List<Exception>();

            public List<Point> FindBestPoints(IEnumerable<Quadrant> startingQuadrants)
            {
                _queue = new QueueWorker<Quadrant>(null, Consume);
                try
                {
                    _queue.RunAsync(ParallelEx.GetThreadCount(), @"SimilarityGrid");
                    foreach (var q in startingQuadrants)
                    {
                        Enqueue(q);
                    }
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
                if (quadrant.XCount <= 4 || quadrant.YCount <= 4)
                {
                    var pointsToAdd = quadrant.EnumeratePoints().ToList();
                    lock (this)
                    {
                        _results.AddRange(pointsToAdd);
                    }

                    return;
                }
                lock (this)
                {
                    _queue.Add(quadrant);
                    _totalItemCount++;
                }
            }

        }
        public class Point
        {
            public Point(SimilarityGrid grid, int x, int y, double score)
            {
                Grid = grid;
                X = x;
                Y = y;
                Score = score;
            }

            public SimilarityGrid Grid { get; }
            public int X { get; }
            public int Y { get; }
            public double XRetentionTime
            {
                get { return Grid.XEntries[X].RetentionTime; }
            }

            public double YRetentionTime
            {
                get
                {
                    return Grid.YEntries[Y].RetentionTime;
                }
            }
            public double Score { get; }
        }
    }
}
