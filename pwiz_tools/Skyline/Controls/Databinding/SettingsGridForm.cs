using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding;

namespace pwiz.Skyline.Controls.Databinding
{
    public class SettingsGridForm : DocumentGridForm
    {
        public SettingsGridForm(SettingsViewContext settingsViewContext) : base(settingsViewContext, "Settings Grid")
        {

        }

        public SettingsGridForm(IDocumentUIContainer documentContainer) : this(new SettingsViewContext(new SkylineDataSchema(documentContainer, SkylineDataSchema.GetLocalizedSchemaLocalizer())))
        {

        }
    }
}
