using System;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Model;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Menus
{
    public class ChromatogramTransitionMenuItems
    {
        public ToolStripMenuItem AllMenuItem { get; set; }
        public ToolStripMenuItem PrecursorsMenuItem { get; set; }
        public ToolStripMenuItem ProductsMenuItem { get; set; }
        public ToolStripMenuItem SingleMenuItem { get; set; }
        public ToolStripMenuItem TotalMenuItem { get; set; }
        public ToolStripSeparator GlobalChromatogramSeparator { get; set; }
        public ToolStripMenuItem TicMenuItem { get; set; }
        public ToolStripMenuItem BasePeakMenuItem { get; set; }
        public ToolStripMenuItem InjectionTimeMenuItem { get; set; }
        public ToolStripMenuItem QcMenuItem { get; set; }
        public ToolStripMenuItem OnlyQuantitativeMenuItem { get; set; }
        public ToolStripMenuItem SplitGraphMenuItem { get; set; }
        public EventHandler QcMenuItem_Click { get; set; }

        public void UpdateMenuItems(SrmDocument document, PeptideDocNode peptideDocNode)
        {
            var displayType = GraphChromatogram.DisplayType;
            // If both MS1 and MS/MS ions are not possible, then menu items to differentiate precursors and
            // products are not necessary.
            bool showIonTypeOptions = true == peptideDocNode?.TransitionGroups.Any(tg=>GraphChromatogram.IsMultipleIonSources(document.Settings.TransitionSettings.FullScan, tg));
            PrecursorsMenuItem.Visible =
                ProductsMenuItem.Visible = showIonTypeOptions;

            if (!showIonTypeOptions &&
                (displayType == DisplayTypeChrom.precursors || displayType == DisplayTypeChrom.products))
                displayType = DisplayTypeChrom.all;

            // Only show all ions chromatogram options when at least one chromatogram of this type exists
            bool anyGlobalChromatograms = false;
            var measuredResults = document.Settings.MeasuredResults;
            foreach (var tuple in new[]
                     {
                         Tuple.Create(BasePeakMenuItem, DisplayTypeChrom.base_peak,
                             measuredResults?.HasBasePeakChromatogram),
                         Tuple.Create(TicMenuItem, DisplayTypeChrom.tic, measuredResults?.HasTicChromatogram),
                         Tuple.Create(InjectionTimeMenuItem, DisplayTypeChrom.injection_time,
                             measuredResults?.HasInjectionTime)
                     })
            {
                var menuItem = tuple.Item1;
                var menuItemDisplayType = tuple.Item2;
                if (true == tuple.Item3)
                {
                    anyGlobalChromatograms = true;
                    menuItem.Visible = true;
                    menuItem.Checked = menuItemDisplayType == displayType;
                }
                else
                {
                    menuItem.Visible = false;
                    if (menuItemDisplayType == displayType)
                    {
                        displayType = DisplayTypeChrom.all;
                    }
                }
            }

            QcMenuItem.DropDownItems.Clear();
            var qcTraceNames = (measuredResults?.QcTraceNames ?? Enumerable.Empty<string>()).ToList();
            if (qcTraceNames.Count > 0)
            {
                anyGlobalChromatograms = true;
                var qcContextTraceItems = new ToolStripItem[qcTraceNames.Count];
                for (int i = 0; i < qcTraceNames.Count; i++)
                {
                    qcContextTraceItems[i] = new ToolStripMenuItem(qcTraceNames[i], null, QcMenuItem_Click)
                    {
                        Checked = displayType == DisplayTypeChrom.qc &&
                                  Settings.Default.ShowQcTraceName == qcTraceNames[i]
                    };
                }

                QcMenuItem.DropDownItems.AddRange(qcContextTraceItems);
            }
            else
            {
                QcMenuItem.Visible = false;
            }
            GlobalChromatogramSeparator.Visible = anyGlobalChromatograms;

            AllMenuItem.Checked = (displayType == DisplayTypeChrom.all);
            PrecursorsMenuItem.Checked = (displayType == DisplayTypeChrom.precursors);
            ProductsMenuItem.Checked = (displayType == DisplayTypeChrom.products);
            SingleMenuItem.Checked = (displayType == DisplayTypeChrom.single);
            TotalMenuItem.Checked = (displayType == DisplayTypeChrom.total);
            SplitGraphMenuItem.Checked = Settings.Default.SplitChromatogramGraph;
            OnlyQuantitativeMenuItem.Checked = Settings.Default.ShowQuantitativeOnly;
        }
    }
}
