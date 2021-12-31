using System.Collections.Generic;
using System.Runtime.CompilerServices;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Skydb
{
    public class SpectrumIdMap : IMsDataFileScanIds
    {
        private Dictionary<string, int> _spectrumIdToScanId = new Dictionary<string, int>();
        private List<string> _scanIdToSpectrumId = new List<string>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int GetScanId(string spectrumId)
        {
            if (!_spectrumIdToScanId.TryGetValue(spectrumId, out int scanId))
            {
                scanId = _scanIdToSpectrumId.Count;
                _spectrumIdToScanId.Add(spectrumId, scanId);
                _scanIdToSpectrumId.Add(spectrumId);
            }
            return scanId;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetSpectrumId(int scanId)
        {
            return _scanIdToSpectrumId[scanId];
        }

        string IMsDataFileScanIds.GetMsDataFileSpectrumId(int index)
        {
            return GetSpectrumId(index);
        }
    }
}
