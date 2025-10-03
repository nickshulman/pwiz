
using JetBrains.Annotations;
using pwiz.Common.SystemUtil.PerfCounters;
using pwiz.ProteomeDatabase;

namespace pwiz.Skyline.Model
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public static class SkylinePerfCounters
    {
        public static readonly PerfCounter ReadSkydFile = new PerfCounter();
        public static readonly PerfCounter ReadLibrarySpectrum = new PerfCounter();
        // public static readonly PerfCounter CalculateMassDistribution = new PerfCounter();
        // public static readonly PerfCounter CalcTransitionGroupResult = new PerfCounter();
        public static readonly PerfCounter FetchProteinMetadata = ProteomePerfCounters.FetchProteinMetadata;
    }
}
