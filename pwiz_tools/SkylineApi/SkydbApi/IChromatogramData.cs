using System.Collections.Generic;

namespace SkylineApi
{
    public interface IExtractedChromatograms
    {
        string SourceFilePath { get; }
        IEnumerable<IChromatogramGroup> ChromatogramGroups { get; }
        IEnumerable<string> ScoreNames { get; }
    }
    public interface IChromatogramGroup
    {
        double PrecursorMz { get; }
        string TextId { get; }
        double? StartTime { get; }
        double? EndTime { get; }
        IEnumerable<IChromatogram> ExtractedChromatograms { get; }
        InterpolationParameters InterpolationParameters { get; }
        IEnumerable<ICandidatePeakGroup> CandidatePeakGroups { get; }
    }

    public interface IChromatogram
    {
        double ProductMz { get; }
        double ExtractionWidth { get; }
        double? IonMobilityValue { get; }
        double? IonMobilityExtractionWidth { get; }
        int NumPoints { get; }
        IList<float> RetentionTimes { get; }
        IList<float> Intensities { get; }
        IList<float> MassErrors { get; }
        IList<string> SpectrumIdentifiers { get; }
    }

    public interface ICandidatePeakGroup
    {
        double? GetScore(string name);
        bool IsBestPeak { get; }
        IList<ICandidatePeak> CandidatePeaks { get; }
        PeakIdentified Identified { get; }
    }

    public interface ICandidatePeak
    {
        double StartTime { get; }
        double EndTime { get; }
        double Area { get; }
        double BackgroundArea { get; }
        double Height { get; }
        double FullWidthAtHalfMax { get; }
        int? PointsAcross { get; }
        bool DegenerateFwhm { get; }
        bool ForcedIntegration { get; }
        bool? Truncated { get; }
        double? MassError { get; }
    }

    public interface ISpectrumInfo
    {
        string SpectrumIdentifier { get; }
        double RetentionTime { get; }
    }

    public class InterpolationParameters
    {
        public InterpolationParameters(double startTime, double endTime, int numberOfPoints, double intervalDelta, bool inferZeroes)
        {
            StartTime = startTime;
            EndTime = endTime;
            NumberOfPoints = numberOfPoints;
            IntervalDelta = intervalDelta;
            InferZeroes = inferZeroes;
        }

        public double StartTime { get; }
        public double EndTime { get; }
        public int NumberOfPoints { get; }
        public double IntervalDelta { get; }
        public bool InferZeroes { get; }
    }

    public enum PeakIdentified
    {
        False,
        Aligned,
        True,
    }
}
