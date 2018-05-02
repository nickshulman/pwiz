using pwiz.Common.DataBinding;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;

namespace pwiz.Skyline.Controls.Graphs
{
    public class ChromatogramGridForm : DataboundGridForm
    {
        public ChromatogramGridForm(SkylineDataSchema dataSchema)
        {
            BindingListSource.QueryLock = dataSchema.QueryLock;
            var parentColumn = ColumnDescriptor.RootColumn(dataSchema, typeof(ChromatogramGroup));
            var viewSpec = SkylineViewContext.GetDefaultViewInfo(parentColumn).ViewSpec;
            viewSpec = viewSpec.SetSublistId(PropertyPath.Root.Property("TransitionChromatograms").LookupAllItems());
            var viewInfo = new ViewInfo(parentColumn, viewSpec).ChangeViewGroup(ViewGroup.BUILT_IN);
            var viewContext = new SkylineViewContext(dataSchema, new []
            {
                new RowSourceInfo(new Chromatograms(dataSchema), viewInfo)
            });
            BindingListSource.SetViewContext(viewContext);
        }
    }
}
