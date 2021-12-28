using System;
using System.Collections.Generic;
using System.Data.Common;

namespace SkylineApi
{
    public interface IExtractedDataFile
    {
        string SourceFilePath { get; }
        IEnumerable<IChromatogramGroup> ChromatogramGroups { get; }
        IEnumerable<string> ScoreNames { get; }
        DateTime? LastWriteTime { get; }
        bool HasCombinedIonMobility { get; }
        bool Ms1Centroid { get; }
        bool Ms2Centroid { get; }
        DateTime? RunStartTime { get; }
        double? MaxRetentionTime { get; }
        double? MaxIntensity { get; }
        double? TotalIonCurrentArea { get; }
        string SampleId { get; }
        string InstrumentSerialNumber { get; }
        IEnumerable<InstrumentInfo> InstrumentInfos { get; }
    }
    public interface IChromatogramGroup
    {
        double PrecursorMz { get; }
        string TextId { get; }
        double? StartTime { get; }
        double? EndTime { get; }
        IEnumerable<IChromatogram> Chromatograms { get; }
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

    public class InstrumentInfo
    {
        public InstrumentInfo(string model, string ionization, string analyzer, string detector)
        {
            Model = model;
            Ionization = ionization;
            Analyzer = analyzer;
            Detector = detector;
        }
        public string Model { get; }
        public string Ionization { get; }
        public string Analyzer { get; }
        public string Detector { get; }
    }
}
