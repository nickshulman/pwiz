using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model
{
    public class ScanInfos : AbstractReadOnlyList<ScanInfo>
    {
        private IList<ScanInfo> _scanInfos;
        private IDictionary<ScanInfo.Type, ScanInfo.Type> scanTypeMap;
        public ScanInfos(IsolationScheme isolationScheme, IList<ScanInfo> scanInfos)
        {
            _scanInfos = scanInfos;
            IsolationScheme = isolationScheme;
        }

        public IsolationScheme IsolationScheme { get; private set; }

        public override int Count
        {
            get { return _scanInfos.Count; }
        }

        public override ScanInfo this[int index]
        {
            get
            {
                return ApplyIsolationScheme(_scanInfos[index]);
            }
        }

        private ScanInfo ApplyIsolationScheme(ScanInfo scanInfo)
        {
            if (IsolationScheme == null || IsolationScheme.FromResults && !IsolationScheme.UseMargin)
            {
                return scanInfo;
            }
            if (scanInfo.ScanType.MsLevel != 2)
            {
                return scanInfo;
            }

        }
    }
}
