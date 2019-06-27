using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.DataBinding;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.DocumentContainers
{
    public class SkylineWindowDocumentSettingsContainer : WritableDocumentSettingsContainer
    {
        public SkylineWindowDocumentSettingsContainer(QueryLock queryLock, SkylineWindow skylineWindow, DataSchemaLocalizer dataSchemaLocalizer) : base(queryLock, dataSchemaLocalizer)
        {
            SkylineWindow = skylineWindow;
        }

        public SkylineWindow SkylineWindow { get; private set; }

        protected override void ModifyDocumentNow(EditDescription editDescription, Func<DocumentSettings, DocumentSettings> modifyFunc, Func<SrmDocumentPair, AuditLogEntry> auditLogFunc)
        {
            SettingsSnapshot settingsSnapshot = DocumentSettings.Settings;
            DocumentSettings newDocumentSettings;
            SkylineWindow.ModifyDocument(editDescription.GetUndoText(DataSchemaLocalizer), 
                doc=>
                {
                    newDocumentSettings = modifyFunc(new DocumentSettings(doc, settingsSnapshot));
                    return newDocumentSettings.Document;
                },
                auditLogFunc);
        }

        protected override void CommitBatchModifyDocumentNow(string description, DataGridViewPasteHandler.BatchModifyInfo batchModifyInfo)
        {
            string message = Resources.DataGridViewPasteHandler_EndDeferSettingsChangesOnDocument_Updating_settings;
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
                        newDocument = DocumentSettings.Document.EndDeferSettingsChanges(_batchChangesOriginalDocument.Document,
                            srmSettingsChangeMonitor);
                    });
                    return newDocument;
                }
            }, GetAuditLogFunction(batchModifyInfo));
        }
    }
}
