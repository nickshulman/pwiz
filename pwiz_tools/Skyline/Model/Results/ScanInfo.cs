using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Google.Protobuf;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results.ProtoBuf;

namespace pwiz.Skyline.Model.Results
{
    public class ScanInfo : Immutable
    {
        private int _scanIdentifierInt;
        private string _scanIdentifierText;

        private ScanInfo(int index)
        {
            ScanIndex = index;
        }
        public ScanInfo(int index, MsDataSpectrum msDataSpectrum) : this(index)
        {
            ScanType = new Type(msDataSpectrum.Level,
                msDataSpectrum.Precursors.Select(precursor => new IsolationWindow(precursor)));
            RetentionTime = msDataSpectrum.RetentionTime.GetValueOrDefault();
            int identifierInt;
            if (int.TryParse(msDataSpectrum.Id, 0, CultureInfo.InvariantCulture, out identifierInt) &&
                identifierInt.ToString(CultureInfo.InvariantCulture) == msDataSpectrum.Id)
            {
                _scanIdentifierInt = identifierInt;
                _scanIdentifierText = string.Empty;
            }
            else
            {
                _scanIdentifierText = msDataSpectrum.Id;
                _scanIdentifierInt = 0;
            }
        }
        public int ScanIndex { get; private set; }

        public ScanInfo ChangeScanIndex(int scanIndex)
        {
            return ChangeProp(ImClone(this), im => im.ScanIndex = scanIndex);
        }
        public double RetentionTime { get; private set; }
        public Type ScanType { get; private set; }

        public ScanInfo ChangeScanType(Type type)
        {
            return ChangeProp(ImClone(this), im => ScanType = type);
        }

        public string ScanIdentifier
        {
            get { return _scanIdentifierText ?? _scanIdentifierInt.ToString(CultureInfo.InvariantCulture); }
        }

        protected bool Equals(ScanInfo other)
        {
            return _scanIdentifierInt == other._scanIdentifierInt &&
                   string.Equals(_scanIdentifierText, other._scanIdentifierText) && ScanIndex == other.ScanIndex &&
                   RetentionTime.Equals(other.RetentionTime) && Equals(ScanType, other.ScanType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ScanInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _scanIdentifierInt;
                hashCode = (hashCode * 397) ^ (_scanIdentifierText != null ? _scanIdentifierText.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ScanIndex;
                hashCode = (hashCode * 397) ^ RetentionTime.GetHashCode();
                hashCode = (hashCode * 397) ^ (ScanType != null ? ScanType.GetHashCode() : 0);
                return hashCode;
            }
        }

        public ResultFileDataProto.Types.ScanInfo ToScanInfoProto(
            ResultFileDataProto resultFileDataProto, IDictionary<Type, int> scanTypeIndexes)
        {
            var proto = new ResultFileDataProto.Types.ScanInfo
            {
                RetentionTime = RetentionTime,
                ScanIdentifierInt = _scanIdentifierInt,
                ScanIdentifierText = _scanIdentifierText ?? String.Empty,
            };
            int scanTypeIndex;
            if (scanTypeIndexes.TryGetValue(ScanType, out scanTypeIndex))
            {
                proto.ScanTypeIndex = scanTypeIndex;
            }
            else
            {
                scanTypeIndex = resultFileDataProto.ScanTypes.Count;
                scanTypeIndexes.Add(ScanType, scanTypeIndex);
                resultFileDataProto.ScanTypes.Add(ScanType.ToScanTypeProto());
                proto.ScanTypeIndex = scanTypeIndex;
            }
            return proto;
        }
        public class IsolationWindow : Immutable
        {
            public IsolationWindow(double targetMz)
            {
                TargetMz = targetMz;
            }

            public IsolationWindow(MsPrecursor msPrecursor)
            {
                TargetMz = msPrecursor.IsolationMz ?? msPrecursor.PrecursorMz.Value;
                LowerOffset = msPrecursor.IsolationWindowLower.GetValueOrDefault();
                UpperOffset = msPrecursor.IsolationWindowUpper.GetValueOrDefault();
            }

            public IsolationWindow(ResultFileDataProto.Types.ScanType.Types.IsolationWindow isolationWindowProto)
            {
                TargetMz = isolationWindowProto.TargetMz;
                LowerOffset = isolationWindowProto.LowerOffset;
                UpperOffset = isolationWindowProto.UpperOffset;
            }
            public double TargetMz { get; private set; }
            public double LowerOffset { get; private set; }

            public IsolationWindow ChangeLowerOffset(double lowerOffset)
            {
                return ChangeProp(ImClone(this), im => im.LowerOffset = lowerOffset);
            }
            public double UpperOffset { get; private set; }

            public IsolationWindow ChangeUpperOffset(double upperOffset)
            {
                return ChangeProp(ImClone(this), im => im.UpperOffset = upperOffset);
            }

            protected bool Equals(IsolationWindow other)
            {
                return TargetMz.Equals(other.TargetMz) && LowerOffset.Equals(other.LowerOffset) && UpperOffset.Equals(other.UpperOffset);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((IsolationWindow)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = TargetMz.GetHashCode();
                    hashCode = (hashCode * 397) ^ LowerOffset.GetHashCode();
                    hashCode = (hashCode * 397) ^ UpperOffset.GetHashCode();
                    return hashCode;
                }
            }

            public IsolationWindow ApplyIsolationScheme(TransitionSettings transitionSettings)
            {
                var isolationScheme = transitionSettings.FullScan.IsolationScheme;
                var myIsolationWidth = LowerOffset + UpperOffset;
                var isolationTargetMz = TargetMz;
                if (isolationScheme.FromResults)
                {
                    if (!isolationScheme.UseMargin)
                    {
                        return this;
                    }
                    return new IsolationWindow(TargetMz)
                        .ChangeLowerOffset(LowerOffset - isolationScheme.PrecursorFilter.GetValueOrDefault())
                        .ChangeUpperOffset(UpperOffset - isolationScheme.PrecursorRightFilter.GetValueOrDefault(
                            isolationScheme.PrecursorFilter.GetValueOrDefault()));
                }
                if (isolationScheme.PrecursorFilter.HasValue && !isolationScheme.UseMargin)
                {
                    // Use the user specified isolation width, unless it is larger than
                    // the acquisition isolation width.  In this case the chromatograms
                    // may be very confusing (spikey), because of incorrectly included
                    // data points.
                    var isolationWidthValue = isolationScheme.PrecursorFilter.Value +
                                              (isolationScheme.PrecursorRightFilter ?? 0);

                    if (myIsolationWidth > 0 && myIsolationWidth < isolationWidthValue)
                        isolationWidthValue = myIsolationWidth;

                    // Make sure the isolation target is centered in the desired window, even
                    // if the window was specified as being asymetric
                    if (isolationScheme.PrecursorRightFilter.HasValue)
                    {
                        isolationTargetMz += isolationScheme.PrecursorRightFilter.Value - isolationWidthValue / 2;
                    }
                    return new IsolationWindow(isolationTargetMz).ChangeLowerOffset(isolationWidthValue / 2)
                        .ChangeUpperOffset(isolationWidthValue / 2);
                }
                if (isolationScheme.PrespecifiedIsolationWindows.Count > 0)
                {
                    var isolationWindow = isolationScheme.GetIsolationWindow(isolationTargetMz, transitionSettings.Instrument.MzMatchTolerance);
                    if (isolationWindow != null)
                    {
                        var isolationHalfWidth = (isolationWindow.End - isolationWindow.Start)/2;
                        return new IsolationWindow(isolationWindow.Start + isolationHalfWidth)
                            .ChangeLowerOffset(isolationHalfWidth).ChangeUpperOffset(isolationHalfWidth);
                    }
                }
                return this;
            }
        }
        public class Type : Immutable
        {
            public static readonly Type UNKNOWN = new Type(0, null);
            private readonly int _hashCode;
            public Type(int msLevel, IEnumerable<IsolationWindow> isolationWindows)
            {
                MsLevel = msLevel;
                IsolationWindows = ImmutableList.ValueOfOrEmpty(isolationWindows);
                _hashCode = MsLevel.GetHashCode() * 397 ^ IsolationWindows.GetHashCode();
            }

            public Type(ResultFileDataProto.Types.ScanType typeProto)
            {
                MsLevel = typeProto.MsLevel;
                IsolationWindows = ImmutableList.ValueOfOrEmpty(typeProto.IsolationWindows.Select(w=>new IsolationWindow(w)));
            }

            public int MsLevel { get; private set; }
            public ImmutableList<IsolationWindow> IsolationWindows { get; private set; }

            public ResultFileDataProto.Types.ScanType ToScanTypeProto()
            {
                var proto = new ResultFileDataProto.Types.ScanType
                {
                    MsLevel = MsLevel
                };
                foreach (var isolationWindow in IsolationWindows)
                {
                    proto.IsolationWindows.Add(new ResultFileDataProto.Types.ScanType.Types.IsolationWindow()
                    {
                        TargetMz = isolationWindow.TargetMz,
                        LowerOffset = isolationWindow.LowerOffset,
                        UpperOffset = isolationWindow.UpperOffset
                    });
                }
                return proto;
            }

            protected bool Equals(Type other)
            {
                return MsLevel == other.MsLevel && Equals(IsolationWindows, other.IsolationWindows);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Type) obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public Type ApplyIsolationScheme(TransitionSettings transitionSettings)
            {
                var isolationScheme = transitionSettings.FullScan.IsolationScheme;
                if (isolationScheme == null || isolationScheme.FromResults && !isolationScheme.UseMargin)
                {
                    return this;
                }
                if (MsLevel != 2)
                {
                    return this;
                }

                var newWindows = ImmutableList.ValueOf(IsolationWindows.Select(w => w.ApplyIsolationScheme(transitionSettings)));
                if (Equals(newWindows, IsolationWindows))
                {
                    return this;
                }
                return new Type(MsLevel, newWindows);
            }
        }
        public static ResultFileDataProto ToResultFileDataProto(IEnumerable<ScanInfo> scanInfos)
        {
            var proto = new ResultFileDataProto();
            IDictionary<Type, int> scanTypeIndexes = new Dictionary<Type, int>();
            foreach (var scanInfo in scanInfos)
            {
                proto.ScanInfos.Add(scanInfo.ToScanInfoProto(proto, scanTypeIndexes));
            }
            return proto;
        }

        public static byte[] ToBytes(IEnumerable<ScanInfo> scanInfos)
        {
            var memoryStream = new MemoryStream();
            var proto = ToResultFileDataProto(scanInfos);
            proto.WriteTo(memoryStream);
            return memoryStream.ToArray();
        }

        public static IEnumerable<ScanInfo> FromBytes(byte[] bytes)
        {
            var proto = new ResultFileDataProto();
            proto.MergeFrom(new MemoryStream(bytes));
            var scanTypes = proto.ScanTypes.Select(scanType => new Type(scanType)).ToArray();
            return proto.ScanInfos.Select((scanInfo, index) => new ScanInfo(index)
            {
                ScanType = scanTypes[scanInfo.ScanTypeIndex],
                _scanIdentifierText = scanInfo.ScanIdentifierText,
                _scanIdentifierInt = scanInfo.ScanIdentifierInt,
                RetentionTime = scanInfo.RetentionTime,
            });
        }
    }
}
