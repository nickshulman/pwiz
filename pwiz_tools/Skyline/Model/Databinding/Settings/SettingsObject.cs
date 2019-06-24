using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Databinding.Settings
{
    public class SettingsObject
    {
        public Properties.Settings ApplicationSettings { get; private set; }
        public SrmSettings SrmSettings { get; set; }

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
