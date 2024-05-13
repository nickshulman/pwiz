using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Model.PeakImputation
{
    public class ApexPeakBounds
    {
        public ApexPeakBounds(double apexTime, double startTime, double endTime)
        {
            ApexTime = apexTime;
            StartTime = startTime;
            EndTime = endTime;
        }
        public double ApexTime { get; }
        public double StartTime { get; }
        public double EndTime { get; }

        public ApexPeakBounds Align(AlignmentFunction alignmentFunction)
        {
            return new ApexPeakBounds(alignmentFunction.GetY(ApexTime), alignmentFunction.GetY(StartTime),
                alignmentFunction.GetY(EndTime));
        }

        public ApexPeakBounds ReverseAlign(AlignmentFunction alignmentFunction)
        {
            return new ApexPeakBounds(alignmentFunction.GetX(ApexTime), alignmentFunction.GetX(StartTime),
                alignmentFunction.GetX(EndTime));
        }

        public static ApexPeakBounds Average(IEnumerable<ApexPeakBounds> peakBounds)
        {
            var startTimes = new List<double>();
            var endTimes = new List<double>();
            var apexes = new List<double>();
            foreach (var bounds in peakBounds)
            {
                startTimes.Add(bounds.StartTime);
                endTimes.Add(bounds.EndTime);
                apexes.Add(bounds.ApexTime);
            }

            if (startTimes.Count == 0)
            {
                return null;
            }

            return new ApexPeakBounds(apexes.Mean(), startTimes.Mean(), endTimes.Mean());
        }

        public override string ToString()
        {
            return string.Format(@"[{0},{1}]", StartTime.ToString(Formats.RETENTION_TIME),
                EndTime.ToString(Formats.RETENTION_TIME));
        }
    }
}