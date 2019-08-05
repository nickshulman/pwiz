/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.AuditLog.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.AbsoluteQuantification;
using pwiz.Skyline.Model.DocumentContainers;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Model.GroupComparison;
using pwiz.Skyline.Model.Lists;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using SkylineTool;

namespace pwiz.Skyline.Model.Databinding
{
    public class SkylineDataSchema : DataSchema
    {
        private readonly DocumentSettingsContainer _documentSettingsContainer;
        private readonly CachedValue<ImmutableSortedList<ResultKey, Replicate>> _replicates;
        private readonly CachedValue<IDictionary<ResultFileKey, ResultFile>> _resultFiles;
        private readonly CachedValue<ElementRefs> _elementRefCache;


        public SkylineDataSchema(IDocumentUIContainer documentContainer, DataSchemaLocalizer dataSchemaLocalizer) 
            : this(DocumentSettingsContainer.FromDocumentUi(documentContainer, dataSchemaLocalizer))
        {
        }

        public SkylineDataSchema(DocumentSettingsContainer documentSettingsContainer) : base(documentSettingsContainer.QueryLock, documentSettingsContainer.DataSchemaLocalizer)
        {
            _documentSettingsContainer = documentSettingsContainer;
            ChromDataCache = new ChromDataCache();
            

            _replicates = CachedValue.Create(this, CreateReplicateList);
            _resultFiles = CachedValue.Create(this, CreateResultFileList);
            _elementRefCache = CachedValue.Create(this, () => new ElementRefs(Document));
        }

        public override string DefaultUiMode
        {
            get
            {
                return UiModes.FromDocumentType(ModeUI);
            }
        }

        public SrmDocument.DOCUMENT_TYPE ModeUI
        {
            get
            {
                if (SkylineWindow != null)
                {
                    return SkylineWindow.ModeUI;
                }

                if (DocumentSettings.Document.DocumentType == Program.ModeUI)
                {
                    return DocumentSettings.Document.DocumentType;
                }

                return SrmDocument.DOCUMENT_TYPE.mixed;
            }
        }

        protected override bool IsScalar(Type type)
        {
            return base.IsScalar(type) || type == typeof(IsotopeLabelType) || type == typeof(DocumentLocation) ||
                   type == typeof(SampleType) || type == typeof(GroupIdentifier) || type == typeof(StandardType) ||
                   type == typeof(NormalizationMethod) || type == typeof(RegressionFit) ||
                   type == typeof(AuditLogRow.AuditLogRowText) || type == typeof(AuditLogRow.AuditLogRowId);
        }

        public override bool IsRootTypeSelectable(Type type)
        {
            if (typeof(ListItem).IsAssignableFrom(type))
            {
                return false;
            }
            return base.IsRootTypeSelectable(type) && type != typeof(SkylineDocument);
        }

        public override IEnumerable<PropertyDescriptor> GetPropertyDescriptors(Type type)
        {
            return base.GetPropertyDescriptors(type)
                .Concat(GetAnnotations(type))
                .Concat(GetRatioProperties(type))
                .Concat(GetListProperties(type));
        }

        public IEnumerable<PropertyDescriptor> GetAnnotations(Type type)
        {
            if (null == type)
            {
                return new AnnotationPropertyDescriptor[0];
            }
            var annotationTargets = GetAnnotationTargets(type);
            if (annotationTargets.IsEmpty)
            {
                return new AnnotationPropertyDescriptor[0];
            }
            var properties = new List<PropertyDescriptor>();
            foreach (var annotationDef in Document.Settings.DataSettings.AnnotationDefs)
            {
                var intersectTargets = annotationDef.AnnotationTargets.Intersect(annotationTargets);
                if (!intersectTargets.IsEmpty)
                {
                    properties.Add(MakeLookupPropertyDescriptor(annotationDef, new AnnotationPropertyDescriptor(this, annotationDef, true)));
                }
            }
            return properties;
        }

        public IEnumerable<PropertyDescriptor> GetListProperties(Type type)
        {
            if (!typeof(ListItem).IsAssignableFrom(type))
            {
                return new AnnotationPropertyDescriptor[0];
            }
            var listName = ListItemTypes.INSTANCE.GetListName(type);
            if (string.IsNullOrEmpty(listName))
            {
                return new AnnotationPropertyDescriptor[0];
            }
            var listData = Document.Settings.DataSettings.FindList(listName);
            if (listData == null)
            {
                return new AnnotationPropertyDescriptor[0];
            }

            return listData.ListDef.Properties.Select(annotationDef =>
                MakeLookupPropertyDescriptor(annotationDef,
                    new ListColumnPropertyDescriptor(this, listData.ListDef.Name, annotationDef)));

        }

        private AnnotationDef.AnnotationTargetSet GetAnnotationTargets(Type type)
        {
            return AnnotationDef.AnnotationTargetSet.OfValues(
                type.GetCustomAttributes(true)
                    .OfType<AnnotationTargetAttribute>()
                    .Select(attr => attr.AnnotationTarget));
        }

        public IEnumerable<RatioPropertyDescriptor> GetRatioProperties(Type type)
        {
            return RatioPropertyDescriptor.ListProperties(Document, type);
        }

        public SrmDocument Document
        {
            get
            {
                return DocumentSettings.Document;
            }
        }

        public DocumentSettings DocumentSettings
        {
            get { return _documentSettingsContainer.DocumentSettings; }
        }

        public void Listen(IDocumentSettingsListener listener)
        {
            _documentSettingsContainer.Listen(listener);
        }

        public void Unlisten(IDocumentSettingsListener listener)
        {
            _documentSettingsContainer.Unlisten(listener);
        }

        public SkylineWindow SkylineWindow { get { return _documentSettingsContainer.SkylineWindow; } }

        private ReplicateSummaries _replicateSummaries;
        public ReplicateSummaries GetReplicateSummaries()
        {
            ReplicateSummaries replicateSummaries;
            if (null == _replicateSummaries)
            {
                replicateSummaries = new ReplicateSummaries(Document);
            }
            else
            {
                replicateSummaries = _replicateSummaries.GetReplicateSummaries(Document);
            }
            return _replicateSummaries = replicateSummaries;
        }

        public ChromDataCache ChromDataCache { get; private set; }
        public ElementRefs ElementRefs { get { return _elementRefCache.Value; } }

        public override PropertyDescriptor GetPropertyDescriptor(Type type, string name)
        {
            var propertyDescriptor = base.GetPropertyDescriptor(type, name);
            if (null != propertyDescriptor)
            {
                return propertyDescriptor;
            }
            if (null == type)
            {
                return null;
            }
            propertyDescriptor = RatioPropertyDescriptor.GetProperty(Document, type, name);
            if (null != propertyDescriptor)
            {
                return propertyDescriptor;
            }
            if (name.StartsWith(AnnotationDef.ANNOTATION_PREFIX))
            {
                var annotationTargets = GetAnnotationTargets(type);
                if (!annotationTargets.IsEmpty)
                {
                    var annotationDef = new AnnotationDef(name.Substring(AnnotationDef.ANNOTATION_PREFIX.Length),
                        annotationTargets, AnnotationDef.AnnotationType.text, new string[0]);
                    return new AnnotationPropertyDescriptor(this, annotationDef, false);
                }
            }

            return null;
        }

        public override string GetColumnDescription(ColumnDescriptor columnDescriptor)
        {
            String description = base.GetColumnDescription(columnDescriptor);
            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
            var columnCaption = GetColumnCaption(columnDescriptor);
            return ColumnToolTips.ResourceManager.GetString(columnCaption.GetCaption(DataSchemaLocalizer.INVARIANT));
        }

        public override IColumnCaption GetInvariantDisplayName(string uiMode, Type type)
        {
            if (typeof(ListItem).IsAssignableFrom(type))
            {
                return ColumnCaption.UnlocalizableCaption(ListItemTypes.INSTANCE.GetListName(type));
            }
            return base.GetInvariantDisplayName(uiMode, type);
        }

        public override string GetTypeDescription(string uiMode, Type type)
        {
            if (typeof(ListItem).IsAssignableFrom(type))
            {
                return string.Format(Resources.SkylineDataSchema_GetTypeDescription_Item_in_list___0__, ListItemTypes.INSTANCE.GetListName(type));
            }
            return base.GetTypeDescription(uiMode, type);
        }

        public ImmutableSortedList<ResultKey, Replicate> ReplicateList { get { return _replicates.Value; } }
        public IDictionary<ResultFileKey, ResultFile> ResultFileList { get { return _resultFiles.Value; } }

        public static DataSchemaLocalizer GetLocalizedSchemaLocalizer()
        {
            return new DataSchemaLocalizer(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, ColumnCaptions.ResourceManager);
        }

        public void BeginBatchModifyDocument()
        {
            _documentSettingsContainer.BeginBatchModifyDocument();
        }

        public void CommitBatchModifyDocument(string description, DataGridViewPasteHandler.BatchModifyInfo batchModifyInfo)
        {
            _documentSettingsContainer.CommitBatchModifyDocument(description, batchModifyInfo);
        }

        public void RollbackBatchModifyDocument()
        {
            _documentSettingsContainer.RollbackBatchModifyDocument();
        }

        public void ModifyDocument(EditDescription editDescription, Func<SrmDocument, SrmDocument> action, Func<SrmDocumentPair, AuditLogEntry> logFunc = null)
        {
            ModifyDocumentAndSettings(editDescription, documentSettings=>documentSettings.ChangeDocument(action(documentSettings.Document)), logFunc);
        }

        public void ModifyDocumentAndSettings(EditDescription editDescription,
            Func<DocumentSettings, DocumentSettings> action, Func<SrmDocumentPair, AuditLogEntry> logFunc = null)
        {
            _documentSettingsContainer.ModifyDocumentAndSettings(editDescription, action, logFunc);
        }

        private ImmutableSortedList<ResultKey, Replicate> CreateReplicateList()
        {
            var srmDocument = Document;
            if (!srmDocument.Settings.HasResults)
            {
                return ImmutableSortedList<ResultKey, Replicate>.EMPTY;
            }
            return ImmutableSortedList<ResultKey, Replicate>.FromValues(
                Enumerable.Range(0, srmDocument.Settings.MeasuredResults.Chromatograms.Count)
                    .Select(replicateIndex =>
                    {
                        var replicate = new Replicate(this, replicateIndex);
                        return new KeyValuePair<ResultKey, Replicate>(new ResultKey(replicate, 0), replicate);
                    }), Comparer<ResultKey>.Default);
        }
 
        private IDictionary<ResultFileKey, ResultFile> CreateResultFileList()
        {
            return ReplicateList.Values.SelectMany(
                    replicate =>
                        replicate.ChromatogramSet.MSDataFileInfos.Select(
                            chromFileInfo => new ResultFile(replicate, chromFileInfo.FileId, 0)))
                .ToDictionary(resultFile => new ResultFileKey(resultFile.Replicate.ReplicateIndex,
                    resultFile.ChromFileInfoId, resultFile.OptimizationStep));
        }

        public PropertyDescriptor MakeLookupPropertyDescriptor(AnnotationDef annotationDef, PropertyDescriptor innerPropertyDescriptor)
        {
            if (string.IsNullOrEmpty(annotationDef.Lookup))
            {
                return innerPropertyDescriptor;
            }
            var listLookupPropertyDescriptor = new ListLookupPropertyDescriptor(this, annotationDef.Lookup, innerPropertyDescriptor);
            var listData = listLookupPropertyDescriptor.ListData;
            if (listData == null || listData.PkColumn == null)
            {
                return innerPropertyDescriptor;
            }
            return listLookupPropertyDescriptor;
        }

        public static SkylineDataSchema MemoryDataSchema(SrmDocument document, DataSchemaLocalizer localizer)
        {
            var documentSettingsContainer = DocumentSettingsContainer.FromDocumentSettings(
                new DocumentSettings(document, 
                    // TODO: unsafe
                    SettingsSnapshot.FromSettings(Settings.Default)), localizer);
            return new SkylineDataSchema(documentSettingsContainer);
        }

        public override string NormalizeUiMode(string uiMode)
        {
            if (string.IsNullOrEmpty(uiMode))
            {
                return UiModes.PROTEOMIC;
            }

            return uiMode;
        }
    }
}
