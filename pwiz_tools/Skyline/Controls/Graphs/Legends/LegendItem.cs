using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace pwiz.Skyline.Controls.Graphs.Legends
{
    public class LegendItem : Control
    {
        public string Label { get; set; }
        public Symbol Symbol { get; set; }
        public Line Line { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            int xMid = ClientRectangle.Left + ClientRectangle.Right / 2;
            int yMid = ClientRectangle.Top + ClientRectangle.Bottom / 2;
            var g = e.Graphics;
            if (Line != null)
            {
                Line.Fill.Draw(g, ClientRectangle);
                var pen = new Pen(Line.Color, Line.Width);
                pen.DashStyle = Line.Style;
                g.DrawLine(pen, ClientRectangle.Left, yMid, ClientRectangle.Right, yMid);
            }

            if (Symbol != null)
            {

            }
        }
    }
}
