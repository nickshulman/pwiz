using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping;

namespace pwiz.Skyline.Model.DocumentContainers
{
    public class DocumentSettings
    {
        public DocumentSettings(SrmDocument document, SettingsSnapshot settings)
        {
            Document = document;
            Settings = settings;
            ReferenceId = new object();
        }

        public SrmDocument Document { get; private set; }
        public SettingsSnapshot Settings { get; private set; }
        public object ReferenceId { get; private set; }

        public DocumentSettings BeginDeferSettingsChanges()
        {
            if (Document.DeferSettingsChanges)
            {
                return this;
            }
            return new DocumentSettings(Document.BeginDeferSettingsChanges(), Settings);
        }
    }
}
