using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopographTool.Model.DataRows
{
    public class ScanInfoRow
    {
        public string ReplicateLocator { get; private set; }
        public string ResultFileLocator { get; private set; }
        public string RetentionTimes { get; private set; }
        public string IsolationWindowTargets { get; private set; }
        public string IsolationWindowLowerOffsets { get; private set; }
        public string IsolationWindowUpperOffsets { get; private set; }
        public string ReplicateName { get; private set; }
        public string Condition { get; private set; }
        public double? TimePoint { get; private set; }
    }
}
