using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocumentContainers;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class SettingsObject
    {
        public SettingsObject(DocumentSettings documentSettings)
        {
            DocumentSettings = documentSettings;
        }

        public DocumentSettings DocumentSettings { get; private set; }

        public SrmSettings SrmSettings
        {
            get { return DocumentSettings.Document.Settings; }
            set { DocumentSettings = DocumentSettings.ChangeDocument(DocumentSettings.Document.ChangeSettings(value)); }
        }

        public PeptideSettings PeptideSettings
        {
            get { return SrmSettings.PeptideSettings; }
            set { SrmSettings = SrmSettings.ChangePeptideSettings(value); }
        }

        public DigestSettings DigestSettings
        {
            get { return PeptideSettings.DigestSettings; }
            set
            {
                PeptideSettings = PeptideSettings.ChangeDigestSettings(value);
            }
        }
    }
}
