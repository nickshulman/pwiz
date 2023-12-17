using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Controls.Alignment
{
    public class UserInterfaceTypeConverter<T> : TypeConverter
    {
        public UserInterfaceTypeConverter()
        {
            Console.Out.WriteLine("Here");
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var declaredProperties = CollectionUtil.SafeToDictionary(typeof(T).GetProperties(BindingFlags.Public)
                .Select((propertyInfo, index) => new KeyValuePair<string, int>(propertyInfo.Name, index)));
            var sortedProperties = TypeDescriptor.GetProperties(value.GetType()).Cast<PropertyDescriptor>().OrderBy(x =>
            {
                if (declaredProperties.TryGetValue(x.Name, out int order))
                {
                    return order;
                }

                return int.MaxValue;
            });
            return new PropertyDescriptorCollection(sortedProperties.ToArray());
        }
    }
}
