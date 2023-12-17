using System;

namespace pwiz.Common.PropertyDescriptors
{
    public class OrderAttribute : Attribute
    {
        public OrderAttribute(int ordinal)
        {
            Ordinal = ordinal;
        }
        public int Ordinal { get; set; }
    }
}
