using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Skyline.Model.Databinding.SettingsEntities;

namespace pwiz.Skyline.Model.ElementLocators
{
    public class SettingRef : ElementRef
    {
        public static readonly SettingRef PROTOTYPE = new SettingRef();

        public SettingRef() : base(DocumentRef.PROTOTYPE)
        {
        }

        public override string ElementType
        {
            get { return @"Setting"; }
        }

        protected override IEnumerable<ElementRef> EnumerateSiblings(SrmDocument document)
        {
            return ScalarSettings.EnumerateSettings().Select(pd => ChangeName(pd.Name));
        }
    }

    public class SettingsListRef : ElementRef
    {
        public static readonly SettingsListRef PROTOTYPE = new SettingsListRef();
        public static readonly SettingsListRef StructuralModification 
            = (SettingsListRef) PROTOTYPE.ChangeName(@"StructuralModification");

        public static readonly SettingsListRef IsotopeModification
            = (SettingsListRef) PROTOTYPE.ChangeName(@"IsotopeModification");

        public static readonly SettingsListRef Annotation
            = (SettingsListRef) PROTOTYPE.ChangeName(@"Annotation");

        public SettingsListRef() : base(DocumentRef.PROTOTYPE)
        {
        }

        public override string ElementType
        {
            get { return @"SettingsList"; }
        }
        protected override IEnumerable<ElementRef> EnumerateSiblings(SrmDocument document)
        {
            yield return StructuralModification;
            yield return IsotopeModification;
        }
    }

    public class SettingsListItemRef : ElementRef
    {
        public static readonly SettingsListItemRef PROTOTYPE = new SettingsListItemRef();

        public SettingsListItemRef() : base(SettingsListRef.PROTOTYPE)
        {

        }

        public override string ElementType
        {
            get { return @"SettingsListItem"; }
        }

        protected override IEnumerable<ElementRef> EnumerateSiblings(SrmDocument document)
        {
            yield break;
        }
    }
}
