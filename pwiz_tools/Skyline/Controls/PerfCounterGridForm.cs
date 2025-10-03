using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Common.SystemUtil.PerfCounters;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Controls
{
    public partial class PerfCounterGridForm : FormEx
    {
        private RowSource _rowSource;
        private long _workingSet;
        private long _availableBytes;
        private long _totalBytes;
        public PerfCounterGridForm()
        {
            InitializeComponent();
            _rowSource = new RowSource(GetPerfCounters(typeof(SkylinePerfCounters)));
            var viewSpec = GetBuiltInViews().ViewSpecs.First();
            var dataSchema = new DataSchema();
            var rowSourceInfo = new RowSourceInfo(typeof(Row), _rowSource,
                new[] { new ViewInfo(dataSchema, typeof(Row), viewSpec).ChangeViewGroup(ViewGroup.BUILT_IN) });
            var viewContext = new PerfCounterViewContext(dataSchema, rowSourceInfo);
            bindingListSource1.SetViewContext(viewContext, rowSourceInfo.Views.First());
            UpdateNow();
        }

        private class Row
        {
            public Row(string name, PerfCounter perfCounter)
            {
                Name = name;
                Counts = new FormattedPerfCounts(perfCounter.GetCounts());
                Children = perfCounter.GetDetails().Select(detail => new Row(detail.Key, detail.Value)).ToImmutable();
            }
            public string Name { get; }
            public FormattedPerfCounts Counts { get; }
            public ImmutableList<Row> Children { get; }
        }

        private class FormattedPerfCounts : IComparable
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

            public int CompareTo(object other)
            {
                return _perfCounts.CompareTo(((FormattedPerfCounts) other)?._perfCounts);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateNow();
        }

        public void UpdateNow()
        {
            _rowSource.Update();
            _workingSet = Process.GetCurrentProcess().WorkingSet64;
            _totalBytes = MemoryInfo.TotalBytes;
            _availableBytes = MemoryInfo.AvailableBytes;
            lblMemoryUsage.Text = string.Format("Memory used by Skyline: {0:N0} All applications: {1:N0} Total available: {2:N0} MB",
                _workingSet / 1024 / 1024, (_totalBytes - _availableBytes) / 1024 / 1024, _totalBytes / 1024 / 1024);
            panelMemoryBar.Invalidate();
        }

        private class RowSource : AbstractRowSource
        {
            private List<Row> _rows = new List<Row>();

            public RowSource(IEnumerable<KeyValuePair<string, PerfCounter>> perfCounters)
            {
                PerfCounters = perfCounters.ToImmutable();
            }

            public ImmutableList<KeyValuePair<string, PerfCounter>> PerfCounters { get; }
            public void Update()
            {
                _rows = PerfCounters.Select(counter => new Row(counter.Key, counter.Value)).ToList();
                FireListChanged();
                
            }

            public override IEnumerable GetItems()
            {
                return _rows;
            }
        }

        private void btnGarbageCollect_Click(object sender, EventArgs e)
        {
            CollectGarbage();
        }

        public void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            UpdateNow();
        }

        public static void ShowForm()
        {
            var existing = FormUtil.OpenForms.OfType<PerfCounterGridForm>().FirstOrDefault();
            if (existing != null)
            {
                CommonActionUtil.SafeBeginInvoke(existing, () => existing.Activate());
                return;
            }

            ActionUtil.RunAsync(() =>
            {
                using var form = new PerfCounterGridForm();
                form.ShowParentlessDialog();
            }, nameof(PerfCounterGridForm));
        }

        public static IEnumerable<KeyValuePair<string, PerfCounter>> GetPerfCounters(Type type)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                PerfCounter perfCounter = null;
                if (member is FieldInfo field)
                {
                    perfCounter = field.GetValue(null) as PerfCounter;
                }
                else if (member is PropertyInfo property)
                {
                    perfCounter = property.GetValue(null) as PerfCounter;
                }

                if (perfCounter != null)
                {
                    yield return new KeyValuePair<string, PerfCounter>(member.Name, perfCounter);
                }
            }
        }

        private class PerfCounterViewContext : BaseSkylineViewContext
        {
            public PerfCounterViewContext(DataSchema dataSchema, RowSourceInfo rowSourceInfo) : base(dataSchema,
                new[] { rowSourceInfo })
            {
            }

            public override ViewSpecList GetViewSpecList(ViewGroupId viewGroup)
            {
                if (Equals(viewGroup, ViewGroup.BUILT_IN.Id))
                {
                    return GetBuiltInViews();
                }

                return base.GetViewSpecList(viewGroup);
            }
        }

        public static ViewSpecList GetBuiltInViews()
        {
            return new ViewSpecList(new[]
                {
                    new ViewSpec().SetName("Counters").SetRowType(typeof(Row)).SetColumns(new[]
                    {
                        nameof(Row.Name),
                        nameof(Row.Counts),

                    }.Select(name => new ColumnSpec(PropertyPath.Root.Property(name))))
                },
                new []
                {
                    new ViewLayoutList("Counters").ChangeLayouts(new []
                    {
                        new ViewLayout("Recent Activity").ChangeColumnFormats(new []
                        {
                            Tuple.Create(new ColumnId(nameof(Row.Name)), ColumnFormat.EMPTY.ChangeWidth(250)),
                            Tuple.Create(new ColumnId(nameof(Row.Counts)), ColumnFormat.EMPTY.ChangeWidth(500))

                        }).ChangeRowTransforms(new []
                        {
                            RowFilter.Empty.SetColumnSorts(new[]{new RowFilter.ColumnSort(new ColumnId(nameof(Row.Counts)), ListSortDirection.Descending)})
                        })
                    }).ChangeDefaultLayoutName("Recent Activity")
                }
            );
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetCounters();
            UpdateNow();
        }

        public void ResetCounters()
        {
            foreach (var counter in _rowSource.PerfCounters)
            {
                counter.Value.Reset();
            }
        }

        private void panelMemoryBar_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var skylineWidth = panelMemoryBar.Width * _workingSet / _totalBytes;
            var usedWidth = panelMemoryBar.Width * (_totalBytes - _availableBytes) / _totalBytes;
            e.Graphics.FillRectangle(new SolidBrush(Color.LightGray), 0, 0, panelMemoryBar.Width, panelMemoryBar.Height);
            e.Graphics.FillRectangle(new SolidBrush(Color.Orange), 0, 0, usedWidth, panelMemoryBar.Height);
            e.Graphics.FillRectangle(new SolidBrush(Color.DodgerBlue), 0, 0, skylineWidth, panelMemoryBar.Height);
        }

        private void panelMemoryBar_SizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
    }
}
