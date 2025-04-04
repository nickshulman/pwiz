using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using pwiz.Common.Collections;

namespace pwiz.Common.SystemUtil.PerfCounters
{
    public class PerfCounter
    {
        private HierarchicalCounter _counts = new HierarchicalCounter();
        private Dictionary<string, PerfCounter> _detailCounters;

        public void Increment(string detail, PerfQuantity perfQuantity)
        {
            lock (this)
            {
                _counts.Increment(perfQuantity);
                if (detail != null)
                {

                    _detailCounters ??= new Dictionary<string, PerfCounter>();
                    if (!_detailCounters.TryGetValue(detail, out var detailCounter))
                    {
                        detailCounter = new PerfCounter();
                        _detailCounters.Add(detail, detailCounter);
                    }

                    detailCounter.Increment(null, perfQuantity);
                }
            }
        }

        public PerfCounts GetCounts()
        {
            return _counts.GetCounts();
        }

        public ImmutableList<KeyValuePair<string, PerfCounter>> GetDetails()
        {
            lock (this)
            {
                return _detailCounters?.ToImmutable() ?? ImmutableList<KeyValuePair<string, PerfCounter>>.EMPTY;
            }
        }

        public void Measure(string detail, int size, [InstantHandle] Action action)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            action();
            stopWatch.Stop();
            Increment(detail, new PerfQuantity(1, size, stopWatch.Elapsed));
        }

        public void Measure([InstantHandle] Action action)
        {
            Measure(null, 0, action);
        }

        public T Measure<T>(string detail, int size, [InstantHandle] Func<T> function)
        {
            T result = default;
            Measure(detail, size, () =>
            {
                result = function();
            });
            return result;
        }

        public T Measure<T>([InstantHandle] Func<T> function)
        {
            return Measure(null, 0, function);
        }

        public void Reset()
        {
            lock (this)
            {
                _counts = new HierarchicalCounter();
                _detailCounters = null;
            }
        }
    }
}
