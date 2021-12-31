using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.Skydb
{
    public class SpectrumIdMap
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
    }
}
