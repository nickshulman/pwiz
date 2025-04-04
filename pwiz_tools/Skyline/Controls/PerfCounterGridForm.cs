using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.SystemUtil.PerfCounters;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Controls
{
    public partial class PerfCounterGridForm : FormEx
    {
        private RowSource _rowSource = new RowSource();
        public PerfCounterGridForm()
        {
            InitializeComponent();
            var dataSchema = new DataSchema();
            var viewSpec = new ViewSpec().SetName("default").SetColumns(new []
            {
                nameof(Row.Name),
                nameof(Row.Counts),

            }.Select(name=>new ColumnSpec(PropertyPath.Root.Property(name))));
            var rowSourceInfo = new RowSourceInfo(typeof(Row), _rowSource,
                new[] { new ViewInfo(dataSchema, typeof(Row), viewSpec).ChangeViewGroup(ViewGroup.BUILT_IN) });
            var viewContext = new BaseSkylineViewContext(dataSchema, new []{rowSourceInfo});
            bindingListSource1.SetViewContext(viewContext, rowSourceInfo.Views.First());
            ShowInTaskbar = true;
        }

        private class Row
        {
            public Row(PerfCounter perfCounter)
            {
                Name = perfCounter.Name;
                Counts = new FormattedPerfCounts(perfCounter.GetCounts());
            }
            public string Name { get; }
            public FormattedPerfCounts Counts { get; }
        }

        private class FormattedPerfCounts : IComparable<FormattedPerfCounts>
        {
            private PerfCounts _perfCounts;
            public FormattedPerfCounts(PerfCounts perfCounts)
            {
                _perfCounts = perfCounts;
            }

            [ChildDisplayName("LastMinute{0}")]
            public PerfQuantity LastMinute
            {
                get { return _perfCounts.LastMinute; }
            }
            [ChildDisplayName("LastHour{0}")]
            public PerfQuantity LastHour
            {
                get { return _perfCounts.LastHour; }
            }
            [ChildDisplayName("LastDay{0}")]
            public PerfQuantity LastDay
            {
                get { return _perfCounts.LastDay; }
            }
            [ChildDisplayName("Total{0}")]
            public PerfQuantity Total
            {
                get { return _perfCounts.Total; }
            }

            public override string ToString()
            {
                return _perfCounts.ToString();
            }

            public int CompareTo(FormattedPerfCounts other)
            {
                return _perfCounts.CompareTo(other._perfCounts);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _rowSource.Update();
        }

        private class RowSource : AbstractRowSource
        {
            private List<Row> _rows = new List<Row>();
            public void Update()
            {
                _rows = GetPerfCounters().Select(counter => new Row(counter)).ToList();
                FireListChanged();
                
            }

            public override IEnumerable GetItems()
            {
                return _rows;
            }
        }

        private static IEnumerable<PerfCounter> GetPerfCounters()
        {
            return SkylinePerfCounters.All;
        }
    }
}
