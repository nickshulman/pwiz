using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Google.Protobuf;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.ProteowizardWrapper;
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

        public string ScanIdentifier
        {
            get { return _scanIdentifierText ?? _scanIdentifierInt.ToString(CultureInfo.InvariantCulture); }
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
        }
        public class Type : Immutable
        {
            public static readonly Type UNKNOWN = new Type(0, null);
            public Type(int msLevel, IEnumerable<IsolationWindow> isolationWindows)
            {
                MsLevel = msLevel;
                IsolationWindows = ImmutableList.ValueOfOrEmpty(isolationWindows);
            }

            public Type(ResultFileDataProto.Types.ScanType typeProto)
            {
                MsLevel = typeProto.MsLevel;
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
                unchecked
                {
                    return (MsLevel * 397) ^ (IsolationWindows != null ? IsolationWindows.GetHashCode() : 0);
                }
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
