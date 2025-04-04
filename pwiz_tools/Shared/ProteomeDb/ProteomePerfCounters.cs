using pwiz.Common.SystemUtil.PerfCounters;

namespace pwiz.ProteomeDatabase
{
    public static class ProteomePerfCounters
    {
        public static readonly PerfCounter FetchProteinMetadata = new PerfCounter();
    }
}
