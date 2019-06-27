using System.Collections.Generic;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public abstract class SettingsCollection<T> where T : IKeyContainer<string>
    {
        protected abstract IEnumerable<MappedList<string, T>> GetSettingsLists(Properties.Settings applicationSettings);
    }
}
