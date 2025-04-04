
using System.Collections.Generic;
using pwiz.Common.SystemUtil.PerfCounters;
using pwiz.ProteomeDatabase;

namespace pwiz.Skyline.Model
{
    public static class SkylinePerfCounters
    {
        public static readonly PerfCounter ReadFile = new PerfCounter("ReadFile");
        public static readonly PerfCounter ReadChromPeak = new PerfCounter("ReadChromPeak");
        public static readonly PerfCounter ReadChromScores = new PerfCounter("ReadChromScores");

        public static IEnumerable<PerfCounter> All
        {
            get
            {
                return new[]
                {
                    ReadFile,
                    ReadChromPeak,
                    ProteomePerfCounters.FetchProteinMetadata
                };
            }
        }
    }
}
