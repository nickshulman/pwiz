using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocumentContainers;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class StructuralModification : AbstractModification
    {
        public StructuralModification(SkylineDataSchema dataSchema, string name) : base(dataSchema, name)
        {
        }

        public bool Explicit
        {
            get { return _modificationInfo.Value.StaticMod?.IsExplicit ?? false; }
            set
            {
                ChangeStaticMod(EditDescription.SetColumn(nameof(Explicit), value), mod=>mod.ChangeExplicit(value));
            }
        }

        protected override ModificationInfo GetModificationInfo(DocumentSettings documentSettings)
        {
            var modInfo = new ModificationInfo();
            foreach (var mod in documentSettings.Document.Settings.PeptideSettings.Modifications.StaticModifications)
            {
                if (mod.Name == Name)
                {
                    modInfo = modInfo.ChangeStaticMod(mod).ChangeLabelTypes(ImmutableList.Singleton(IsotopeLabelType.light));
                    break;
                }
            }

            if (modInfo.StaticMod == null)
            {
                foreach (var mod in documentSettings.Settings.StructuralModifications)
                {
                    if (mod.Name == Name)
                    {
                        modInfo = modInfo.ChangeStaticMod(mod);
                        break;
                    }
                }
            }

            return modInfo;
        }

        protected override DocumentSettings ChangeDocumentSettingsModificationInfo(DocumentSettings documentSettings,
            ModificationInfo modificationInfoNew)
        {
            var staticModifications = ImmutableList.ValueOf(ReplaceModInList(
                documentSettings.Document.Settings.PeptideSettings.Modifications.StaticModifications,
                modificationInfoNew.StaticMod));

            if (!ArrayUtil.EqualsDeep(staticModifications,
                documentSettings.Document.Settings.PeptideSettings.Modifications.StaticModifications))
            {
                var settings = documentSettings.Document.Settings;
                settings = settings.ChangePeptideSettings(settings.PeptideSettings.ChangeModifications(
                    settings.PeptideSettings.Modifications.ChangeStaticModifications(staticModifications)));
                var document = documentSettings.Document;
                document = document.ChangeSettings(settings);
                documentSettings = documentSettings.ChangeDocument(document);
            }

            documentSettings = documentSettings.ChangeSettings(documentSettings.Settings.ChangeStructuralModifications(
                ReplaceModInList(documentSettings.Settings.StructuralModifications, modificationInfoNew.StaticMod)));
            return documentSettings;
        }

        public bool Variable
        {
            get { return _modificationInfo.Value.StaticMod?.IsVariable ?? false; }
            set
            {
                ChangeStaticMod(EditDescription.SetColumn(nameof(Variable), value), mod=>mod.ChangeVariable(value));
            }
        }

        public IList<NeutralLoss> Losses
        {
            get
            {
                var losses = _modificationInfo.Value.StaticMod?.Losses;
                if (losses == null)
                {
                    return null;
                }

                return losses.Select(loss => new NeutralLoss(loss)).ToArray();
            }
        }

        public override ElementRef GetElementRef()
        {
            return SettingsListItemRef.PROTOTYPE
                .ChangeName(Name)
                .ChangeParent(SettingsListRef.StructuralModification);
        }
    }
}
