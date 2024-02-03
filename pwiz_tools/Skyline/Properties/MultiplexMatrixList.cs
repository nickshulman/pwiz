using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.SettingsUI;

namespace pwiz.Skyline.Properties
{
    public class MultiplexMatrixList : SettingsList<MultiplexMatrix>
    {
        public override IEnumerable<MultiplexMatrix> GetDefaults(int revisionIndex)
        {
            return new[] { MultiplexMatrix.NONE };
        }

        public override string Title
        {
            get { return "Multiplex Matrices"; }
        }

        public override string Label
        {
            get
            {
                return "Multiplex Matrix";
            }
        }

        public override MultiplexMatrix EditItem(Control owner, MultiplexMatrix item, IEnumerable<MultiplexMatrix> existing, object tag)
        {
            using (var dlg = new MultiplexingDlg(Settings.Default.MeasuredIonList.Where(ion => ion.IsCustom).ToList(),
                       item, existing))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    return dlg.MultiplexMatrix;
                }

                return null;
            }
        }

        public override MultiplexMatrix CopyItem(MultiplexMatrix item)
        {
            return (MultiplexMatrix) item.ChangeName(string.Empty);
        }

        public override string GetDisplayName(MultiplexMatrix item)
        {
            // Use the localized text in the UI
            return Equals(item, MultiplexMatrix.NONE) ? Resources.SettingsList_ELEMENT_NONE_None : base.GetDisplayName(item);
        }
    }
}
