using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocumentContainers;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class IsotopeModification : AbstractModification
    {
        public IsotopeModification(SkylineDataSchema dataSchema, string name) : base(dataSchema, name)
        {
        }

        public string LabelTypes
        {
            get
            {
                return DataSchema.DataSchemaLocalizer.CallWithCultureInfo(new FormattableList<string>(_modificationInfo.Value.LabelTypes.Select(label => label.Name).ToArray()).ToString);
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

                DataSchema.ModifyDocument(EditColumnDescription(nameof(LabelTypes), value), doc =>
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
                                ImmutableList.ValueOf(mods.Append(_modificationInfo.Value.StaticMod)));
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

        public RelativeRT RelativeRetentionTime
        {
            get { return _modificationInfo.Value.StaticMod?.RelativeRT ?? RelativeRT.Unknown; }
            set
            {
                ChangeStaticMod(EditDescription.SetColumn(nameof(RelativeRetentionTime), value), mod=>mod.ChangeRelativeRT(value));
            }
        }

        protected override ModificationInfo GetModificationInfo(DocumentSettings documentSettings)
        {
            ModificationInfo modInfo = new ModificationInfo();
            foreach (var heavyMods in documentSettings.Document.Settings.PeptideSettings.Modifications.HeavyModifications)
            {
                foreach (var mod in heavyMods.Modifications)
                {
                    if (mod.Name == Name)
                    {
                        modInfo = modInfo.ChangeStaticMod(modInfo.StaticMod ?? mod);
                        modInfo = modInfo.ChangeLabelTypes(modInfo.LabelTypes.Append(heavyMods.LabelType));
                    }
                }
            }

            if (modInfo.StaticMod == null)
            {
                foreach (var mod in documentSettings.Settings.IsotopeModifications)
                {
                    if (mod.Name == Name)
                    {
                        modInfo = modInfo.ChangeStaticMod(mod);
                    }
                }
            }

            return modInfo;
        }

        protected override DocumentSettings ChangeDocumentSettingsModificationInfo(DocumentSettings documentSettings,
            ModificationInfo modificationInfoNew)
        {
            var modSettings = documentSettings.Document.Settings.PeptideSettings.Modifications;
            foreach (var typedModifications in modSettings.HeavyModifications)
            {
                var modifications = typedModifications.Modifications;
                if (modificationInfoNew.LabelTypes.Contains(typedModifications.LabelType))
                {
                    modifications = ImmutableList.ValueOf(ReplaceModInList(modifications, modificationInfoNew.StaticMod));
                }
                else
                {
                    modifications = ImmutableList.ValueOf(ReplaceModInList(modifications, null));
                }

                modSettings = modSettings.ChangeModifications(typedModifications.LabelType, modifications);
            }

            var docSettings = documentSettings.Document.Settings;
            docSettings =
                docSettings.ChangePeptideSettings(docSettings.PeptideSettings.ChangeModifications(modSettings));
            documentSettings = documentSettings.ChangeDocument(documentSettings.Document.ChangeSettings(docSettings));
            documentSettings = documentSettings.ChangeSettings(documentSettings.Settings.ChangeIsotopeModifications(
                ReplaceModInList(documentSettings.Settings.IsotopeModifications, modificationInfoNew.StaticMod)));
            return documentSettings;
        }

        public override ElementRef GetElementRef()
        {
            return SettingsListItemRef.PROTOTYPE.ChangeName(Name)
                .ChangeParent(SettingsListRef.IsotopeModification);
        }
    }
}
