using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Util;
using Serilog.Events;

namespace pwiz.Skyline.Controls.Databinding
{
    public partial class EventLogForm : DataboundGridForm
    {
        public EventLogForm()
        {
            InitializeComponent();
            var dataSchema = new DataSchema();
            var viewContext = new EventLogViewContext(dataSchema);
            BindingListSource.SetViewContext(viewContext);
        }
    }
}
