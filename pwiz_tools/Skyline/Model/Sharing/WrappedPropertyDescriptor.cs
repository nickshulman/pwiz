using System;
using System.ComponentModel;

namespace pwiz.Skyline.Model.Sharing
{
    public class WrappedPropertyDescriptor : PropertyDescriptor
    {
        public WrappedPropertyDescriptor(PropertyDescriptor propertyDescriptor, string name, Attribute[] attributes) : base(name, attributes)
        {
            InnerPropertyDescriptor = propertyDescriptor;
        }

        public PropertyDescriptor InnerPropertyDescriptor { get; private set; }

        public override bool CanResetValue(object component)
        {
            return InnerPropertyDescriptor.CanResetValue(component);
        }

        public override object GetValue(object component)
        {
            return InnerPropertyDescriptor.GetValue(component);
        }

        public override void ResetValue(object component)
        {
            InnerPropertyDescriptor.ResetValue(component);
        }

        public override void SetValue(object component, object value)
        {
            InnerPropertyDescriptor.SetValue(component, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return InnerPropertyDescriptor.ShouldSerializeValue(component);
        }

        public override Type ComponentType
        {
            get { return InnerPropertyDescriptor.ComponentType; }
        }
        public override bool IsReadOnly
        {
            get { return InnerPropertyDescriptor.IsReadOnly; }
        }
        public override Type PropertyType
        {
            get { return InnerPropertyDescriptor.PropertyType; }
        }
    }
}
