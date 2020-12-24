using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocumentContainers;
using pwiz.Skyline.Model.ElementLocators;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public abstract class NamedSettingsObject<TDef> : SkylineObject
    {
        protected NamedSettingsObject(SkylineDataSchema dataSchema, string name) : base(dataSchema)
        {
            Name = name;
        }

        public string Name { get; private set; }

        protected abstract string GetName(TDef def);
        protected abstract IEnumerable<TDef> GetListFromDocument(SrmDocument document);
        protected abstract SrmDocument ReplaceListInDocument(SrmDocument document, IEnumerable<TDef> newList);

        protected abstract IEnumerable<TDef> GetListFromSettings(SettingsSnapshot settingsSnapshot);

        protected abstract SettingsSnapshot ReplaceListInSettings(SettingsSnapshot settingsSnapshot,
            IEnumerable<TDef> newList);

        protected abstract SettingsListRef GetSettingsListRef();

        public override ElementRef GetElementRef()
        {
            return SettingRef.PROTOTYPE.ChangeName(Name).ChangeParent(GetSettingsListRef());
        }

        protected IEnumerable<TDef> ReplaceInList(IEnumerable<TDef> items, TDef newItem)
        {
            bool found = false;
            foreach (var item in items)
            {
                if (GetName(item) == Name)
                {
                    if (newItem != null)
                    {
                        yield return newItem;
                    }
                    found = true;
                }
            }

            if (!found && newItem != null)
            {
                yield return newItem;
            }
        }

        public string Locator { get { return GetLocator(); } }

        protected TDef FindItem(DocumentSettings documentSettings)
        {
            return GetListFromDocument(documentSettings.Document).Concat(GetListFromSettings(documentSettings.Settings))
                .FirstOrDefault(obj => GetName(obj) == Name);
        }

        protected DocumentSettings ReplaceItem(DocumentSettings documentSettings, TDef newItem)
        {
            var newSettings = ReplaceListInSettings(documentSettings.Settings,
                ReplaceInList(GetListFromSettings(documentSettings.Settings), newItem));
            return documentSettings.ChangeDocument(ReplaceItemInDocument(documentSettings.Document, newItem))
                .ChangeSettings(newSettings);
        }

        protected void ModifyItem(EditDescription editDescription, Func<TDef, TDef> modifier)
        {
            DataSchema.ModifyDocumentAndSettings(editDescription, documentAndSettings =>
                ReplaceItem(documentAndSettings, modifier(FindItem(documentAndSettings)))
            );
        }

        protected SrmDocument ReplaceItemInDocument(SrmDocument document, TDef newItem)
        {
            return ReplaceListInDocument(document, ReplaceInList(GetListFromDocument(document), newItem));
        }

        protected bool IsPartOfDocument()
        {
            return GetListFromDocument(DataSchema.Document).Any(item => GetName(item) == Name);
        }
    }
}
