﻿using JetBrains.Annotations;
using MathNet.Numerics.Distributions;
using pwiz.Skyline.Model.Results.Scoring;

namespace pwiz.Skyline.Model.Results.Imputation
{
    public abstract class CutoffScoreType
    {
        public static readonly CutoffScoreType RAW = new RawScore();
        public static readonly CutoffScoreType PVALUE = new PValue();
        public static readonly CutoffScoreType QVALUE = new QValue();
        public static readonly CutoffScoreType PERCENTILE = new Percentile();

        public abstract double? ToRawScore([CanBeNull] ScoringResults scoringResults, double value);
        public abstract double? FromRawScore([CanBeNull] ScoringResults scoringResults, double value);
        public abstract bool IsEnabled(PeakScoringModelSpec scoringModelSpec);

        private class RawScore : CutoffScoreType
        {
            public override double? ToRawScore(ScoringResults scoringResults, double value)
            {
                return value;
            }

            public override double? FromRawScore(ScoringResults scoringResults, double value)
            {
                return value;
            }

            public override bool IsEnabled(PeakScoringModelSpec scoringModelSpec)
            {
                return true;
            }

            public override string ToString()
            {
                return "Raw Score";
            }
        }

        private class Percentile : CutoffScoreType
        {
            public override double? ToRawScore(ScoringResults scoringResults, double value)
            {
                return scoringResults?.GetScoreAtPercentile(value);
            }

            public override double? FromRawScore(ScoringResults scoringResults, double value)
            {
                return scoringResults?.GetPercentileOfScore(value);
            }

            public override bool IsEnabled(PeakScoringModelSpec scoringModelSpec)
            {
                return true;
            }

            public override string ToString()
            {
                return "Percentile";
            }
        }

        private class PValue : CutoffScoreType
        {
            public override double? ToRawScore(ScoringResults scoringResults, double value)
            {
                return Normal.InvCDF(0, 1, 1 - value);
            }

            public override double? FromRawScore(ScoringResults scoringResults, double value)
            {
                return 1 - Normal.CDF(0, 1, value);
            }

            public override bool IsEnabled(PeakScoringModelSpec scoringModelSpec)
            {
                return !Equals(scoringModelSpec, LegacyScoringModel.DEFAULT_MODEL);
            }

            public override string ToString()
            {
                return "P-Value";
            }
        }

        private class QValue : CutoffScoreType
        {
            public override double? FromRawScore(ScoringResults scoringResults, double value)
            {
                return scoringResults?.ScoreQValueMap?.GetQValue(value);
            }

            public override bool IsEnabled(PeakScoringModelSpec scoringModelSpec)
            {
                return !Equals(scoringModelSpec, LegacyScoringModel.DEFAULT_MODEL);
            }

            public override double? ToRawScore(ScoringResults scoringResults, double value)
            {
                return scoringResults?.ScoreQValueMap?.GetZScore(value);
            }

            public override string ToString()
            {
                return "Q-Value";
            }
        }
    }
}
