using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pwiz.Common.SystemUtil.PerfCounters
{
    public class HierarchicalCounter
    {
        private readonly PerfQuantity[] _secondBuckets = Enumerable.Repeat(PerfQuantity.Zero, 60).ToArray();
        private readonly PerfQuantity[] _minuteBuckets = Enumerable.Repeat(PerfQuantity.Zero, 60).ToArray();
        private readonly PerfQuantity[] _hourBuckets = Enumerable.Repeat(PerfQuantity.Zero, 24).ToArray();
        private DateTime _lastSecondUpdate = DateTime.UtcNow;
        private int _currentSecondIndex;
        private int _currentMinuteIndex;
        private int _currentHourIndex;
        private readonly object _sync = new object();
        private PerfQuantity _total;

        public void Increment(PerfQuantity value)
        {
            lock (_sync)
            {
                AdvanceBuckets();
                _secondBuckets[_currentSecondIndex] += value;
                _total += value;
            }
        }

        public PerfCounts GetCounts()
        {
            lock (_sync)
            {
                AdvanceBuckets();
                var lastMinute = Sum(_secondBuckets);
                var lastHour = lastMinute + Sum(_minuteBuckets);
                var lastDay = lastHour + Sum(_hourBuckets);
                return new PerfCounts(lastMinute, lastHour, lastDay, _total);
            }
        }
        
        private void AdvanceBuckets()
        {
            var now = DateTime.UtcNow;
            var elapsedSeconds = (int)(now - _lastSecondUpdate).TotalSeconds;

            if (elapsedSeconds <= 0)
                return;

            for (int i = 0; i < Math.Min(elapsedSeconds, _secondBuckets.Length); i++)
            {
                _currentSecondIndex = (_currentSecondIndex + 1) % _secondBuckets.Length;

                var rolledUpValue = _secondBuckets[_currentSecondIndex];
                _secondBuckets[_currentSecondIndex] = PerfQuantity.Zero;

                if (_currentSecondIndex % 60 == 0)
                {
                    _currentMinuteIndex = (_currentMinuteIndex + 1) % _minuteBuckets.Length;
                    var minuteRollupValue = _minuteBuckets[_currentMinuteIndex];
                    _minuteBuckets[_currentMinuteIndex] = PerfQuantity.Zero;
                    _minuteBuckets[_currentMinuteIndex] += rolledUpValue;

                    if (_currentMinuteIndex % 60 == 0)
                    {
                        _currentHourIndex = (_currentHourIndex + 1) % _hourBuckets.Length;
                        _hourBuckets[_currentHourIndex] = PerfQuantity.Zero;
                        _hourBuckets[_currentHourIndex] += minuteRollupValue;
                    }
                }
                else
                {
                    _minuteBuckets[_currentMinuteIndex] += rolledUpValue;
                }
            }

            _lastSecondUpdate = _lastSecondUpdate.AddSeconds(elapsedSeconds);
        }

        private static PerfQuantity Sum(IEnumerable<PerfQuantity> quantities)
        {
            return quantities.Aggregate(PerfQuantity.Zero, (q1, q2) => q1 + q2);
        }
    }

    public class PerfCounts : IComparable<PerfCounts>
    {
        public PerfCounts(PerfQuantity lastMinute, PerfQuantity lastHour, PerfQuantity lastDay, PerfQuantity total)
        {
            LastMinute = lastMinute ?? PerfQuantity.Zero;
            LastHour = lastHour ?? PerfQuantity.Zero;
            LastDay = lastDay ?? PerfQuantity.Zero;
            Total = total ?? PerfQuantity.Zero;
        }

        public PerfQuantity LastMinute { get; }
        public PerfQuantity LastHour { get; }
        public PerfQuantity LastDay { get; }
        public PerfQuantity Total { get; }

        public override string ToString()
        {
            PerfQuantity lastQuantity = PerfQuantity.Zero;
            var parts = new List<string>();
            foreach (var part in new[]
                     {
                         Tuple.Create("LastMinute", LastMinute),
                         Tuple.Create("LastHour", LastHour),
                         Tuple.Create("LastDay", LastDay),
                         Tuple.Create("Total", Total)
                     })
            {
                if (part.Item2 > lastQuantity)
                {
                    parts.Add(part.Item1 + @":" + part.Item2);
                    lastQuantity = part.Item2;
                }
            }

            if (parts.Count == 0)
            {
                return "Total:0";
            }
            return string.Join(" ", parts);
        }

        public int CompareTo(PerfCounts other)
        {
            if (other == null)
            {
                return 1;
            }

            int result = LastMinute.CompareTo(other.LastMinute);
            if (result == 0)
            {
                result = LastHour.CompareTo(other.LastHour);
            }

            if (result == 0)
            {
                result = LastDay.CompareTo(other.LastDay);
            }

            if (result == 0)
            {
                result = Total.CompareTo(other.Total);
            }
            return result;
        }
    }

    public class PerfQuantity : IComparable<PerfQuantity>, IComparable
    {
        public static readonly PerfQuantity Zero = new PerfQuantity(0, 0, TimeSpan.Zero);
        public PerfQuantity(long count, long size, TimeSpan duration)
        {
            Count = count;
            Size = size;
            Duration = duration;
        }

        public long Count { get; private set; }
        public long Size { get; private set; }
        public TimeSpan Duration { get; private set; }

        public bool IsZero
        {
            get
            {
                return Equals(Zero);
            }
        }

        public static PerfQuantity operator +(PerfQuantity a, PerfQuantity b)
        {
            if (false != a?.IsZero)
            {
                return b ?? Zero;
            }
            if (false != b?.IsZero)
            {
                return a;
            }
            return new PerfQuantity(a.Count + b.Count, a.Size + b.Size, a.Duration + b.Duration);
        }

        protected bool Equals(PerfQuantity other)
        {
            return Count == other.Count && Size == other.Size && Duration.Equals(other.Duration);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PerfQuantity)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Count.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ Duration.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            if (Count != 0)
            {
                stringBuilder.Append(Count);
            }


            if (!Equals(Duration, TimeSpan.Zero))
            {
                stringBuilder.Append(" in ");
                stringBuilder.Append(FormatDuration(Duration));
            }

            if (stringBuilder.Length == 0)
            {
                return "0";
            }

            return stringBuilder.ToString();
        }

        public int CompareTo(PerfQuantity other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var durationComparison = Duration.CompareTo(other.Duration);
            if (durationComparison != 0) return durationComparison;
            var sizeComparison = Size.CompareTo(other.Size);
            if (sizeComparison != 0) return sizeComparison;
            return Count.CompareTo(other.Count);
        }

        private static string FormatDuration(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return FormatNumber(timeSpan.TotalHours) + "h";
            }

            if (timeSpan.TotalMinutes >= 10)
            {
                return FormatNumber(timeSpan.TotalMinutes) + "m";
            }

            if (timeSpan.TotalSeconds >= 10)
            {
                return FormatNumber(timeSpan.TotalSeconds) + "s";
            }

            if (timeSpan.TotalMilliseconds >= 10)
            {
                return FormatNumber(timeSpan.TotalMilliseconds) + "ms";
            }

            return FormatNumber(timeSpan.Ticks / 10.0) + "us";
        }

        private static string FormatNumber(double value)
        {
            if (value >= 100)
            {
                return value.ToString("0");
            }

            if (value >= 10)
            {
                return value.ToString("0.#");
            }
            return value.ToString("0.##");
        }

        public int CompareTo(object obj)
        {
            if (obj is null) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is PerfQuantity other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(PerfQuantity)}");
        }

        public static bool operator <(PerfQuantity left, PerfQuantity right)
        {
            return Comparer<PerfQuantity>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(PerfQuantity left, PerfQuantity right)
        {
            return Comparer<PerfQuantity>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(PerfQuantity left, PerfQuantity right)
        {
            return Comparer<PerfQuantity>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(PerfQuantity left, PerfQuantity right)
        {
            return Comparer<PerfQuantity>.Default.Compare(left, right) >= 0;
        }
    }
}
