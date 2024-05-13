using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MathNet.Numerics.Statistics;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Irt;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.RetentionTimes
{
    public class AlignmentTarget : Immutable, IXmlSerializable
    {
        public AlignmentTarget(MsDataFileUri file, AverageType averageType, RtValueType rtValueType, RegressionMethodRT regressionMethod)
        {
            File = Equals(file, MsDataFilePath.EMPTY) ? null : file;
            AverageType = averageType;
            RtValueType = rtValueType;
            RegressionMethod = regressionMethod;
        }

        public MsDataFileUri File { get; private set; }

        public AlignmentTarget ChangeFile(MsDataFileUri file)
        {
            return ChangeProp(ImClone(this), im => im.File = Equals(file, MsDataFilePath.EMPTY) ? null : file);
        }
        public AverageType AverageType { get; private set; }

        public AlignmentTarget ChangeAverageType(AverageType averageType)
        {
            return ChangeProp(ImClone(this), im => im.AverageType = averageType);
        }
        public RtValueType RtValueType { get; private set; }

        public AlignmentTarget ChangeRtValueType(RtValueType rtValueType)
        {
            return ChangeProp(ImClone(this), im => im.RtValueType = rtValueType);
        }

        public RegressionMethodRT RegressionMethod { get; private set; }

        public AlignmentTarget ChangeRegressionMethod(RegressionMethodRT value)
        {
            return ChangeProp(ImClone(this), im => im.RegressionMethod = value);
        }

        
        protected bool Equals(AlignmentTarget other)
        {
            return Equals(File, other.File) && Equals(AverageType, other.AverageType) && Equals(RtValueType, other.RtValueType) && RegressionMethod == other.RegressionMethod;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlignmentTarget)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (File != null ? File.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ AverageType.GetHashCode();
                hashCode = (hashCode * 397) ^ RtValueType.GetHashCode();
                hashCode = (hashCode * 397) ^ RegressionMethod.GetHashCode();
                return hashCode;
            }
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        enum ATTR
        {
            target,
            values,
            aggregate,
            regression_method,
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (AverageType != null || RtValueType == null)
            {
                throw new InvalidOperationException();
            }

            AverageType = AverageType.FromName(reader.GetAttribute(ATTR.aggregate)) ?? AverageType.MEAN;
            RtValueType = RtValueType.FromName(reader.GetAttribute(ATTR.values)) ?? RtValueType.PEAK_APEXES;
            var targetText = reader.GetAttribute(ATTR.target);
            if (targetText != null)
            {
                File = MsDataFileUri.Parse(targetText);
            }

            RegressionMethod = reader.GetEnumAttribute(ATTR.regression_method, RegressionMethodRT.linear);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeIfString(ATTR.target, File?.ToString());
            writer.WriteAttribute(ATTR.values, RtValueType.Name);
            writer.WriteAttribute(ATTR.aggregate, AverageType.Name);
            writer.WriteAttribute(ATTR.regression_method, RegressionMethod);
        }

        public IEnumerable<KeyValuePair<object, double>> GetRetentionTimes(SrmDocument document)
        {
            if (File != null)
            {
                return GetRetentionTimes(document, File);
            }

            return RtValueType.GetSummaryRetentionTimes(document);
        }

        public IEnumerable<KeyValuePair<object, double>> GetRetentionTimes(SrmDocument document, MsDataFileUri source)
        {
            foreach (var kvp in RtValueType.GetRetentionTimesForFile(document, source))
            {
                yield return new KeyValuePair<object, double>(kvp.Key, AverageType.Calculate(kvp.Value));
            }
        }

    }

    public class AverageType
    {
        public static readonly AverageType MEAN = new AverageType("mean", () => "mean", values => values.Mean());
        public static readonly AverageType MEDIAN = new AverageType("median", () => "median", values => values.Median());

        public static ImmutableList<AverageType> ALL = ImmutableList.ValueOf(new[] { MEAN, MEDIAN });

        public static AverageType FromName(string name)
        {
            return ALL.FirstOrDefault(averageType => Equals(averageType.Name, name));
        }

        private Func<string> _getLabelFunc;
        private Func<IEnumerable<double>, double> _impl;
        private AverageType(string name, Func<string> getLabelFunc, Func<IEnumerable<double>, double> impl)
        {
            Name = name;
            _getLabelFunc = getLabelFunc;
            _impl = impl;
        }

        public string Name { get; }
        public override string ToString()
        {
            return _getLabelFunc();
        }

        public double Calculate(IEnumerable<double> values)
        {
            return _impl(values);
        }
    }

    public abstract class RtValueType
    {
        public static readonly RtValueType PEAK_APEXES = new PeakApexes();
        public static readonly RtValueType PSM_TIMES = new PsmTimes();
        public static readonly RtValueType IRT = new Irt();

        public static ImmutableList<RtValueType> ALL = ImmutableList.ValueOf(new[] { PEAK_APEXES, PSM_TIMES, IRT });

        public static IEnumerable<RtValueType> ForDocument(SrmDocument document)
        {
            return ALL.Where(rtValueType => rtValueType.IsAvailable(document));
        }

        public static RtValueType FromName(string name)
        {
            return ALL.FirstOrDefault(rtValueType => Equals(name, rtValueType.Name));
        }

        public virtual bool IsAvailable(SrmDocument document)
        {
            return true;
        }

        public abstract string GetConsenusName();

        public abstract IEnumerable<MsDataFileUri> ListTargets(SrmDocument document);

        public abstract string Name { get; }
        public abstract string Caption { get; }
        public override string ToString()
        {
            return Caption;
        }

        public IEnumerable<KeyValuePair<object, double>> GetRetentionTimesForFile(AverageType averageType,
            SrmDocument document, MsDataFileUri file)
        {
            return GetRetentionTimesForFile(document, file).Select(kvp =>
                new KeyValuePair<object, double>(kvp.Key, averageType.Calculate(kvp.Value)));
        }
        public abstract IEnumerable<KeyValuePair<object, IEnumerable<double>>> GetRetentionTimesForFile(SrmDocument document, MsDataFileUri file);

        public virtual IEnumerable<KeyValuePair<object, double>> GetSummaryRetentionTimes(SrmDocument document)
        {
            List<IEnumerable<KeyValuePair<object, double>>> fileTimes =
                new List<IEnumerable<KeyValuePair<object, double>>>();
            foreach (var source in ListTargets(document))
            {
                fileTimes.Add(GetRetentionTimesForFile(AverageType.MEAN, document, source));
            }

            if (fileTimes.Count == 0)
            {
                return Array.Empty<KeyValuePair<object, double>>();
            }

            if (fileTimes.Count == 1)
            {
                return fileTimes[0];
            }

            return fileTimes.SelectMany(times => times).GroupBy(kvp => kvp.Key, kvp => kvp.Value).Select(grouping =>
                new KeyValuePair<object, double>(grouping.Key, AverageType.MEAN.Calculate(grouping)));

        }


        private abstract class AbstractRtValueType<TMoleculeKey> : RtValueType
        {
            public override IEnumerable<KeyValuePair<object, IEnumerable<double>>> GetRetentionTimesForFile(
                SrmDocument document, MsDataFileUri file)
            {
                return GetMoleculeRetentionTimes(document, file).Select(grouping =>
                    new KeyValuePair<object, IEnumerable<double>>(grouping.Key, grouping.Value));
            }


            protected abstract IEnumerable<KeyValuePair<TMoleculeKey, IEnumerable<double>>> GetMoleculeRetentionTimes(SrmDocument document,
                MsDataFileUri file);
        }

        private class PeakApexes : AbstractRtValueType<PeptideModKey>
        {
            public override string Name
            {
                get { return @"peak_apexes"; }
            }

            public override string Caption
            {
                get
                {
                    return "Peak Apexes";
                }
            }

            protected override IEnumerable<KeyValuePair<PeptideModKey, IEnumerable<double>>> GetMoleculeRetentionTimes(SrmDocument document, MsDataFileUri source)
            {
                ReplicateFileId replicateFileId = ReplicateFileId.Find(document, source);
                if (replicateFileId == null)
                {
                    yield break;
                }
                foreach (var peptideGroup in document.Molecules.GroupBy(peptideDocNode => peptideDocNode.Key))
                {
                    var times = new List<double>();
                    foreach (var peptideDocNode in peptideGroup)
                    {
                        foreach (var peptideChromInfo in peptideDocNode.GetSafeChromInfo(replicateFileId.ReplicateIndex))
                        {
                            if (ReferenceEquals(peptideChromInfo.FileId, replicateFileId.FileId))
                            {
                                if (peptideChromInfo.RetentionTime.HasValue)
                                {
                                    times.Add(peptideChromInfo.RetentionTime.Value);
                                }
                            }
                        }
                    }

                    if (times.Count > 0)
                    {
                        yield return new KeyValuePair<PeptideModKey, IEnumerable<double>>(peptideGroup.Key, times);
                    }
                }
            }

            public override IEnumerable<MsDataFileUri> ListTargets(SrmDocument document)
            {
                return document.MeasuredResults?.Chromatograms.SelectMany(chrom =>
                    chrom.MSDataFilePaths) ?? Array.Empty<MsDataFileUri>().Prepend(MsDataFilePath.EMPTY);
            }

            public override string GetConsenusName()
            {
                return "Consensus Peak";
            }
        }

        private class PsmTimes : AbstractRtValueType<Target>
        {
            public override string Name
            {
                get { return @"psm_times"; }
            }

            public override string Caption
            {
                get
                {
                    return "PSM Times";
                }
            }

            public override string GetConsenusName()
            {
                return "Consensus PSM";
            }

            protected override IEnumerable<KeyValuePair<Target, IEnumerable<double>>> GetMoleculeRetentionTimes(SrmDocument document, MsDataFileUri source)
            {
                var retentionTimes = document.Settings.GetRetentionTimes(source);
                if (retentionTimes == null)
                {
                    return Array.Empty<KeyValuePair<Target, IEnumerable<double>>>();
                }
               
                return retentionTimes.GetFirstRetentionTimes().Select(kvp =>
                    new KeyValuePair<Target, IEnumerable<double>>(kvp.Key, new[] { kvp.Value }));
            }

            public override IEnumerable<MsDataFileUri> ListTargets(SrmDocument document)
            {
                return document.Settings.PeptideSettings.Libraries.Libraries.SelectMany(library =>
                    library.ListRetentionTimeSources().Select(source => new MsDataFilePath(source.Name)));
            }
        }

        private class Irt : AbstractRtValueType<Target>
        {
            public override string Name
            {
                get { return "irt"; }
            }
            public override string Caption
            {
                get { return "iRT"; }
            }

            private RCalcIrt GetCalcIrt(SrmDocument document)
            {
                return document.Settings.PeptideSettings?.Prediction?.RetentionTime?.Calculator as RCalcIrt;
            }

            public override bool IsAvailable(SrmDocument document)
            {
                return GetCalcIrt(document) != null;
            }

            protected override IEnumerable<KeyValuePair<Target, IEnumerable<double>>> GetMoleculeRetentionTimes(SrmDocument document, MsDataFileUri file)
            {
                var calculator = GetCalcIrt(document);
                if (calculator == null)
                {
                    return Array.Empty<KeyValuePair<Target, IEnumerable<double>>>();
                }

                return calculator.GetDbIrtPeptides().Where(pep => pep.Standard).Select(pep =>
                    new KeyValuePair<Target, IEnumerable<double>>(pep.ModifiedTarget, new [] { pep.Irt }));
            }

            public override IEnumerable<MsDataFileUri> ListTargets(SrmDocument document)
            {
                return Array.Empty<MsDataFileUri>();
            }

            public override string GetConsenusName()
            {
                return "iRT";
            }
        }
    }
}
