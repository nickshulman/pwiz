using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.DocumentContainers
{
    public abstract class WritableDocumentSettingsContainer : DocumentSettingsContainer
    {
        protected DocumentSettings _batchChangesOriginalDocument;
        protected List<EditDescription> _batchEditDescriptions;

        public WritableDocumentSettingsContainer(QueryLock queryLock, DataSchemaLocalizer dataSchemaLocalizer) : base(queryLock, dataSchemaLocalizer)
        {
            DataSchemaLocalizer = dataSchemaLocalizer;
        }

        public DataSchemaLocalizer DataSchemaLocalizer { get; private set; }

        public void ModifyDocument(EditDescription editDescription, Func<DocumentSettings, DocumentSettings> modifyFunc,
            Func<SrmDocumentPair, AuditLogEntry> auditLogFunc)
        {
            if (_batchChangesOriginalDocument == null)
            {
                ModifyDocumentNow(editDescription, modifyFunc, auditLogFunc);
                return;
            }
            VerifyDocumentCurrent(_batchChangesOriginalDocument.Document, DocumentSettings.Document);
            _batchEditDescriptions.Add(editDescription);
            DocumentSettings = modifyFunc(DocumentSettings.BeginDeferSettingsChanges());
        }

        protected abstract void ModifyDocumentNow(EditDescription editDescription,
            Func<DocumentSettings, DocumentSettings> modifyFunc,
            Func<SrmDocumentPair, AuditLogEntry> auditLogFunc);

        protected void VerifyDocumentCurrent(SrmDocument expectedCurrentDocument, SrmDocument actualCurrentDocument)
        {
            if (!ReferenceEquals(expectedCurrentDocument, actualCurrentDocument))
            {
                throw new InvalidOperationException(Resources.SkylineDataSchema_VerifyDocumentCurrent_The_document_was_modified_in_the_middle_of_the_operation_);
            }
        }

        public void BeginBatchModifyDocument()
        {
            if (null != _batchChangesOriginalDocument)
            {
                throw new InvalidOperationException();
            }
            if (!ReferenceEquals(_document, _documentContainer.Document))
            {
                DocumentChangedEventHandler(_documentContainer, new DocumentChangedEventArgs(_document));
            }
            _batchChangesOriginalDocument = Tuple.Create(_document, _settingsSnapshot);
            _batchEditDescriptions = new List<EditDescription>();
        }

        protected abstract DocumentSettings GetCurrentDocument();

        public void CommitBatchModifyDocument(string description,
            DataGridViewPasteHandler.BatchModifyInfo batchModifyInfo)
        {
            if (null == _batchChangesOriginalDocument)
            {
                throw new InvalidOperationException();
            }
            CommitBatchModifyDocumentNow(description, batchModifyInfo);
        }

        protected abstract void CommitBatchModifyDocumentNow(string description,
            DataGridViewPasteHandler.BatchModifyInfo batchModifyInfo);

        protected Func<SrmDocumentPair, AuditLogEntry> GetAuditLogFunction(
            DataGridViewPasteHandler.BatchModifyInfo batchModifyInfo)
        {
            if (batchModifyInfo == null)
            {
                return null;
            }
            return docPair =>
            {
                MessageType singular, plural;
                var detailType = MessageType.set_to_in_document_grid;
                Func<EditDescription, object[]> getArgsFunc = descr => new object[]
                {
                    descr.AuditLogParseString, descr.ElementRefName,
                    CellValueToString(descr.Value)
                };

                switch (batchModifyInfo.BatchModifyAction)
                {
                    case DataGridViewPasteHandler.BatchModifyAction.Paste:
                        singular = MessageType.pasted_document_grid_single;
                        plural = MessageType.pasted_document_grid;
                        break;
                    case DataGridViewPasteHandler.BatchModifyAction.Clear:
                        singular = MessageType.cleared_document_grid_single;
                        plural = MessageType.cleared_document_grid;
                        detailType = MessageType.cleared_cell_in_document_grid;
                        getArgsFunc = descr => new[]
                            {(object) descr.ColumnCaption.GetCaption(DataSchemaLocalizer), descr.ElementRefName};
                        break;
                    case DataGridViewPasteHandler.BatchModifyAction.FillDown:
                        singular = MessageType.fill_down_document_grid_single;
                        plural = MessageType.fill_down_document_grid;
                        break;
                    default:
                        return null;
                }

                var entry = AuditLogEntry.CreateCountChangeEntry(singular, plural, docPair.NewDocumentType,
                    _batchEditDescriptions,
                    descr => MessageArgs.Create(descr.ColumnCaption.GetCaption(DataSchemaLocalizer)),
                    null).ChangeExtraInfo(batchModifyInfo.ExtraInfo + Environment.NewLine);

                entry = entry.Merge(batchModifyInfo.EntryCreator.Create(docPair));

                return entry.AppendAllInfo(_batchEditDescriptions.Select(descr => new MessageInfo(detailType, docPair.NewDocumentType,
                    getArgsFunc(descr))).ToList());
            };
        }

        protected static string CellValueToString(object value)
        {
            if (value == null)
                return string.Empty;

            // TODO: only allow reflection for all info?
            bool unused;
            return DiffNode.ObjectToString(true, value, out unused);
        }
    }
}
