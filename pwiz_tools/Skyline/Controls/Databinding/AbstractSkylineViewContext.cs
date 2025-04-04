using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Controls.Databinding
{
    public class BaseSkylineViewContext : AbstractViewContext
    {
        public BaseSkylineViewContext(DataSchema dataSchema, IEnumerable<RowSourceInfo> rowSources) : base(dataSchema, rowSources)
        {
            ApplicationIcon = Resources.Skyline;

        }

        public override IEnumerable<ViewGroup> ViewGroups
        {
            get
            {
                return new[]
                {
                    PersistedViews.MainGroup,
                    PersistedViews.ExternalToolsGroup,
                };
            }
        }

        public override ViewGroup DefaultViewGroup
        {
            get { return PersistedViews.MainGroup; }
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

        public override void AddOrReplaceViews(ViewGroupId groupId, IEnumerable<ViewSpecLayout> viewSpecs)
        {
            var viewSpecsArray = ImmutableList.ValueOf(viewSpecs);
            if (Equals(groupId, PersistedViews.MainGroup.Id))
            {
                ChangeDocumentViewSpecList(viewSpecList => viewSpecList.AddOrReplaceViews(viewSpecsArray));
            }
            base.AddOrReplaceViews(groupId, viewSpecsArray);
        }

        public override void DeleteViews(ViewGroupId groupId, IEnumerable<string> viewNames)
        {
            var viewNameSet = new HashSet<string>(viewNames);
            if (Equals(groupId, PersistedViews.MainGroup.Id))
            {
                ChangeDocumentViewSpecList(viewSpecList => viewSpecList.DeleteViews(viewNameSet));
            }
            base.DeleteViews(groupId, viewNameSet);
        }

        public override bool TryRenameView(ViewGroupId groupId, string oldName, string newName)
        {
            if (!base.TryRenameView(groupId, oldName, newName))
            {
                return false;
            }
            if (Equals(groupId, PersistedViews.MainGroup.Id))
            {
                ChangeDocumentViewSpecList(viewSpecList => viewSpecList.RenameView(oldName, newName));
            }
            return true;
        }

        protected override void SaveViewSpecList(ViewGroupId viewGroup, ViewSpecList viewSpecList)
        {
            Settings.Default.PersistedViews.SetViewSpecList(viewGroup, viewSpecList);
            if (Equals(viewGroup, PersistedViews.MainGroup.Id))
            {
                ChangeDocumentViewSpecList(docViewSpecList =>
                {
                    var newViews = new Dictionary<string, ViewSpec>();
                    foreach (var viewSpec in viewSpecList.ViewSpecs)
                    {
                        newViews[viewSpec.Name] = viewSpec;
                    }
                    var newDocViews = new List<ViewSpec>();
                    var newLayouts = new List<ViewLayoutList>();
                    foreach (var oldDocView in docViewSpecList.ViewSpecs)
                    {
                        ViewSpec newDocView;
                        if (newViews.TryGetValue(oldDocView.Name, out newDocView))
                        {
                            newDocViews.Add(newDocView);
                            ViewLayoutList viewLayoutList = viewSpecList.GetViewLayouts(oldDocView.Name);
                            if (!viewLayoutList.IsEmpty)
                            {
                                newLayouts.Add(viewLayoutList);
                            }
                        }
                    }
                    return new ViewSpecList(newDocViews, newLayouts);
                });

                var skylineWindow = GetSkylineWindow();
                if (skylineWindow != null)
                {
                    skylineWindow.ModifyDocument(DatabindingResources.SkylineViewContext_SaveViewSpecList_Change_Document_Reports, doc =>
                    {
                        var oldViewNames = new HashSet<string>(
                            doc.Settings.DataSettings.ViewSpecList.ViewSpecs.Select(spec => spec.Name));
                        var newViewSpecList = viewSpecList.Filter(spec => oldViewNames.Contains(spec.Name));
                        if (Equals(newViewSpecList, doc.Settings.DataSettings.ViewSpecList))
                        {
                            return doc;
                        }
                        return doc.ChangeSettings(doc.Settings.ChangeDataSettings(
                            doc.Settings.DataSettings.ChangeViewSpecList(newViewSpecList)));
                    }, AuditLogEntry.SettingsLogFunction);
                }
            }
        }

        protected virtual SkylineWindow GetSkylineWindow()
        {
            return null;
        }

        protected void ChangeDocumentViewSpecList(Func<ViewSpecList, ViewSpecList> changeViewSpecFunc)
        {
            var skylineWindow = GetSkylineWindow();
            if (skylineWindow != null)
            {
                skylineWindow.ModifyDocument(DatabindingResources.SkylineViewContext_ChangeDocumentViewSpec_Change_Document_Reports, doc =>
                {
                    var oldViewSpecList = doc.Settings.DataSettings.ViewSpecList;
                    var newViewSpecList = changeViewSpecFunc(oldViewSpecList);
                    if (Equals(newViewSpecList, oldViewSpecList))
                    {
                        return doc;
                    }
                    return doc.ChangeSettings(doc.Settings.ChangeDataSettings(
                        doc.Settings.DataSettings.ChangeViewSpecList(newViewSpecList)));
                }, AuditLogEntry.SettingsLogFunction);
            }
            
        }

        public override string GetExportDirectory()
        {
            return Settings.Default.ExportDirectory;
        }

        protected override string GetDefaultExportFilename(ViewInfo viewInfo)
        {
            return viewInfo.Name;
        }

        public override void SetExportDirectory(string value)
        {
            Settings.Default.ExportDirectory = value;
        }

        protected override IEnumerable<TabularFileFormat> ListAvailableExportFormats()
        {
            yield return new TabularFileFormat(TextUtil.GetCsvSeparator(DataSchema.DataSchemaLocalizer.FormatProvider),
                TextUtil.FILTER_CSV);
            yield return new TabularFileFormat('\t', TextUtil.FILTER_TSV);
        }

        public override DialogResult ShowMessageBox(Control owner, string message, MessageBoxButtons messageBoxButtons)
        {
            return new AlertDlg(message, messageBoxButtons).ShowAndDispose(FormUtil.FindTopLevelOwner(owner));
        }

        public override bool RunLongJob(Control owner, Action<CancellationToken, IProgressMonitor> job)
        {
            using (var longWaitDlg = new LongWaitDlg())
            {
                var status = longWaitDlg.PerformWork(FormUtil.FindTopLevelOwner(owner), 1000, progressMonitor => job(longWaitDlg.CancellationToken, progressMonitor));
                return status.IsComplete;
            }
        }

        public override bool RunOnThisThread(Control owner, Action<CancellationToken, IProgressMonitor> job)
        {
            var longOperationRunner = new LongOperationRunner();
            bool finished = false;
            longOperationRunner.Run(longWaitBroker =>
            {
                var progressWaitBroker = new ProgressWaitBroker(progressMonitor=>job(longWaitBroker.CancellationToken, progressMonitor));
                progressWaitBroker.PerformWork(longWaitBroker);
                finished = !longWaitBroker.IsCanceled;
            });
            return finished;
        }

        protected override void SetClipboardText(Control owner, string text)
        {
            ClipboardHelper.SetClipboardText(owner, text);
        }

        public bool Export(Control owner, ViewInfo viewInfo)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = GetExportDirectory();
                saveFileDialog.OverwritePrompt = true;
                saveFileDialog.DefaultExt = TextUtil.EXT_CSV;
                saveFileDialog.Filter = TextUtil.FileDialogFiltersAll(TextUtil.FILTER_CSV, TextUtil.FILTER_TSV);
                saveFileDialog.FileName = GetDefaultExportFilename(viewInfo);
                // TODO: If document has been saved, initial directory should be document directory
                if (saveFileDialog.ShowDialog(FormUtil.FindTopLevelOwner(owner)) == DialogResult.Cancel)
                {
                    return false;
                }
                char separator = saveFileDialog.FilterIndex == 2
                    ? TextUtil.SEPARATOR_TSV
                    : TextUtil.GetCsvSeparator(DataSchema.DataSchemaLocalizer.FormatProvider);
                SetExportDirectory(Path.GetDirectoryName(saveFileDialog.FileName));
                return ExportToFile(owner, viewInfo, saveFileDialog.FileName, separator);
            }
        }

        public bool IsInvariantLanguage()
        {
            return ReferenceEquals(DataSchema.DataSchemaLocalizer, DataSchemaLocalizer.INVARIANT);
        }

        public override DsvWriter CreateDsvWriter(char separator, ColumnFormats columnFormats)
        {
            var dsvWriter = base.CreateDsvWriter(separator, columnFormats);
            if (IsInvariantLanguage())
            {
                dsvWriter.NumberFormatOverride = Formats.RoundTrip;
            }

            return dsvWriter;
        }

        public DsvWriter GetDsvWriter(char separator)
        {
            return CreateDsvWriter(separator, null);
        }

        public bool ExportToFile(Control owner, ViewInfo viewInfo, string fileName, char separator)
        {
            try
            {
                return SafeWriteToFile(owner, fileName, stream =>
                {
                    bool success = false;
                    using (var longWait = new LongWaitDlg())
                    {
                        longWait.Text = DatabindingResources.ExportReportDlg_ExportReport_Generating_Report;
                        var action = new Action<IProgressMonitor>(progressMonitor =>
                        {
                            IProgressStatus status = new ProgressStatus(DatabindingResources.ExportReportDlg_ExportReport_Building_report);
                            progressMonitor.UpdateProgress(status);
                            using (var writer = new StreamWriter(stream))
                            {
                                success = Export(longWait.CancellationToken, progressMonitor, ref status, viewInfo, writer, separator);
                                writer.Close();
                            }
                            if (success)
                            {
                                progressMonitor.UpdateProgress(status.Complete());
                            }
                        });
                        longWait.PerformWork(owner, 1500, action);
                    }
                    return success;
                });
            }
            catch (Exception x)
            {
                MessageDlg.ShowWithException(owner,
                    string.Format(DatabindingResources.ExportReportDlg_ExportReport_Failed_exporting_to, fileName, x.Message), x);
                return false;
            }
        }

        public bool Export(CancellationToken cancellationToken, IProgressMonitor progressMonitor,
            ref IProgressStatus status, ViewInfo viewInfo, TextWriter writer, char separator)
        {
            ViewLayout viewLayout = null;
            if (viewInfo.ViewGroup != null)
            {
                var viewLayoutList = GetViewLayoutList(viewInfo.ViewGroup.Id.ViewName(viewInfo.Name));
                if (viewLayoutList != null)
                {
                    viewLayout = viewLayoutList.DefaultLayout;
                }
            }

            return Export(cancellationToken, progressMonitor, ref status, viewInfo, viewLayout, writer, separator);
        }

        public bool Export(CancellationToken cancellationToken, IProgressMonitor progressMonitor, ref IProgressStatus status, ViewInfo viewInfo, ViewLayout viewLayout, TextWriter writer, char separator)
        {
            progressMonitor ??= new SilentProgressMonitor(cancellationToken);
            RowItemEnumerator rowItemEnumerator;
            using (var bindingListSource = new BindingListSource(cancellationToken))
            {
                bindingListSource.SetViewContext(this, viewInfo);
                if (viewLayout != null)
                {
                    foreach (var column in viewLayout.ColumnFormats)
                    {
                        bindingListSource.ColumnFormats.SetFormat(column.Item1, column.Item2);
                    }
                }

                rowItemEnumerator = RowItemEnumerator.FromBindingListSource(bindingListSource);
            }

            progressMonitor.UpdateProgress(status = status.ChangePercentComplete(5)
                .ChangeMessage(DatabindingResources.ExportReportDlg_ExportReport_Writing_report));
            WriteDataWithStatus(progressMonitor, ref status, writer, rowItemEnumerator, separator);
            if (progressMonitor.IsCanceled)
                return false;
            writer.Flush();
            progressMonitor.UpdateProgress(status = status.Complete());
            return true;
        }

        protected override bool SafeWriteToFile(Control owner, string fileName, Func<Stream, bool> writeFunc)
        {
            using (var fileSaver = new FileSaver(fileName, true))
            {
                if (!fileSaver.CanSave(owner))
                {
                    return false;
                }
                if (writeFunc(fileSaver.Stream))
                {
                    fileSaver.Commit();
                    return true;
                }
            }
            return false;
        }

        public override void ExportViews(Control owner, ViewSpecList viewSpecList)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Settings.Default.ActiveDirectory;
                saveFileDialog.CheckPathExists = true;
                saveFileDialog.Filter = TextUtil.FileDialogFilterAll(DatabindingResources.ExportReportDlg_ShowShare_Skyline_Reports, ReportSpecList.EXT_REPORTS);
                saveFileDialog.ShowDialog(FormUtil.FindTopLevelOwner(owner));
                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    ExportViewsToFile(owner, viewSpecList, saveFileDialog.FileName);
                }
            }
        }

        public override void ExportViewsToFile(Control owner, ViewSpecList viewSpecList, string fileName)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ViewSpecList));
                SafeWriteToFile(owner, fileName, stream =>
                {
                    xmlSerializer.Serialize(stream, viewSpecList);
                    return true;
                });
            }
            catch (Exception x)
            {
                MessageDlg.ShowWithException(owner,
                    string.Format(DatabindingResources.ExportReportDlg_ExportReport_Failed_exporting_to, fileName, x.Message), x);
            }
        }

        public override void ImportViews(Control owner, ViewGroup group)
        {
            using (var importDialog = new OpenFileDialog())
            {
                importDialog.InitialDirectory = Settings.Default.ActiveDirectory;
                importDialog.CheckPathExists = true;
                importDialog.Filter = TextUtil.FileDialogFilterAll(DatabindingResources.ExportReportDlg_ShowShare_Skyline_Reports,
                    ReportSpecList.EXT_REPORTS);
                importDialog.ShowDialog(FormUtil.FindTopLevelOwner(owner));

                if (string.IsNullOrEmpty(importDialog.FileName))
                {
                    return;
                }
                ImportViewsFromFile(owner, group, importDialog.FileName);
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
                    string.Format(DatabindingResources.SkylineViewContext_ImportViews_Failure_loading__0__, fileName),
                    fileName, x.InnerException ?? x);
                return;
            }
            if (!views.ViewSpecs.Any())
            {
                ShowMessageBox(owner, DatabindingResources.SkylineViewContext_ImportViews_No_views_were_found_in_that_file_,
                    MessageBoxButtons.OK);
                return;
            }
            CopyViewsToGroup(owner, group, views);
        }

        protected ViewSpecList LoadViews(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                var reportOrViewSpecs = ReportSharing.DeserializeReportList(stream);
                return new ViewSpecList(ReportSharing.ConvertAll(reportOrViewSpecs, ((SkylineDataSchema) DataSchema).Document));
            }
        }
    }
}
