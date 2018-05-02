using System.Collections.Generic;
using pwiz.Common.Collections;

namespace TopographTool.Model
{
    public class ScanInfo
    {
        public ScanInfo(IEnumerable<IsolationWindow> isolationWindows)
        {
            IsolationWindows = ImmutableList.ValueOf(isolationWindows);
        }

        public ImmutableList<IsolationWindow> IsolationWindows { get; private set; }
        
    }

}
