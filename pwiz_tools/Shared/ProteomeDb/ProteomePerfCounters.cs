using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.SystemUtil.PerfCounters;

namespace pwiz.ProteomeDatabase
{
    public static class ProteomePerfCounters
    {
        public static readonly PerfCounter FetchProteinMetadata = new PerfCounter("FetchProteinMetadata");
    }
}
