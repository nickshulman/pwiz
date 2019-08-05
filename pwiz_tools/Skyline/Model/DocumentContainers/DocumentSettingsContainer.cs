using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using Timer = System.Windows.Forms.Timer;

namespace pwiz.Skyline.Model.DocumentContainers
{
    public class DocumentSettingsContainer
    {
        private DocumentSettings _documentSettings;
        private readonly HashSet<IDocumentSettingsListener> _documentSettingsChangeListeners = new HashSet<IDocumentSettingsListener>();
        private IDisposable _queryLockDisposable;
        private DocumentSettings _batchChangesOriginalDocument;
        private List<EditDescription> _batchEditDescriptions;
        private Timer _timer;

        public static DocumentSettingsContainer FromDocumentUi(IDocumentUIContainer documentUiContainer,
            DataSchemaLocalizer dataSchemaLocalizer)
        {
            return new DocumentSettingsContainer(documentUiContainer, new DocumentSettings(documentUiContainer.DocumentUI, SettingsSnapshot.FromSettings(Settings.Default)),
                new QueryLock(CancellationToken.None), dataSchemaLocalizer);
        }

        public static DocumentSettingsContainer FromDocumentSettings(DocumentSettings documentSettings, DataSchemaLocalizer dataSchemaLocalizer)
        {
            var memoryDocumentContainer = new MemoryDocumentContainer();
            memoryDocumentContainer.SetDocument(documentSettings.Document, memoryDocumentContainer.Document);
            return new DocumentSettingsContainer(memoryDocumentContainer, documentSettings, new QueryLock(CancellationToken.None), dataSchemaLocalizer);
        }

        public DocumentSettingsContainer(IDocumentContainer documentContainer, DocumentSettings documentSettings,
            QueryLock queryLock, DataSchemaLocalizer dataSchemaLocalizer)
        {
            DocumentContainer = documentContainer;
            _documentSettings = documentSettings;
            QueryLock = queryLock;
            DataSchemaLocalizer = dataSchemaLocalizer;
        }

        public DocumentSettings DocumentSettings
        {
            get { return _documentSettings; }
        }

        public void Listen(IDocumentSettingsListener listener)
        {
            bool firstListener;
            lock (_documentSettingsChangeListeners)
            {
                firstListener = _documentSettingsChangeListeners.Count == 0;

                if (!_documentSettingsChangeListeners.Add(listener))
                {
                    throw new ArgumentException(@"Listener already added");
                }
            }

            if (firstListener)
            {
                FirstListenerAdded();
            }
        }

        public void Unlisten(IDocumentSettingsListener listener)
        {
            bool lastListener;
            lock (_documentSettingsChangeListeners)
            {
                if (!_documentSettingsChangeListeners.Remove(listener))
                {
                    throw new ArgumentException(@"Listener had not been added");
                }

                lastListener = _documentSettingsChangeListeners.Count == 0;
            }

            if (lastListener)
            {
                LastListenerRemoved();
            }
        }

        protected void FirstListenerAdded()
        {
            var documentUiContainer = DocumentContainer as IDocumentUIContainer;
            if (null == documentUiContainer)
            {
                DocumentContainer.Listen(DocumentChangedEventHandler);
            }
            else
            {
                documentUiContainer.ListenUI(DocumentChangedEventHandler);
            }

            if (SkylineWindow != null)
            {
                SkylineWindow.Invoke(new Action(() => {
                    _timer = new Timer()
                    {
                        Enabled = true,
                        Interval = 10000,
                    };
                    _timer.Tick += TimerOnTick;
                }));
            }
        }

        protected void LastListenerRemoved()
        {
            var documentUiContainer = DocumentContainer as IDocumentUIContainer;
            if (null == documentUiContainer)
            {
                DocumentContainer.Unlisten(DocumentChangedEventHandler);
            }
            else
            {
                documentUiContainer.UnlistenUI(DocumentChangedEventHandler);
            }

            if (SkylineWindow != null)
            {
                using (var timer = Interlocked.Exchange(ref _timer, null))
                { }
            }
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            var settingsSnapshot = SettingsSnapshot.FromSettings(Settings.Default);
            if (Equals(settingsSnapshot, _documentSettings.Settings))
            {
                return;
            }
            SetDocumentSettings(_documentSettings.ChangeSettings(settingsSnapshot));
        }

        public QueryLock QueryLock { get; private set; }

        public IDocumentContainer DocumentContainer { get; private set; }

        public SkylineWindow SkylineWindow { get { return DocumentContainer as SkylineWindow; } }

        public DataSchemaLocalizer DataSchemaLocalizer { get; private set; }

        public void BeginBatchModifyDocument()
        {

        }

        public void CommitBatchModifyDocument(string description, DataGridViewPasteHandler.BatchModifyInfo batchModifyInfo)
        {
            if (null == _batchChangesOriginalDocument)
            {
                throw new InvalidOperationException();
            }
            string message = Resources.DataGridViewPasteHandler_EndDeferSettingsChangesOnDocument_Updating_settings;
            if (SkylineWindow != null)
            {
                SkylineWindow.ModifyDocument(description, document =>
                {
                    VerifyDocumentCurrent(_batchChangesOriginalDocument.Document, document);
                    using (var longWaitDlg = new LongWaitDlg
                    {
                        Message = message
                    })
                    {
                        SrmDocument newDocument = document;
                        longWaitDlg.PerformWork(SkylineWindow, 1000, progressMonitor =>
                        {
                            var srmSettingsChangeMonitor = new SrmSettingsChangeMonitor(progressMonitor,
                                message);
                            newDocument = _documentSettings.Document.EndDeferSettingsChanges(_batchChangesOriginalDocument.Document,
                                srmSettingsChangeMonitor);
                        });
                        return newDocument;
                    }
                }, GetAuditLogFunction(batchModifyInfo));
            }
            else
            {
                VerifyDocumentCurrent(_batchChangesOriginalDocument.Document, DocumentContainer.Document);
                if (!DocumentContainer.SetDocument(
                    _documentSettings.Document.EndDeferSettingsChanges(_batchChangesOriginalDocument.Document, null),
                    _batchChangesOriginalDocument.Document))
                {
                    throw new InvalidOperationException(Resources
                        .SkylineDataSchema_VerifyDocumentCurrent_The_document_was_modified_in_the_middle_of_the_operation_);
                }
            }
            _batchChangesOriginalDocument = null;
            _batchEditDescriptions = null;
        }



        public void RollbackBatchModifyDocument()
        {
            using (_queryLockDisposable)
            {
                _batchChangesOriginalDocument = null;
                _batchEditDescriptions = null;
                SetDocumentSettings(_documentSettings.ChangeDocument(DocumentContainer.Document));
            }

        }

        public void ModifyDocumentAndSettings(EditDescription editDescription,
            Func<DocumentSettings, DocumentSettings> action, Func<SrmDocumentPair, AuditLogEntry> logFunc = null)
        {
            if (_batchChangesOriginalDocument != null)
            {
                VerifyDocumentCurrent(_batchChangesOriginalDocument.Document, DocumentContainer.Document);
                _batchEditDescriptions.Add(editDescription);
                _documentSettings = action(_documentSettings);
                return;
            }


            DocumentSettings newDocumentSettings = null;
            if (SkylineWindow != null)
            {
                SkylineWindow.ModifyDocument(editDescription.GetUndoText(DataSchemaLocalizer), doc =>
                    {
                        newDocumentSettings = action(_documentSettings.ChangeDocument(doc));
                        return newDocumentSettings.Document;
                    },
                    logFunc ?? (docPair => AuditLogEntry.CreateSimpleEntry(MessageType.set_to_in_document_grid, docPair.NewDocumentType,
                        editDescription.AuditLogParseString, editDescription.ElementRefName, CellValueToString(editDescription.Value))));
            }
            else
            {
                var doc = DocumentContainer.Document;
                newDocumentSettings = action(_documentSettings.ChangeDocument(doc));
                if (!DocumentContainer.SetDocument(newDocumentSettings.Document, doc))
                {
                    throw new InvalidOperationException(Resources
                        .SkylineDataSchema_VerifyDocumentCurrent_The_document_was_modified_in_the_middle_of_the_operation_);
                }
            }
            UpdateApplicationSettings(newDocumentSettings.Settings);
        }


        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        private void VerifyDocumentCurrent(SrmDocument expectedCurrentDocument, SrmDocument actualCurrentDocument)
        {
            if (!ReferenceEquals(expectedCurrentDocument, actualCurrentDocument))
            {
                throw new InvalidOperationException(Resources.SkylineDataSchema_VerifyDocumentCurrent_The_document_was_modified_in_the_middle_of_the_operation_);
            }
        }
        // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local

        private static string CellValueToString(object value)
        {
            if (value == null)
                return string.Empty;

            return DiffNode.ObjectToString(true, value, out _);
        }

        private void UpdateApplicationSettings(SettingsSnapshot newSettingsSnapshot)
        {
            newSettingsSnapshot.UpdateSettings(_documentSettings.Settings, Settings.Default);
        }

        private void DocumentChangedEventHandler(object sender, DocumentChangedEventArgs args)
        {
            var documentSettingsNew = _documentSettings.ChangeDocument(DocumentContainer.Document);
            if (SkylineWindow != null)
            {
                var settingsSnapshotNew = SettingsSnapshot.FromSettings(Settings.Default);
                if (!settingsSnapshotNew.Equals(documentSettingsNew.Settings))
                {
                    documentSettingsNew = documentSettingsNew.ChangeSettings(settingsSnapshotNew);
                }
            }
            SetDocumentSettings(documentSettingsNew);
        }

        private void SetDocumentSettings(DocumentSettings documentSettingsNew)
        {
            using (QueryLock.CancelAndGetWriteLock())
            {
                _documentSettings = documentSettingsNew;
            }
            IList<IDocumentSettingsListener> listeners;
            lock (_documentSettingsChangeListeners)
            {
                listeners = _documentSettingsChangeListeners.ToArray();
            }
            foreach (var listener in listeners)
            {
                listener.DocumentSettingsChanged();
            }

        }
        private Func<SrmDocumentPair, AuditLogEntry> GetAuditLogFunction(
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
    }
}
