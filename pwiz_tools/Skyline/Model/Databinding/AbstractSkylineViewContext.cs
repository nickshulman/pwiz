using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding
{
    public abstract class AbstractSkylineViewContext : AbstractViewContext
    {
        public AbstractSkylineViewContext(DataSchema dataSchema, IEnumerable<RowSourceInfo> rowSources) : base(dataSchema, rowSources)
        {
        }


        public override bool RunLongJob(Control owner, Action<CancellationToken, IProgressMonitor> job)
        {
            using (var longWaitDlg = new LongWaitDlg())
            {
                var status = longWaitDlg.PerformWork(FormUtil.FindTopLevelOwner(owner), 1000, progressMonitor => job(longWaitDlg.CancellationToken, progressMonitor));
                return status.IsComplete;
            }
        }


        protected override void SaveViewSpecList(ViewGroupId viewGroup, ViewSpecList viewSpecList)
        {
            Settings.Default.PersistedViews.SetViewSpecList(viewGroup, viewSpecList);
        }

        public override string GetExportDirectory()
        {
            return Settings.Default.ExportDirectory;
        }

        public override void SetExportDirectory(string value)
        {
            Settings.Default.ExportDirectory = value;
        }

        public override DialogResult ShowMessageBox(Control owner, string message, MessageBoxButtons messageBoxButtons)
        {
            return new AlertDlg(message, messageBoxButtons).ShowAndDispose(FormUtil.FindTopLevelOwner(owner));
        }

        public override void ExportViews(Control owner, ViewSpecList viewSpecList)
        {
            using (var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Settings.Default.ActiveDirectory,
                CheckPathExists = true,
                Filter = TextUtil.FileDialogFilterAll(Resources.ExportReportDlg_ShowShare_Skyline_Reports, ReportSpecList.EXT_REPORTS)
            })
            {
                saveFileDialog.ShowDialog(FormUtil.FindTopLevelOwner(owner));
                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    ExportViewsToFile(owner, viewSpecList, saveFileDialog.FileName);
                }
            }
        }

        public override void ExportViewsToFile(Control owner, ViewSpecList viewSpecList, string fileName)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ViewSpecList));
            SafeWriteToFile(owner, fileName, stream =>
            {
                xmlSerializer.Serialize(stream, viewSpecList);
                return true;
            });
        }

        public override void ImportViews(Control owner, ViewGroup group)
        {
            using (var importDialog = new OpenFileDialog
            {
                InitialDirectory = Settings.Default.ActiveDirectory,
                CheckPathExists = true,
                Filter = TextUtil.FileDialogFilterAll(Resources.ExportReportDlg_ShowShare_Skyline_Reports,
                    ReportSpecList.EXT_REPORTS)
            })
            {
                importDialog.ShowDialog(FormUtil.FindTopLevelOwner(owner));

                if (string.IsNullOrEmpty(importDialog.FileName))
                {
                    return;
                }
                ImportViewsFromFile(owner, @group, importDialog.FileName);
            }
        }

        public override void ImportViewsFromFile(Control owner, ViewGroup group, string fileName)
        {
            ViewSpecList views;
            try
            {
                views = LoadViews(fileName);
            }
            catch (Exception x)
            {
                new MessageBoxHelper(owner.FindForm()).ShowXmlParsingError(
                    string.Format(Resources.SkylineViewContext_ImportViews_Failure_loading__0__, fileName),
                    fileName, x.InnerException ?? x);
                return;
            }
            if (!views.ViewSpecs.Any())
            {
                ShowMessageBox(owner, Resources.SkylineViewContext_ImportViews_No_views_were_found_in_that_file_,
                    MessageBoxButtons.OK);
                return;
            }
            CopyViewsToGroup(owner, @group, views);
        }

        protected ViewSpecList LoadViews(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                var reportOrViewSpecs = ReportSharing.DeserializeReportList(stream);
                return new ViewSpecList(ReportSharing.ConvertAll(reportOrViewSpecs, ((SkylineDataSchema)DataSchema).Document));
            }
        }


        public override ViewSpecList GetViewSpecList(ViewGroupId viewGroup)
        {
            return base.GetViewSpecList(viewGroup)
                   ?? SortViewSpecList(Settings.Default.PersistedViews.GetViewSpecList(viewGroup)) 
                   ?? ViewSpecList.EMPTY;
        }

        private ViewSpecList SortViewSpecList(ViewSpecList viewSpecList)
        {
            var viewSpecs = viewSpecList.ViewSpecs.ToArray();
            var stringComparer = StringComparer.Create(DataSchema.DataSchemaLocalizer.FormatProvider, true);
            Array.Sort(viewSpecs, (v1,v2)=>stringComparer.Compare(v1.Name, v2.Name));
            return new ViewSpecList(viewSpecs, viewSpecList.ViewLayouts);
        }
    }
}
