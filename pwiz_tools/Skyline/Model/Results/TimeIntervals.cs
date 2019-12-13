﻿using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{
    public class TimeIntervals
    {
        public static TimeIntervals FromIntervals(IEnumerable<KeyValuePair<float, float>> intervals)
        {
            return FromIntervalsSorted(MergeIntervals(intervals));
        }

        private static TimeIntervals FromIntervalsSorted(IEnumerable<KeyValuePair<float, float>> intervals)
        {
            var list = intervals.ToList();
            return new TimeIntervals
            {
                Starts = ImmutableList.ValueOf(list.Select(kvp => kvp.Key)),
                Ends = ImmutableList.ValueOf(list.Select(kvp => kvp.Value))
            };
        }

        public ImmutableList<float> Starts { get; private set; }
        public ImmutableList<float> Ends { get; private set; }

        public IEnumerable<KeyValuePair<float, float>> Intervals
        {
            get { return Enumerable.Range(0, Count).Select(i => new KeyValuePair<float, float>(Starts[i], Ends[i])); }
        }

        public int Count
        {
            get { return Starts.Count; }
        }

        public TimeIntervals Intersect(TimeIntervals other)
        {
            using (var myIntervals = Intervals.GetEnumerator())
            using (var otherIntervals = other.Intervals.GetEnumerator())
            {
                return FromIntervalsSorted(Intersect(myIntervals, otherIntervals));
            }
        }

        public int? IndexOfIntervalContaining(float time)
        {
            int index = CollectionUtil.BinarySearch(Ends, time);
            if (index < 0)
            {
                index = ~index;
            }

            if (index < 0 || index >= Ends.Count)
            {
                return null;
            }
            Assume.IsTrue(Ends[index] >= time);
            if (Starts[index] <= time)
            {
                return index;
            }

            return null;
        }

        public bool ContainsTime(float time)
        {
            return IndexOfIntervalContaining(time).HasValue;
        }

        public static IEnumerable<KeyValuePair<float, float>> MergeIntervals(
            IEnumerable<KeyValuePair<float, float>> intervals)
        {
            KeyValuePair<float, float>? lastInterval = null;
            foreach (var interval in intervals.OrderBy(kvp => kvp.Key))
            {
                if (interval.Value <= interval.Key)
                {
                    continue;
                }

                if (lastInterval.HasValue)
                {
                    if (lastInterval.Value.Value >= interval.Key)
                    {
                        lastInterval = new KeyValuePair<float, float>(lastInterval.Value.Key,
                            Math.Max(lastInterval.Value.Value, interval.Value));
                        continue;
                    }
                    else
                    {
                        yield return lastInterval.Value;
                    }
                }

                lastInterval = interval;
            }

            if (lastInterval.HasValue)
            {
                yield return lastInterval.Value;
            }
        }

        private static IEnumerable<KeyValuePair<float, float>> Intersect(IEnumerator<KeyValuePair<float, float>> en1,
            IEnumerator<KeyValuePair<float, float>> en2)
        {
            if (!en1.MoveNext() || !en2.MoveNext())
            {
                yield break;
            }

            KeyValuePair<float, float> interval1 = en1.Current;
            KeyValuePair<float, float> interval2 = en2.Current;
            while (true)
            {
                var intersection = new KeyValuePair<float, float>(
                    Math.Max(interval1.Key, interval2.Key),
                    Math.Min(interval1.Value, interval2.Value));
                if (intersection.Key < intersection.Value)
                {
                    yield return intersection;
                }
                if (interval1.Value <= interval2.Value)
                {
                    if (!en1.MoveNext())
                    {
                        yield break;
                    }

                    interval1 = en1.Current;
                }
                else
                {
                    if (!en2.MoveNext())
                    {
                        yield break;
                    }

                    interval2 = en2.Current;
                }
            }
        }

        public static TimeIntervals FromScanTimes(IEnumerable<float> times, float maxScanDuration)
        {
            return FromIntervalsSorted(InferTimeIntervals(times, maxScanDuration));
        }

        private static IEnumerable<KeyValuePair<float, float>> InferTimeIntervals(IEnumerable<float> times,
            float maxScanDuration)
        {
            KeyValuePair<float, float>? lastInterval = null;
            foreach (var time in times)
            {
                if (lastInterval.HasValue)
                {
                    if (time < lastInterval.Value.Value)
                    {
                        throw new InvalidOperationException();
                    }
                    if (time <= lastInterval.Value.Value + maxScanDuration)
                    {
                        lastInterval = new KeyValuePair<float, float>(lastInterval.Value.Key, time);
                        continue;
                    }
                    else
                    {
                        if (lastInterval.Value.Value > lastInterval.Value.Key)
                        {
                            yield return lastInterval.Value;
                        }
                    }
                }
                lastInterval = new KeyValuePair<float, float>(time, time);
            }

            if (lastInterval.HasValue && lastInterval.Value.Value > lastInterval.Value.Key)
            {
                yield return lastInterval.Value;
            }
        }
    }
}
