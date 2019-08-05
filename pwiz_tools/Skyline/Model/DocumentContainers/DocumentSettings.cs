namespace pwiz.Skyline.Model.DocumentContainers
{
    public class DocumentSettings
    {
        public DocumentSettings(SrmDocument document, SettingsSnapshot settings)
        {
            Document = document;
            Settings = settings;
            ReferenceId = new object();
        }

        public SrmDocument Document { get; private set; }
        public SettingsSnapshot Settings { get; private set; }
        public object ReferenceId { get; private set; }

        public DocumentSettings BeginDeferSettingsChanges()
        {
            if (Document.DeferSettingsChanges)
            {
                return this;
            }
            return new DocumentSettings(Document.BeginDeferSettingsChanges(), Settings);
        }

        public DocumentSettings ChangeDocument(SrmDocument document)
        {
            return new DocumentSettings(document, Settings);
        }

        public DocumentSettings ChangeSettings(SettingsSnapshot settingsSnapshot)
        {
            return new DocumentSettings(Document, settingsSnapshot);
        }
    }
}
