using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Common.PropertyDescriptors
{
    public class PropertiesObject : ICustomTypeDescriptor
    {
        private ImmutableList<PropertyDescriptor> _properties;
        public virtual AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public virtual string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public virtual string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public virtual TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public virtual EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public virtual PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public virtual object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public virtual EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public virtual EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public virtual PropertyDescriptorCollection GetProperties()
        {
            return SortProperties(TypeDescriptor.GetProperties(this, true));
        }

        public virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return SortProperties(TypeDescriptor.GetProperties(this, attributes, true));
        }

        public virtual object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        protected virtual PropertyDescriptorCollection SortProperties(PropertyDescriptorCollection propertyDescriptorCollection)
        {
            return propertyDescriptorCollection;
            return new PropertyDescriptorCollection(propertyDescriptorCollection.Cast<PropertyDescriptor>()
                .OrderBy(p => p, Comparer<PropertyDescriptor>.Create(CompareProperties)).ToArray());
        }

        protected virtual int CompareProperties(PropertyDescriptor pd1, PropertyDescriptor pd2)
        {
            var orderAttribute1 = pd1.Attributes.OfType<OrderAttribute>().FirstOrDefault();
            var orderAttribute2 = pd2.Attributes.OfType<OrderAttribute>().FirstOrDefault();
            return (orderAttribute1?.Ordinal ?? int.MaxValue).CompareTo(orderAttribute2?.Ordinal ?? int.MaxValue);
        }
    }
}
