using System.ComponentModel;
using System.Net.Configuration;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Databinding.Settings
{
    public class ScalarSettings
    {
        [Browsable(false)]
        public SettingsObject Settings { get; private set; }

        public int MaxMissedCleavages
        {
            get { return Settings.DigestSettings.MaxMissedCleavages; }
            set
            {
                Settings.DigestSettings = new DigestSettings(value, ExcludeRaggedEnds);
            }
        }

        public bool ExcludeRaggedEnds
        {
            get { return Settings.DigestSettings.ExcludeRaggedEnds; }
            set
            {
                Settings.DigestSettings = new DigestSettings(MaxMissedCleavages, value);
            }
        }

        public string BackgroundProteomeName
        {
            get { return Settings.PeptideSettings.BackgroundProteome?.Name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Settings.PeptideSettings = Settings.PeptideSettings.ChangeBackgroundProteome(null);
                    return;
                }

                var backgroundProteome = Settings.ApplicationSettings.BackgroundProteomeList[value];
                if (backgroundProteome == null)
                {
                    throw new 
                }
            }
        }
    }
}
