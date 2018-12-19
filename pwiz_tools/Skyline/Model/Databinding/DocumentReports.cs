using System.Collections.Generic;
using System.IO;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding
{
    public class DocumentReports
    {
        public DocumentReports(IProgressMonitor progresMonitor, SkylineDataSchema dataSchema)
        {
            ProgressMonitor = progresMonitor;
            DataSchema = dataSchema;
        }

        public IProgressMonitor ProgressMonitor { get; private set; }
        public SkylineDataSchema DataSchema { get; private set; }

        public bool AddElementLocators { get; set; }

        public void WriteReport(ViewSpec viewSpec, ViewLayout viewLayout, string subName, TextWriter textWriter)
        {
            var bindingListSource = new BindingListSource();
            GetSkylineViewContext(bindingListSource, viewSpec, subName);
            if (viewLayout != null)
            {
                bindingListSource.ApplyLayout(viewLayout);
            }
            var skylineViewContext = (SkylineViewContext) bindingListSource.ViewContext;
            skylineViewContext.WriteToStream(ProgressMonitor, bindingListSource, skylineViewContext.GetDsvWriter(TextUtil.SEPARATOR_CSV), textWriter);
        }

        public IEnumerable<string> GetSubNames(ViewSpec viewSpec)
        {
            foreach (var rowSource in SkylineViewContext.GetDocumentGridRowSources(DataSchema))
            {
                if (rowSource.Name == viewSpec.RowSource)
                {
                    yield return string.Empty;
                    yield break;
                }
            }
        }

        public bool GetSkylineViewContext(BindingListSource bindingListSource, ViewSpec viewSpec, string name)
        {
            SkylineViewContext viewContext;
            foreach (var rowSource in SkylineViewContext.GetDocumentGridRowSources(DataSchema))
            {
                if (rowSource.Name == viewSpec.RowSource)
                {
                    var parentColumn = ColumnDescriptor.RootColumn(DataSchema, rowSource.RowType);
                    viewContext = new SkylineViewContext(parentColumn, rowSource.Rows);
                    var viewInfo = new ViewInfo(parentColumn, viewSpec);
                    bindingListSource.SetViewContext(viewContext, viewInfo);
                    return true;
                }
            }

            return false;
        }
    }
}
