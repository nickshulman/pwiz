using System;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Databinding.Settings
{
    public interface ISimpleSetting
    {
        string Name { get; }
        object GetValue(SrmSettings srmSettings);
        SrmSettings SetValue(SrmSettings srmSettings, object value);
        Type ValueType { get; }
    }

    public class SimpleSetting<T> : ISimpleSetting
    {
        private Func<SrmSettings, T> _getter;
        private Func<SrmSettings, T, SrmSettings> _setter;
        public string Name { get; private set; }
        public Type ValueType
        {
            get { return typeof(T); }
        }

        public T GetValue(SrmSettings srmSettings)
        {
            return _getter(srmSettings);
        }

        public SrmSettings SetValue(SrmSettings srmSettings, T value)
        {
            return _setter(srmSettings, value);
        }

        object ISimpleSetting.GetValue(SrmSettings srmSettings)
        {
            return GetValue(srmSettings);
        }
    }
}
