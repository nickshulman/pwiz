using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using pwiz.Skyline.Model.Databinding.SettingsEntities;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class ScalarSettingRowSource : SkylineObjectList<string, ScalarSetting>
    {
        private static IDictionary<string, PropertyDescriptor> _propertyDescriptors =
            ScalarSettings.EnumerateSettings().ToDictionary(pd => pd.Name);
        public ScalarSettingRowSource(SkylineDataSchema dataSchema) : base(dataSchema)
        {
        }

        protected override IEnumerable<string> ListKeys()
        {
            return _propertyDescriptors.Keys;
        }

        protected override ScalarSetting ConstructItem(string key)
        {
            return new ScalarSetting(DataSchema, _propertyDescriptors[key]);
        }
    }
}
