using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Model.DocumentContainers
{
    public class SettingsSnapshot : Immutable
    {
        private SettingsSnapshot()
        {

        }

        public static SettingsSnapshot FromSettings(Properties.Settings settings)
        {
            return new SettingsSnapshot()
            {
                Enzymes = ImmutableList.ValueOf(settings.EnzymeList),
                AnnotationDefs = ImmutableList.ValueOf(settings.AnnotationDefList),
                StructuralModifications = ImmutableList.ValueOf(settings.StaticModList),
                IsotopeModifications = ImmutableList.ValueOf(settings.HeavyModList),
            };
        }

        public void UpdateSettings(SettingsSnapshot previous, Settings settings)
        {
            UpdateSettingsList(settings.EnzymeList, Enzymes, previous?.Enzymes);
            UpdateSettingsList(settings.AnnotationDefList, AnnotationDefs, previous?.AnnotationDefs);
            UpdateSettingsList(settings.StaticModList, StructuralModifications, previous?.StructuralModifications);
            UpdateSettingsList(settings.HeavyModList, IsotopeModifications, previous?.IsotopeModifications);
        }

        public ImmutableList<Enzyme> Enzymes { get; private set; }

        public SettingsSnapshot ChangeEnzymes(IEnumerable<Enzyme> enzymes)
        {
            return ChangeProp(ImClone(this), im => im.Enzymes = ImmutableList.ValueOf(enzymes));
        }
        public ImmutableList<AnnotationDef> AnnotationDefs { get; private set; }

        public SettingsSnapshot ChangeAnnotationDefs(IEnumerable<AnnotationDef> annotationDefs)
        {
            return ChangeProp(ImClone(this), im => im.AnnotationDefs = ImmutableList.ValueOf(annotationDefs));
        }
        public ImmutableList<StaticMod> StructuralModifications { get; private set; }

        public SettingsSnapshot ChangeStructuralModifications(IEnumerable<StaticMod> staticMods)
        {
            return ChangeProp(ImClone(this), im => im.StructuralModifications = ImmutableList.ValueOf(staticMods));
        }
        public ImmutableList<StaticMod> IsotopeModifications { get; private set; }

        public SettingsSnapshot ChangeIsotopeModifications(IEnumerable<StaticMod> staticMods)
        {
            return ChangeProp(ImClone(this), im => im.IsotopeModifications = ImmutableList.ValueOf(staticMods));
        }

        public static void UpdateSettingsList<T>(SettingsList<T> settingsList, ImmutableList<T> newValues,
            ImmutableList<T> oldValues) where T : XmlNamedElement
        {
            if (ReferenceEquals(newValues, oldValues))
            {
                return;
            }

            foreach (var item in newValues)
            {
                if (!settingsList.Contains(item))
                {
                    settingsList.SetValue(item);
                }
            }

            if (oldValues == null)
            {
                return;
            }

            var itemsToDelete = new HashSet<string>(oldValues.Select(item => item.Name));
            foreach (var item in newValues)
            {
                itemsToDelete.Remove(item.Name);
            }

            foreach (var name in itemsToDelete)
            {
                var item = settingsList[name];
                if (item != null)
                {
                    settingsList.Remove(item);
                }
            }
        }

        protected bool Equals(SettingsSnapshot other)
        {
            return Enzymes.Equals(other.Enzymes) && AnnotationDefs.Equals(other.AnnotationDefs) &&
                   StructuralModifications.Equals(other.StructuralModifications) &&
                   IsotopeModifications.Equals(other.IsotopeModifications);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SettingsSnapshot) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Enzymes.GetHashCode();
                hashCode = (hashCode * 397) ^ AnnotationDefs.GetHashCode();
                hashCode = (hashCode * 397) ^ StructuralModifications.GetHashCode();
                hashCode = (hashCode * 397) ^ IsotopeModifications.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(SettingsSnapshot left, SettingsSnapshot right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SettingsSnapshot left, SettingsSnapshot right)
        {
            return !Equals(left, right);
        }
    }
}
