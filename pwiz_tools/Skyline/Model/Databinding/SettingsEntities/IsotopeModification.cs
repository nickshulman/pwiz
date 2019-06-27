using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class IsotopeModification : AbstractModification
    {
        private readonly CachedValue<ModificationInfo> _modInfo;
        public IsotopeModification(SkylineDataSchema dataSchema, string name) : base(dataSchema, name)
        {
            _modInfo = CachedValue.Create(dataSchema, GetModificationInfo);
        }

        protected override StaticMod StaticMod
        {
            get { return _modInfo.Value.StaticMod; }
        }

        public string LabelTypes
        {
            get
            {
                return DataSchema.DataSchemaLocalizer.CallWithCultureInfo(new FormattableList<string>(_modInfo.Value.LabelTypes.Select(label => label.Name).ToArray()).ToString);
            }

            set
            {
                HashSet<string> labelTypes;
                if (string.IsNullOrEmpty(value))
                {
                    labelTypes = new HashSet<string>();
                }
                else
                {
                    var formattableList = DataSchema.DataSchemaLocalizer
                        .CallWithCultureInfo(() => FormattableList<string>.Parse(value));
                    labelTypes = new HashSet<string>(formattableList);
                }

                var validLabelTypes = new HashSet<string>(SrmDocument.Settings.PeptideSettings.Modifications
                    .GetHeavyModificationTypes().Select(label => label.Name));
                foreach (var name in labelTypes)
                {
                    if (!validLabelTypes.Contains(name))
                    {
                        throw new ArgumentException("Invalid label type " + name);
                    }
                }

                DataSchema.ModifyDocument(EditDescription.SetColumn(nameof(LabelTypes), value), doc =>
                {

                    var peptideModifications = SrmDocument.Settings.PeptideSettings.Modifications;
                    foreach (var labelType in peptideModifications.GetHeavyModificationTypes())
                    {
                        bool newContains = labelTypes.Contains(labelType.Name);
                        var mods = peptideModifications.GetModifications(labelType);
                        if (mods.Contains(mod => mod.Name == Name) == newContains)
                        {
                            continue;
                        }

                        if (newContains)
                        {
                            peptideModifications = peptideModifications.ChangeModifications(labelType,
                                ImmutableList.ValueOf(mods.Append(_modInfo.Value.StaticMod)));
                        }
                        else
                        {
                            peptideModifications = peptideModifications.ChangeModifications(labelType,
                                ImmutableList.ValueOf(mods.Where(m => m.Name != Name)));
                        }
                    }

                    return doc.ChangeSettings(
                        doc.Settings.ChangePeptideSettings(
                            doc.Settings.PeptideSettings.ChangeModifications(peptideModifications)));
                });
            }
        }


        private ModificationInfo GetModificationInfo()
        {
            ModificationInfo modInfo = new ModificationInfo();
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

            if (modInfo.StaticMod == null)
            {
                foreach (var mod in Properties.Settings.Default.HeavyModList)
                {
                    if (mod.Name == Name)
                    {
                        modInfo.StaticMod = mod;
                    }
                }
            }

            return modInfo;
        }

        public override ElementRef GetElementRef()
        {
            return SettingsListItemRef.PROTOTYPE.ChangeName(Name)
                .ChangeParent(SettingsListRef.IsotopeModification);
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
