using System;
using System.ComponentModel;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.ElementLocators;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class ScalarSetting : SkylineObject
    {
        private PropertyDescriptor _propertyDescriptor;
        public ScalarSetting(SkylineDataSchema dataSchema, PropertyDescriptor propertyDescriptor) : base(dataSchema)
        {
            _propertyDescriptor = propertyDescriptor;
        }

        public string SettingName
        {
            get { return _propertyDescriptor.Name; }
        }

        public string SettingValue
        {
            get
            {
                var scalarSettings = new ScalarSettings(DataSchema.DocumentSettings);
                var value = _propertyDescriptor.GetValue(scalarSettings);
                if (value == null)
                {
                    return null;
                }

                return DataSchema.DataSchemaLocalizer.CallWithCultureInfo(() => value.ToString());
            }

            set
            {
                object convertedValue;
                if (value == null)
                {
                    convertedValue = null;
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, _propertyDescriptor.PropertyType,
                        DataSchema.DataSchemaLocalizer.FormatProvider);
                }

                var editDescription = EditDescription.SetColumn(nameof(SettingValue), value).ChangeElementRef(GetElementRef());
                
                DataSchema.ModifyDocumentAndSettings(editDescription,
                    docSettings =>
                    {
                        var scalarSettings = new ScalarSettings(docSettings);
                        _propertyDescriptor.SetValue(scalarSettings, convertedValue);
                        return scalarSettings.Settings.DocumentSettings;
                    });
            }
        }

        public override ElementRef GetElementRef()
        {
            return SettingRef.PROTOTYPE.ChangeName(SettingName);
        }
    }
}
