﻿using System;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Model.Results.Imputation
{
    public class RatedPeak : Immutable
    {
        public RatedPeak(ReplicateFileInfo resultFileInfo, AlignmentFunction alignmentFunction, PeakBounds rawPeakBounds, double? score, bool manuallyIntegrated)
        {
            ReplicateFileInfo = resultFileInfo;
            RawPeakBounds = rawPeakBounds;
            AlignedPeakBounds = rawPeakBounds?.Align(alignmentFunction);
            ManuallyIntegrated = manuallyIntegrated;
            Score = score;
        }

        public ReplicateFileInfo ReplicateFileInfo { get; }
        public PeakBounds RawPeakBounds { get; }

        public PeakBounds AlignedPeakBounds { get; private set; }

        public double? Score { get; }
        public bool ManuallyIntegrated { get; }
        public double? Percentile { get; private set; }

        public RatedPeak ChangePercentile(double? value)
        {
            return ChangeProp(ImClone(this), im => im.Percentile = value);
        }

        public double? PValue { get; private set; }

        public RatedPeak ChangePValue(double? value)
        {
            return ChangeProp(ImClone(this), im => im.PValue = value);
        }

        public double? QValue { get; private set; }

        public RatedPeak ChangeQValue(double? value)
        {
            return ChangeProp(ImClone(this), im => im.QValue = value);
        }

        public bool Best { get; private set; }

        public RatedPeak ChangeBest(bool value)
        {
            return ChangeProp(ImClone(this), im => im.Best = value);
        }

        public bool Accepted { get; private set; }

        public RatedPeak ChangeAccepted(bool value)
        {
            return ChangeProp(ImClone(this), im => im.Accepted = value);
        }

        public double RtShift { get; private set; }

        public RatedPeak ChangeRtShift(double value)
        {
            return ChangeProp(ImClone(this), im => im.RtShift = value);
        }

        public class PeakBounds : IFormattable
        {
            public PeakBounds(double startTime, double endTime)
            {
                StartTime = startTime;
                EndTime = endTime;
            }

            public double StartTime { get; }
            public double EndTime { get; }
            public double MidTime
            {
                get { return (StartTime + EndTime) / 2; }
            }
            public double Width
            {
                get { return EndTime - StartTime; }
            }

            protected bool Equals(PeakBounds other)
            {
                return StartTime.Equals(other.StartTime) && EndTime.Equals(other.EndTime);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PeakBounds)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StartTime.GetHashCode() * 397) ^ EndTime.GetHashCode();
                }
            }

            public PeakBounds Align(AlignmentFunction alignmentFunction)
            {
                if (alignmentFunction == null)
                {
                    return this;
                }

                return new PeakBounds(alignmentFunction.GetY(StartTime), alignmentFunction.GetY(EndTime));
            }

            public override string ToString()
            {
                return string.Format(@"[{0},{1}]", StartTime.ToString(Formats.RETENTION_TIME),
                    EndTime.ToString(Formats.RETENTION_TIME));
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                return string.Format(@"[{0},{1}]", StartTime.ToString(format, formatProvider),
                    EndTime.ToString(format, formatProvider));
            }
        }
    }
}