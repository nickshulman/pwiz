using System.Collections.Generic;
using pwiz.ProteowizardWrapper;

namespace pwiz.Skyline.Model.Results
{
    public sealed class GlobalChromatogramExtractor
    {
        private MsDataFileImpl _dataFile;
        private const string TIC_CHROMATOGRAM_ID = @"TIC";
        private const string BPC_CHROMATOGRAM_ID = @"BPC";

        public GlobalChromatogramExtractor(MsDataFileImpl dataFile)
        {
            _dataFile = dataFile;

            if (dataFile.ChromatogramCount > 0 && dataFile.GetChromatogramId(0, out _) == TIC_CHROMATOGRAM_ID)
                TicChromatogramIndex = 0;
            if (dataFile.ChromatogramCount > 1 && dataFile.GetChromatogramId(1, out _) == BPC_CHROMATOGRAM_ID)
                BpcChromatogramIndex = 1;

            QcTraceByIndex = new SortedDictionary<int, MsDataFileImpl.QcTrace>();
            foreach (var qcTrace in dataFile.GetQcTraces() ?? new List<MsDataFileImpl.QcTrace>())
            {
                QcTraceByIndex[qcTrace.Index] = qcTrace;
            }
        }

        public int? TicChromatogramIndex { get; set; }
        public int? BpcChromatogramIndex { get; }

        public IDictionary<int, MsDataFileImpl.QcTrace> QcTraceByIndex { get; }

        public string GetChromatogramId(int index, out int indexId)
        {
            return _dataFile.GetChromatogramId(index, out indexId);
        }

        public bool GetChromatogram(int index, out float[] times, out float[] intensities)
        {
            if (QcTraceByIndex.TryGetValue(index, out MsDataFileImpl.QcTrace qcTrace))
            {
                times = MsDataFileImpl.ToFloatArray(qcTrace.Times);
                intensities = MsDataFileImpl.ToFloatArray(qcTrace.Intensities);
            }
            else if (index == TicChromatogramIndex || index == BpcChromatogramIndex)
            {
                _dataFile.GetChromatogram(index, out _, out times, out intensities, true);
            }
            else
            {
                times = intensities = null;
            }

            return times != null;
        }

        /// <summary>
        /// Returns true if the TIC chromatogram present in the .raw file can be relied on
        /// for the calculation of total MS1 ion current.
        /// </summary>
        public bool IsTicChromatogramUsable()
        {
            if (!TicChromatogramIndex.HasValue)
            {
                return false;
            }

            float[] times;
            if (!GetChromatogram(TicChromatogramIndex.Value, out times, out _))
            {
                return false;
            }

            if (times.Length == 0)
            {
                return false;
            }

            return true;
        }
    }
}