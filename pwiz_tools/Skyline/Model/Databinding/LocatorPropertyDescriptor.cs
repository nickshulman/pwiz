using System;
using System.ComponentModel;
using pwiz.Skyline.Model.ElementLocators;

namespace pwiz.Skyline.Model.Databinding
{
    public class LocatorPropertyDescriptor : PropertyDescriptor
    {
        private PropertyDescriptor _basePropertyDescriptor;

        public LocatorPropertyDescriptor(string name, PropertyDescriptor basePropertyDescriptor) : base(
            name,
            new[] {new DisplayNameAttribute(name)})
        {
            _basePropertyDescriptor = basePropertyDescriptor;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            var locatable = _basePropertyDescriptor.GetValue(component) as ILocatable;
            if (locatable == null)
            {
                return null;
            }

            return locatable.GetElementRef();
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
            throw new InvalidOperationException();
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get { return _basePropertyDescriptor.ComponentType; }
        }

        public override bool IsReadOnly
        {
            get { return true; }
        }
        public override Type PropertyType { get {return typeof(ElementRef);} }
    }
}
