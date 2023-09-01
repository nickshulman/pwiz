using System.Drawing;
using System.Linq;
using ZedGraph;

namespace pwiz.Skyline.Controls.Graphs.Legends
{
    public class LegendPane : GraphPane
    {
        public override void Draw(Graphics g)
        {
            if (_rect.Width < 1 || _rect.Height < 1)
            {
                return;
            }
            var visibleCurves = CurveList.Where(curve => !string.IsNullOrEmpty(curve.Label.Text) && curve.IsVisible)
                .ToList();
            if (visibleCurves.Count == 0)
            {
                return;
            }

            for (int iCurve = 0; iCurve < visibleCurves.Count; iCurve++)
            {
                var curve = visibleCurves[iCurve];
                var top = _rect.Top + _rect.Height * iCurve / visibleCurves.Count;
                var height = _rect.Height / visibleCurves.Count;
                var rectSymbol = new RectangleF(_rect.Left, top, _rect.Width / 2, height);
                curve.DrawLegendKey(g, this, rectSymbol, 1);
                var rectLabel = new RectangleF(_rect.Left + _rect.Width / 2, top, _rect.Width / 2, height);
                curve.Label.FontSpec.Draw(g, this, curve.Label.Text, rectLabel.X, rectLabel.Y, AlignH.Left, AlignV.Center, 1);
            }
        }
    }
}
