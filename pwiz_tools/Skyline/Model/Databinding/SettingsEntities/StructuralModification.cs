using System;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.ElementLocators;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class StructuralModification : AbstractModification
    {
        private CachedValue<StructuralModificationInfo> _modInfo;
        public StructuralModification(SkylineDataSchema dataSchema, string name) : base(dataSchema, name)
        {
            _modInfo = CachedValue.Create(dataSchema, GetModificationInfo);
        }

        protected override StaticMod StaticMod
        {
            get { return _modInfo.Value.StaticMod; }
        }

        protected override void ChangeStaticMod(EditDescription editDescription, Func<StaticMod, StaticMod> staticMod)
        {
            
        }

        protected StructuralModificationInfo GetModificationInfo()
        {
            var modInfo = new StructuralModificationInfo();
            foreach (var mod in SrmDocument.Settings.PeptideSettings.Modifications.StaticModifications)
            {
                if (mod.Name == Name)
                {
                    modInfo = modInfo.ChangeStaticMod(mod);
                    break;
                }
            }

            if (modInfo.StaticMod == null)
            {
                foreach (var mod in Properties.Settings.Default.StaticModList)
                {
                    if (mod.Name == Name)
                    {
                        modInfo = modInfo.ChangeStaticMod(mod);
                        modInfo = modInfo.ChangeExplicit(true);
                        break;
                    }
                }
            }

            return modInfo;
        }

        public override ElementRef GetElementRef()
        {
            return SettingsListItemRef.PROTOTYPE
                .ChangeName(Name)
                .ChangeParent(SettingsListRef.StructuralModification);
        }

        protected class StructuralModificationInfo : Immutable
        {
            public StaticMod StaticMod { get; private set; }

            public StructuralModificationInfo ChangeStaticMod(StaticMod staticMod)
            {
                return ChangeProp(ImClone(this), im => im.StaticMod = staticMod);
            }
            public bool Explicit { get; private set; }
            public StructuralModificationInfo ChangeExplicit(bool value)
            {
                return ChangeProp(ImClone(this), im => im.Explicit = value);
            }
        }
    }
}
