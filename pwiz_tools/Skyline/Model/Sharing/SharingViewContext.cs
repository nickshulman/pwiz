using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Databinding;

namespace pwiz.Skyline.Model.Sharing
{
    public class SharingViewContext : SkylineViewContext
    {
        private ViewLayout _viewLayout;
        public SharingViewContext(ViewInfo viewInfo, ViewLayout viewLayout, IEnumerable rows) 
            : base(viewInfo.ParentColumn, new StaticRowSource(rows))
        {
            _viewLayout = viewLayout;
        }

        public override IRowSource GetRowSource(ViewInfo viewInfo)
        {
            return RowSources.First().Rows;
        }

        public override ViewLayoutList GetViewLayoutList(ViewName viewName)
        {
            if (_viewLayout == null)
            {
                return ViewLayoutList.EMPTY;
            }

            return new ViewLayoutList(viewName.Name).ChangeDefaultLayoutName(_viewLayout.Name)
                .ChangeLayouts(new[] {_viewLayout});
        }

        public void WriteToStream(IProgressMonitor progressMonitor, BindingListSource bindingListSource, DsvWriter dsvWriter, TextWriter textWriter)
        {
            WriteData(progressMonitor, textWriter, bindingListSource, dsvWriter);
        }
    }
}
