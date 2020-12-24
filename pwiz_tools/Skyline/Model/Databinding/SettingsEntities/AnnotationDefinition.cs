using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocumentContainers;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class AnnotationDefinition : NamedSettingsObject<AnnotationDef>
    {
        private CachedValue<AnnotationDef> _annotationDef;
        public AnnotationDefinition(SkylineDataSchema dataSchema, string name) : base(dataSchema, name)
        {
            _annotationDef = CachedValue.Create(dataSchema, FindAnnotationDef);
        }

        protected override string GetName(AnnotationDef def)
        {
            return def.Name;
        }

        protected override SettingsListRef GetSettingsListRef()
        {
            return SettingsListRef.Annotation;
        }

        protected override IEnumerable<AnnotationDef> GetListFromDocument(SrmDocument document)
        {
            return document.Settings.DataSettings.AnnotationDefs;
        }

        protected override SrmDocument ReplaceListInDocument(SrmDocument document, IEnumerable<AnnotationDef> newList)
        {
            return document.ChangeSettings(document.Settings.ChangeDataSettings(
                document.Settings.DataSettings.ChangeAnnotationDefs(ImmutableList.ValueOf(newList))));
        }

        protected override IEnumerable<AnnotationDef> GetListFromSettings(SettingsSnapshot settingsSnapshot)
        {
            return settingsSnapshot.AnnotationDefs;
        }

        protected override SettingsSnapshot ReplaceListInSettings(SettingsSnapshot settingsSnapshot, IEnumerable<AnnotationDef> newList)
        {
            return settingsSnapshot.ChangeAnnotationDefs(newList);
        }


        private AnnotationDef FindAnnotationDef()
        {
            return FindItem(DataSchema.DocumentSettings) ?? (AnnotationDef) AnnotationDef.EMPTY.ChangeName(Name);
        }

        public bool PartOfDocument
        {
            get
            {
                return IsPartOfDocument();
            }
            set
            {
                if (value == PartOfDocument)
                {
                    return;
                }
                DataSchema.ModifyDocument(EditColumnDescription(nameof(PartOfDocument), value),
                    doc=>ReplaceItemInDocument(doc, value ? _annotationDef.Value : null));
            }
        }

        public string DropDownValues
        {
            get
            {
                var items = _annotationDef.Value.Items;
                if (items == null || items.Count == 0)
                {
                    return null;
                }
                return new FormattableList<string>(items).ToString();
            }
            set
            {
                var newItems = string.IsNullOrEmpty(value) ? null : FormattableList<string>.Parse(value);
                ModifyItem(EditColumnDescription(nameof(DropDownValues), value), def=>def.ChangeItems(newItems));
            }
        }

        public string AppliesTo
        {
            get
            {
                return new FormattableList<AnnotationDef.AnnotationTarget>(_annotationDef.Value.AnnotationTargets).ToString();
            }
            set
            {
                var newTargets = new List<AnnotationDef.AnnotationTarget>();
                if (!string.IsNullOrEmpty(value))
                {
                    var list = FormattableList<string>.Parse(value);
                    foreach (var item in list)
                    {
                        newTargets.Add(TypeSafeEnum.Parse<AnnotationDef.AnnotationTarget>(item));
                    }
                }

                ModifyItem(EditColumnDescription(nameof(AppliesTo), value), def=>
                    new AnnotationDef(def.Name, AnnotationDef.AnnotationTargetSet.OfValues(newTargets), def.ListPropertyType, def.Items));
            }
        }
    }
}
