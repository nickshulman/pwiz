using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using pwiz.Common.Collections;

namespace pwiz.Common.SystemUtil.PerfCounters
{
    public class PerfCounter
    {
        private HierarchicalCounter _counts = new HierarchicalCounter();
        private Dictionary<string, PerfCounter> _detailCounters;

        public PerfCounter(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
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
                        detailCounter = new PerfCounter(detail);
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

        public ImmutableList<PerfCounter> GetDetails()
        {
            lock (this)
            {
                return _detailCounters?.Values.ToImmutable() ?? ImmutableList<PerfCounter>.EMPTY;
            }
        }
    }

    public static class PerfCounters
    {
        public static void Measure(this PerfCounter counter, string detail, int size, [InstantHandle] Action action)
        {
            var start = DateTime.UtcNow;
            action();
            counter.Increment(detail, new PerfQuantity(1, size, DateTime.UtcNow.Subtract(start)));
        }

        public static T Measure<T>(this PerfCounter counter, string detail, int size, [InstantHandle] Func<T> function)
        {
            T result = default;
            Measure(counter, detail, size, () =>
            {
                result = function();
            });
            return result;
        }
    }
}
