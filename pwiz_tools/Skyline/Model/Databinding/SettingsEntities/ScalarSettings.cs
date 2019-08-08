using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocumentContainers;
using pwiz.Skyline.Model.Proteome;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class ScalarSettings
    {
        public ScalarSettings(DocumentSettings documentSettings)
        {
            Settings = new SettingsObject(documentSettings);
        }
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

                var backgroundProteomeSpec = Settings.DocumentSettings.Settings.BackgroundProteomes.FirstOrDefault(prot => prot.Name == value);
                if (backgroundProteomeSpec == null)
                {
                    throw new ArgumentException();
                }

                Settings.PeptideSettings = Settings.PeptideSettings
                    .ChangeBackgroundProteome(new BackgroundProteome(backgroundProteomeSpec));
            }
        }

        public static IEnumerable<PropertyDescriptor> EnumerateSettings()
        {
            return TypeDescriptor.GetProperties(typeof(ScalarSettings)).Cast<PropertyDescriptor>().Where(pd=>pd.IsBrowsable);
        }
    }
}
