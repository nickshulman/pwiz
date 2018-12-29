using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Ionic.Zip;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls.GroupComparison;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.GroupComparison;
using pwiz.Skyline.Model.Sharing.GeneratedCode;
using pwiz.Skyline.Util.Extensions;
using BindingListSource = pwiz.Common.DataBinding.Controls.BindingListSource;

namespace pwiz.Skyline.Model.Sharing
{
    public class DocumentReports : MustDispose
    {
        public DocumentReports(IProgressMonitor progressMonitor, SkylineDataSchema dataSchema)
        {
            ProgressMonitor = progressMonitor;
            DataSchema = dataSchema;
        }

        public IProgressMonitor ProgressMonitor { get; private set; }
        public SkylineDataSchema DataSchema { get; private set; }

        public bool AddElementLocators { get; set; }

        public IEnumerable<ReportExporter> GetReportExporters(CancellationToken cancellationToken, ViewSpecList viewSpecList)
        {
            foreach (var viewSpec in viewSpecList.ViewSpecs)
            {
                ViewLayout viewLayout = null;
                var viewLayouts = viewSpecList.GetViewLayouts(viewSpec.Name);
                if (!string.IsNullOrEmpty(viewLayouts.DefaultLayoutName))
                {
                    viewLayout = viewLayouts.FindLayout(viewLayouts.DefaultLayoutName);
                }

                var reportExporter = GetReportExporter(cancellationToken, viewSpec, viewLayout);
                if (reportExporter != null)
                {
                    yield return reportExporter;
                }
            }
        }

        public bool CanHandleView(ViewSpec viewSpec)
        {
            return null != FindRowSourceConstructor(viewSpec);
        }

        public ReportExporter GetReportExporter(CancellationToken cancellationToken, ViewSpec viewSpec, ViewLayout viewLayout)
        {
            var bindingListSource = MakeBindingListSource(cancellationToken, viewSpec, viewLayout);
            if (bindingListSource == null)
            {
                return null;
            }

            return new ReportExporter(viewSpec.Name, bindingListSource);
        }

        private BindingListSource MakeBindingListSource(CancellationToken cancellationToken, ViewSpec viewSpec, ViewLayout viewLayout)
        {
            var rowSourceConstructor = FindRowSourceConstructor(viewSpec);
            if (rowSourceConstructor == null)
            {
                return null;
            }

            foreach (var propertyPath in rowSourceConstructor.ExtraProperties)
            {
                if (null == viewSpec.Columns.FirstOrDefault(col => col.PropertyPath == propertyPath))
                {
                    viewSpec = viewSpec.SetColumns(viewSpec.Columns.Append(new ColumnSpec(propertyPath)));
                }
            }

            var parentColumn = ColumnDescriptor.RootColumn(DataSchema, rowSourceConstructor.RowType);
            var viewInfo = new ViewInfo(parentColumn, viewSpec).ChangeViewGroup(ViewGroup.BUILT_IN);
            var viewContext = new SharingViewContext(viewInfo, viewLayout, rowSourceConstructor.GetItems());
            var bindingListSource = new BindingListSource(cancellationToken);
            bindingListSource.SetViewContext(viewContext, viewInfo);
            return bindingListSource;
        }

        private RowSourceConstructor FindRowSourceConstructor(ViewSpec viewSpec)
        {
            return GetRowSourceConstructors()
                .FirstOrDefault(constructor => constructor.RowType.FullName == viewSpec.RowSource);
        }

        private IEnumerable<RowSourceConstructor> GetRowSourceConstructors()
        {
            yield return MakeRowSourceConstructor(GetProteins);
            yield return MakeRowSourceConstructor(GetPeptides);
            yield return MakeRowSourceConstructor(GetPrecursors);
            yield return MakeRowSourceConstructor(GetTransitions);
            yield return MakeRowSourceConstructor(GetPeptideResults, PropertyPath.Root.Property(@"Peptide"));
            yield return MakeRowSourceConstructor(GetPrecursorResults, PropertyPath.Root.Property(@"Precursor"));
            yield return MakeRowSourceConstructor(GetTransitionResults, PropertyPath.Root.Property(@"Transition"));
            yield return MakeRowSourceConstructor(GetAllFoldChangeRows,
                PropertyPath.Root.Property(@"GroupComparisonName"));
        }

        private class RowSourceConstructor
        {
            private Func<IEnumerable> _getItemsFunc;
            public RowSourceConstructor(Type rowType, Func<IEnumerable> getItemsFunc,
                ImmutableList<PropertyPath> extraProperties)
            {
                RowType = rowType;
                ExtraProperties = extraProperties;
                _getItemsFunc = getItemsFunc;
            }

            public Type RowType { get; private set; }
            public ImmutableList<PropertyPath> ExtraProperties { get; private set; }

            public IEnumerable GetItems()
            {
                return _getItemsFunc();
            }
        }
        private static RowSourceConstructor MakeRowSourceConstructor<T>(Func<IEnumerable<T>> getItemsFunc,
            params PropertyPath[] extraProperties)
        {
            return new RowSourceConstructor(typeof(T), getItemsFunc, ImmutableList.ValueOfOrEmpty(extraProperties));
        }

        public IEnumerable<Protein> GetProteins()
        {
            return new Proteins(DataSchema).GetItems().Cast<Protein>();
        }

        public IEnumerable<Databinding.Entities.Peptide> GetPeptides()
        {
            return new Peptides(DataSchema, new[] {IdentityPath.ROOT}).GetItems().Cast<Databinding.Entities.Peptide>();
        }

        public IEnumerable<Precursor> GetPrecursors()
        {
            return new Precursors(DataSchema, new[] {IdentityPath.ROOT}).GetItems().Cast<Precursor>();
        }

        public IEnumerable<Databinding.Entities.Transition> GetTransitions()
        {
            return new Transitions(DataSchema, new[] {IdentityPath.ROOT}).GetItems()
                .Cast<Databinding.Entities.Transition>();
        }

        public IEnumerable<PeptideResult> GetPeptideResults()
        {
            return GetPeptides().SelectMany(peptide => peptide.Results.Values);
        }

        public IEnumerable<PrecursorResult> GetPrecursorResults()
        {
            return GetPrecursors().SelectMany(precursor => precursor.Results.Values);
        }

        public IEnumerable<TransitionResult> GetTransitionResults()
        {
            return GetTransitions().SelectMany(transition => transition.Results.Values);
        }

        public IEnumerable<FoldChangeBindingSource.FoldChangeRow> GetAllFoldChangeRows()
        {
            return DataSchema.Document.Settings.DataSettings.GroupComparisonDefs.SelectMany(GetFoldChangeRows);
        }

        private IEnumerable<FoldChangeBindingSource.FoldChangeRow> GetFoldChangeRows(
            GroupComparisonDef groupComparisonDef)
        {
            var documentContainer = new MemoryDocumentContainer();
            documentContainer.SetDocument(DataSchema.Document, documentContainer.Document);
            var groupComparisonModel = new GroupComparisonModel(documentContainer, groupComparisonDef.Name, false);
            return FoldChangeBindingSource.GetFoldChangeRows(DataSchema, groupComparisonModel);
        }

        public void ExportToZipFile(string runName, CancellationToken cancellationToken, Stream outputStream)
        {
            using (var zipFile = new ZipFile())
            {
                var tableInfos = new List<TableType>();
                foreach (var reportExporter in GetReportExporters(cancellationToken,
                    DataSchema.Document.Settings.DataSettings.ViewSpecList))
                {
                    var tableInfo = reportExporter.GetTableInfo(runName);
                    tableInfos.Add(tableInfo);
                    string tsvFileName = tableInfo.tableName + ".tsv";
                    zipFile.AddEntry(tsvFileName, (name, stream) =>
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            reportExporter.WriteReport(ProgressMonitor, writer, TextUtil.SEPARATOR_TSV);
                        }
                    });
                }
                var tables = new TablesType1();
                tables.Items = tableInfos.ToArray();
                zipFile.AddEntry(@"lists.xml", (name, stream) =>
                {
                    var serializer = new XmlSerializer(tables.GetType());
                    serializer.Serialize(stream, tables);
                });
                zipFile.Save(outputStream);
            }
        }
    }
}
