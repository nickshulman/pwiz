using System;
using System.Collections.Generic;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.SettingsEntities;

namespace pwiz.Skyline.Model
{
    public class SettingsViewContext : SkylineViewContext
    {
        public SettingsViewContext(SkylineDataSchema dataSchema) : base(dataSchema, GetSettingsRowSources(dataSchema))
        {

        }

        public static IEnumerable<RowSourceInfo> GetSettingsRowSources(SkylineDataSchema dataSchema)
        {
            yield return new RowSourceInfo(typeof(StructuralModification), new StructuralModificationsRowSource(dataSchema),
                new[]
                {
                    GetViewInfo(dataSchema, typeof(StructuralModification), "Structural Modifications", viewSpec=>viewSpec.SetSublistId(
                        PropertyPath.Root.Property(nameof(StructuralModification.Losses)).LookupAllItems()))
                });
            yield return new RowSourceInfo(typeof(IsotopeModification), new IsotopeModificationsRowSource(dataSchema),
                new[] {GetViewInfo(dataSchema, typeof(IsotopeModification), "Isotope Modifications")}
            );
        }

        private static ViewInfo GetViewInfo(SkylineDataSchema dataSchema, Type rowType, string name)
        {
            return GetViewInfo(dataSchema, rowType, name, x => x);
        }

        private static ViewInfo GetViewInfo(SkylineDataSchema dataSchema, Type rowType, string name,
            Func<ViewSpec, ViewSpec> transformFunc)
        {
            var parentColumn = ColumnDescriptor.RootColumn(dataSchema, rowType);
            return new ViewInfo(parentColumn, transformFunc(GetDefaultViewSpec(parentColumn).SetName(name)));

        }
    }
}
