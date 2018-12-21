using System.Collections.Generic;
using System.IO;
using System.Threading;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Util.Extensions;
using BindingListSource = pwiz.Common.DataBinding.Controls.BindingListSource;

namespace pwiz.Skyline.Model.Sharing
{
    public class DocumentReports : MustDispose
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public DocumentReports(IProgressMonitor progressMonitor, SkylineDataSchema dataSchema)
        {
            ProgressMonitor = progressMonitor;
            DataSchema = dataSchema;
            ActionUtil.RunAsync(CheckCancelledMethod, @"DocumentReportsCheckCancelled");
        }

        public IProgressMonitor ProgressMonitor { get; private set; }
        public SkylineDataSchema DataSchema { get; private set; }

        public bool AddElementLocators { get; set; }

        public IEnumerable<ReportExporter> GetReportExporters(ViewSpecList viewSpecList)
        {
            foreach (var viewSpec in viewSpecList.ViewSpecs)
            {
                ViewLayout viewLayout = null;
                var viewLayouts = viewSpecList.GetViewLayouts(viewSpec.Name);
                if (!string.IsNullOrEmpty(viewLayouts.DefaultLayoutName))
                {
                    viewLayout = viewLayouts.FindLayout(viewLayouts.DefaultLayoutName);
                }

                var bindingListSource = MakeBindingListSource(viewSpec, viewLayout);
                if (bindingListSource == null)
                {
                    continue;
                }

                yield return new ReportExporter(viewSpec.Name, bindingListSource);
            }
        }

        public BindingListSource MakeBindingListSource(ViewSpec viewSpec, ViewLayout viewLayout)
        {
            BindingListSource bindingListSource = new BindingListSource(_cancellationTokenSource.Token);
            foreach (var rowSource in SkylineViewContext.GetDocumentGridRowSources(DataSchema))
            {
                if (rowSource.Name == viewSpec.RowSource)
                {
                    var parentColumn = ColumnDescriptor.RootColumn(DataSchema, rowSource.RowType);
                    var viewInfo = new ViewInfo(parentColumn, viewSpec).ChangeViewGroup(ViewGroup.BUILT_IN);
                    var viewContext = new SharingViewContext(viewInfo, viewLayout, rowSource.Rows.GetItems());
                    bindingListSource.SetViewContext(viewContext, viewInfo);
                    return bindingListSource;
                }
            }

            return null;
        }

        private void CheckCancelledMethod()
        {
            while (!IsDisposed())
            {
                if (ProgressMonitor.IsCanceled)
                {
                    _cancellationTokenSource.Cancel();
                    return;
                }
                Thread.Sleep(100);
            }
        }

        public void WriteElementNames(TextWriter writer)
        {
            
        }
    }
}
