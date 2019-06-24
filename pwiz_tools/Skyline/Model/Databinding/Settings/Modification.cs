using System.Collections.Generic;
using System.Linq;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Databinding.Settings
{
    public class Modification : SkylineObject
    {
        private CachedValue<ModificationInfo> _modInfo;
        public Modification(SkylineDataSchema dataSchema, string name) : base(dataSchema)
        {
            Name = name;
            _modInfo = CachedValue.Create(DataSchema, GetModificationInfo);
        }

        public string Name { get; private set; }

        public string LabelTypes
        {
            get
            {
                return new FormattableList<string>();
            }
        }

        private ModificationInfo GetModificationInfo()
        {
            ModificationInfo modInfo = new ModificationInfo();
            foreach (var mod in SrmDocument.Settings.PeptideSettings.Modifications.StaticModifications)
            {
                if (mod.Name == Name)
                {
                    modInfo.StaticMod = mod;
                    modInfo.LabelTypes.Add(IsotopeLabelType.light);
                    break;
                }
            }

            foreach (var heavyMods in SrmDocument.Settings.PeptideSettings.Modifications.HeavyModifications)
            {
                foreach (var mod in heavyMods.Modifications)
                {
                    if (mod.Name == Name)
                    {
                        modInfo.StaticMod = modInfo.StaticMod ?? mod;
                        modInfo.LabelTypes.Add(heavyMods.LabelType);
                    }
                }
            }

            
        }

        private StaticMod FindStaticMod()
        {
            return ListAllModifications().FirstOrDefault(mod => mod.Name == Name);
        }

        private IEnumerable<StaticMod> ListAllModifications()
        {
            var modifications = SrmDocument.Settings.PeptideSettings.Modifications;
            var settings = Properties.Settings.Default;
            return modifications.StaticModifications
                .Concat(modifications.AllHeavyModifications)
                .Concat(settings.StaticModList)
                .Concat(settings.HeavyModList);
        }

        private class ModificationInfo
        {
            public ModificationInfo()
            {
                LabelTypes = new List<IsotopeLabelType>();
            }
            public StaticMod StaticMod { get; set; }
            public List<IsotopeLabelType> LabelTypes { get; private set; }

        }
    }
}
