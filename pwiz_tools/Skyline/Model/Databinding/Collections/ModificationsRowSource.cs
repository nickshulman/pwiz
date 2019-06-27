using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Model.Databinding.SettingsEntities;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class StructuralModificationsRowSource : SkylineObjectList<string, StructuralModification>
    {
        public StructuralModificationsRowSource(SkylineDataSchema dataSchema) : base(dataSchema)
        {
        }

        protected override IEnumerable<string> ListKeys()
        {
            var modifications = DataSchema.Document.Settings.PeptideSettings.Modifications;
            var settings = Properties.Settings.Default;
            return modifications.StaticModifications
                .Concat(settings.StaticModList)
                .Select(mod => mod.Name)
                .Distinct();
        }

        protected override StructuralModification ConstructItem(string key)
        {
            return new StructuralModification(DataSchema, key);
        }
    }

    public class IsotopeModificationsRowSource : SkylineObjectList<string, IsotopeModification>
    {
        public IsotopeModificationsRowSource(SkylineDataSchema dataSchema) : base(dataSchema)
        {

        }

        protected override IEnumerable<string> ListKeys()
        {
            var modifications = DataSchema.Document.Settings.PeptideSettings.Modifications;
            var settings = Properties.Settings.Default;
            return modifications.HeavyModifications.SelectMany(typedMods=>typedMods.Modifications)
                .Concat(settings.HeavyModList)
                .Select(mod => mod.Name)
                .Distinct();
        }

        protected override IsotopeModification ConstructItem(string key)
        {
            return new IsotopeModification(DataSchema, key);
        }
    }
}
