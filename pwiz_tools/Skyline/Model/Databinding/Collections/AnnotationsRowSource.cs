using System.Collections.Generic;
using System.Linq;
using pwiz.Skyline.Model.Databinding.SettingsEntities;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class AnnotationsRowSource : SkylineObjectList<string, AnnotationDefinition>
    {
        public AnnotationsRowSource(SkylineDataSchema dataSchema) : base(dataSchema)
        {

        }

        protected override AnnotationDefinition ConstructItem(string key)
        {
            return new AnnotationDefinition(DataSchema, key);
        }

        protected override IEnumerable<string> ListKeys()
        {
            return DataSchema.Document.Settings.DataSettings.AnnotationDefs
                .Concat(DataSchema.DocumentSettings.Settings.AnnotationDefs).Select(def => def.Name).Distinct();
        }
    }
}
