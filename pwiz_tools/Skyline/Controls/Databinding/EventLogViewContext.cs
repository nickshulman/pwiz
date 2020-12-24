using System.Collections.Generic;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.EventLog;

namespace pwiz.Skyline.Controls.Databinding
{
    public class EventLogViewContext : AbstractSkylineViewContext
    {
        public EventLogViewContext(DataSchema dataSchema) : base(dataSchema, GetRowSources(dataSchema))
        {

        }



        public static IEnumerable<RowSourceInfo> GetRowSources(DataSchema dataSchema)
        {
            var viewSpec = new ViewSpec().SetName("Default").SetColumns(new[]
                {new ColumnSpec(PropertyPath.Root)});
            var viewInfo = new ViewInfo(dataSchema, typeof(EventLogRow), viewSpec);
            yield return new RowSourceInfo(new EventLogRowSource(), viewInfo);
        }

        public override IEnumerable<ViewGroup> ViewGroups {
            get
            {
                yield return PersistedViews.ExternalToolsGroup;
            }
        }

        public override ViewGroup DefaultViewGroup => PersistedViews.ExternalToolsGroup;
    }
}
