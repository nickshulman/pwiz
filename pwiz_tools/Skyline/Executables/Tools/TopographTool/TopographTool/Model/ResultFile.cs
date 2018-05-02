using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.Collections;
using TopographTool.Model.DataRows;

namespace TopographTool.Model
{
    public class ResultFile
    {
        public ResultFile(Replicate replicate, ScanInfoRow scanInfoRow)
        {
            Replicate = replicate;
            ResultFileLocator = scanInfoRow.ResultFileLocator;
            ReplicateLocator = scanInfoRow.ReplicateLocator;
            RetentionTimes = ImmutableList.ValueOf(RowReader.ParseDoubles(scanInfoRow.RetentionTimes));
            var targetMzs = RowReader.ParseDoubleArrays(scanInfoRow.IsolationWindowTargets).ToArray();
            var lowerOffsets = RowReader.ParseDoubleArrays(scanInfoRow.IsolationWindowLowerOffsets).ToArray();
            var upperOffsets = RowReader.ParseDoubleArrays(scanInfoRow.IsolationWindowUpperOffsets).ToArray();
            var scanInfos = new List<ScanInfo>();
            for (int scanIndex = 0; scanIndex < targetMzs.Length; scanIndex++)
            {
                var isolationWindows = new List<IsolationWindow>();
                var targets = targetMzs[scanIndex];

                for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    isolationWindows.Add(new IsolationWindow(targets[targetIndex] - lowerOffsets[scanIndex][targetIndex], 
                        targets[targetIndex] + upperOffsets[scanIndex][targetIndex]));
                }
                scanInfos.Add(new ScanInfo(isolationWindows));
            }
            ScanInfos = ImmutableList.ValueOf(scanInfos);
        }
        public string ResultFileLocator { get; private set; }
        public string ReplicateLocator { get; private set; }
        public ImmutableList<double> RetentionTimes { get; private set; }
        public ImmutableList<ScanInfo> ScanInfos { get; private set; }
        public Replicate Replicate { get; private set; }
        public string Name { get { return Replicate.Name; } }
    }
}
