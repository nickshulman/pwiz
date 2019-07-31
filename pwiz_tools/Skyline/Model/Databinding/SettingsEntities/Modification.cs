using System;
using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public abstract class AbstractModification : SkylineObject
    {
        protected CachedValue<ModificationInfo> _modificationInfo;
        protected AbstractModification(SkylineDataSchema dataSchema, string name) : base(dataSchema)
        {
            Name = name;
            _modificationInfo = CachedValue.Create(DataSchema, () => GetModificationInfo(DataSchema.DocumentSettings));
        }

        public string Name { get; private set; }

        protected abstract ModificationInfo GetModificationInfo(DocumentSettings documentSettings);
        protected abstract DocumentSettings ChangeDocumentSettingsModificationInfo(DocumentSettings documentSettings,
            ModificationInfo modificationInfoNew);

        protected void ChangeStaticMod(EditDescription editDescription, Func<StaticMod, StaticMod> changeFunc)
        {
            ChangeModificationInfo(editDescription, modInfo=>modInfo.ChangeStaticMod(changeFunc(modInfo.StaticMod)));
        }

        protected void ChangeModificationInfo(EditDescription editDescription,
            Func<ModificationInfo, ModificationInfo> changeFunc)
        {
            editDescription = editDescription.ChangeElementRef(GetElementRef());
            DataSchema.ModifyDocumentAndSettings(editDescription, docSettings => ChangeDocumentSettingsModificationInfo(docSettings, changeFunc(GetModificationInfo(docSettings))));

        }

        public string AminoAcids
        {
            get { return _modificationInfo.Value.StaticMod?.AAs; }
            set
            {
                ChangeStaticMod(EditDescription.SetColumn(nameof(AminoAcids), value), staticMod =>
                {
                    return new StaticMod(staticMod.Name, value, staticMod.Terminus, staticMod.IsVariable,
                        staticMod.Formula, staticMod.LabelAtoms, staticMod.RelativeRT, staticMod.MonoisotopicMass,
                        staticMod.AverageMass, staticMod.Losses, staticMod.UnimodId, staticMod.ShortName,
                        staticMod.PrecisionRequired);
                });
            }
        }

        public string Terminus
        {
            get { return _modificationInfo.Value.StaticMod?.Terminus?.ToString(); }
        }

        public string Formula
        {
            get { return _modificationInfo.Value.StaticMod?.Formula; }
        }

        protected class ModificationInfo : Immutable
        {
            public ModificationInfo()
            {
                LabelTypes = ImmutableList<IsotopeLabelType>.EMPTY;
            }
            public StaticMod StaticMod { get; private set; }
            public ImmutableList<IsotopeLabelType> LabelTypes { get; private set; }

            public ModificationInfo ChangeStaticMod(StaticMod staticMod)
            {
                return ChangeProp(ImClone(this), im => im.StaticMod = staticMod);
            }

            public ModificationInfo ChangeLabelTypes(IEnumerable<IsotopeLabelType> labelTypes)
            {
                return ChangeProp(ImClone(this), im => im.LabelTypes = ImmutableList.ValueOfOrEmpty(labelTypes));
            }
        }

        protected IEnumerable<StaticMod> ReplaceModInList(IEnumerable<StaticMod> modifications, StaticMod newMod)
        {
            bool found = false;
            foreach (var mod in modifications)
            {
                if (mod.Name == Name)
                {
                    if (newMod != null)
                    {
                        yield return newMod;
                    }

                    found = true;
                }
                else
                {
                    yield return mod;
                }
            }

            if (!found && null != newMod)
            {
                yield return newMod;
            }
        }
    }
}
