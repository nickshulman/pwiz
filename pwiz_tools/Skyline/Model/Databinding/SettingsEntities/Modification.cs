using System;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public abstract class AbstractModification : SkylineObject
    {
        protected AbstractModification(SkylineDataSchema dataSchema, string name) : base(dataSchema)
        {
            Name = name;
        }

        public string Name { get; private set; }

        protected abstract StaticMod StaticMod { get; }
        protected abstract SettingsList<StaticMod> SettingsList { get; }
        protected ModificationInfo GetModificationInfo()

        protected void ChangeStaticMod(EditDescription editDescription, Func<StaticMod, StaticMod> changeFunc)
        {
            editDescription = editDescription.ChangeElementRef(GetElementRef());
            DataSchema.ModifyDocument(editDescription, doc=>ChangeStaticMod(doc, ));
        }

        protected abstract SrmDocument ChangeStaticMod(SrmDocument document, StaticMod newStaticMod);
        protected abstract StaticMod GetStaticMod(SrmDocument document);


        public string AminoAcids
        {
            get { return StaticMod?.AAs; }
            set
            {
                var staticMod = StaticMod;
                staticMod = new StaticMod(staticMod.Name, value, staticMod.Terminus, staticMod.IsVariable,
                    staticMod.Formula, staticMod.LabelAtoms, staticMod.RelativeRT, staticMod.MonoisotopicMass,
                    staticMod.AverageMass, staticMod.Losses, staticMod.UnimodId, staticMod.ShortName,
                    staticMod.PrecisionRequired);
                ChangeStaticMod(staticMod);
            }
        }

        public string Terminus
        {
            get { return StaticMod?.Terminus?.ToString(); }
        }

        public string Formula
        {
            get { return StaticMod?.Formula; }
        }

        protected class ModificationInfo : Immutable
        {
            public StaticMod StaticMod { get; private set; }

            public ModificationInfo ChangeStaticMod(StaticMod staticMod)
            {
                return ChangeProp(ImClone(this), im => im.StaticMod = staticMod);
            }
        }
    }
}
