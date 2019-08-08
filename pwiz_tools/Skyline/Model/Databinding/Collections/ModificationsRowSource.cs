using System.Collections.Generic;
using System.Linq;
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
            var documentSettings = DataSchema.DocumentSettings;
            var modifications = documentSettings.Document.Settings.PeptideSettings.Modifications;
            return modifications.StaticModifications
                .Concat(documentSettings.Settings.StructuralModifications)
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
            var documentSettings = DataSchema.DocumentSettings;
            var modifications = documentSettings.Document.Settings.PeptideSettings.Modifications;
            return modifications.HeavyModifications.SelectMany(typedMods=>typedMods.Modifications)
                .Concat(documentSettings.Settings.IsotopeModifications)
                .Select(mod => mod.Name)
                .Distinct();
        }

        protected override IsotopeModification ConstructItem(string key)
        {
            return new IsotopeModification(DataSchema, key);
        }
    }
}
