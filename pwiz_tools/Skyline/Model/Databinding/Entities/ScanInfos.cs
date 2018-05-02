using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.Controls;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public class ScanInfos
    {
        private IList<ScanInfo> _scanInfos;
        public ScanInfos(IList<ScanInfo> scanInfos)
        {
            _scanInfos = scanInfos;
        }

        [DataGridViewColumnType(typeof(LongTextColumn))]
        public FormattableList<double> RetentionTimes
        {
            get
            {
                return new FormattableList<double>(
                    ReadOnlyList.Create(_scanInfos.Count, i => _scanInfos[i].RetentionTime));
            }
        }

        [DataGridViewColumnType(typeof(LongTextColumn))]
        public FormattableList<FormattableList<double>> IsolationWindowTargets
        {
            get
            {
                return new FormattableList<FormattableList<double>>(ReadOnlyList.Create(_scanInfos.Count, i =>
                    MakeSubList(_scanInfos[i].ScanType.IsolationWindows.Select(w => w.TargetMz))
                ));
            }
        }

        [DataGridViewColumnType(typeof(LongTextColumn))]
        public FormattableList<FormattableList<double>> IsolationWindowLowerOffsets
        {
            get
            {
                return new FormattableList<FormattableList<double>>(ReadOnlyList.Create(_scanInfos.Count, i=>
                    MakeSubList(_scanInfos[i].ScanType.IsolationWindows.Select(w=>w.LowerOffset))));
            }
        }

        [DataGridViewColumnType(typeof(LongTextColumn))]
        public FormattableList<FormattableList<double>> IsolationWindowUpperOffsets
        {
            get
            {
                return new FormattableList<FormattableList<double>>(ReadOnlyList.Create(_scanInfos.Count, i =>
                    MakeSubList(_scanInfos[i].ScanType.IsolationWindows.Select(w => w.UpperOffset))));
            }
        }

        private static FormattableList<double> MakeSubList(IEnumerable<double> values)
        {
            return new FormattableList<double>(ImmutableList.ValueOfOrEmpty(values), "[", "]");
        }
    }
}
