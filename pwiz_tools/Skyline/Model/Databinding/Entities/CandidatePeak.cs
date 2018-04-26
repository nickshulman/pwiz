using System.Collections.Generic;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public class CandidatePeak
    {
        private ChromPeak _chromPeak;

        public CandidatePeak(ChromPeak chromPeak)
        {
            _chromPeak = chromPeak;
        }

        public double RetentionTime
        {
            get { return _chromPeak.RetentionTime; }
        }

        public double StartTime
        {
            get { return _chromPeak.StartTime; }
        }

        public double EndTime
        {
            get { return _chromPeak.EndTime; }
        }

        public double Area
        {
            get { return _chromPeak.Area; }
        }

        public double BackgroundArea
        {
            get { return _chromPeak.BackgroundArea; }
        }

        public double Height
        {
            get { return _chromPeak.Height; }
        }

        public double Fwhm
        {
            get { return _chromPeak.Fwhm; }
        }

        public int? PointsAcross
        {
            get { return _chromPeak.PointsAcross; }
        }

        public bool FwhmDegenerate
        {
            get { return _chromPeak.IsFwhmDegenerate; }
        }

        public PeakIdentification Identified {get { return _chromPeak.Identified; }}

        public bool? Truncated {get { return _chromPeak.IsTruncated; }}

        public double? MassError {get { return _chromPeak.MassError; }}
    }
}
