using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using pwiz.Common.Collections;
using pwiz.Common.Spectra;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls.Clustering;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Alignment;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;
using ZedGraph;

namespace pwiz.Skyline.Controls.Alignment
{
    public partial class RunAlignmentForm : DockableFormEx
    {
        private bool _inUpdate;
        private BindingList<CurveRow> _rows = new BindingList<CurveRow>();
        public RunAlignmentForm(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            SkylineWindow = skylineWindow;
            for (int i = 1; i < 13; i++)
            {
                comboSignatureLength.Items.Add(1 << i);
            }
            comboSignatureLength.SelectedIndex = 0;
            comboMsLevel.Items.Add("");
            comboMsLevel.Items.Add(1);
            comboMsLevel.Items.Add(2);
            bindingSource1.DataSource = _rows;
            bindingSource1.ListChanged += BindingSource1_ListChanged;
        }

        private void BindingSource1_ListChanged(object sender, ListChangedEventArgs e)
        {
            UpdateGraph();
        }

        public SkylineWindow SkylineWindow { get; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SkylineWindow.DocumentUIChangedEvent += SkylineWindowOnDocumentUIChangedEvent;
            UpdateControls();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            SkylineWindow.DocumentUIChangedEvent -= SkylineWindowOnDocumentUIChangedEvent;
            base.OnHandleDestroyed(e);
        }


        private void SkylineWindowOnDocumentUIChangedEvent(object sender, DocumentChangedEventArgs e)
        {
            UpdateUI();
        }


        private void OnValuesChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (_inUpdate)
            {
                return;
            }

            try
            {
                _inUpdate = true;
                UpdateGraph();
            }
            finally
            {
                _inUpdate = false;
            }
        }

        private void UpdateGraph()
        {
            zedGraphControl1.GraphPane.CurveList.Clear();
            if (comboXAxis.SelectedItem is RetentionTimeSource xSource)
            {
                var dataX = GetRetentionTimeData(SkylineWindow.Document, xSource);
                if (dataX != null)
                {
                    foreach (var curve in _rows)
                    {
                        var dataY = curve.RetentionTimeData;
                        if (dataY != null)
                        {

                        }
                    }
                }
            }
            if (comboXAxis.SelectedItem is RetentionTimeSource xFileItem && comboYAxis.SelectedItem is RetentionTimeSource yFileItem)
            {
                var dataX = GetRetentionTimeData(document, xFileItem);
                var dataY = GetRetentionTimeData(document, yFileItem);
                if (dataX != null && dataY != null)
                {
                    SimilarityMatrix similarityMatrix = null;
                    using (var longWaitDlg = new LongWaitDlg())
                    {
                        int digestLength = comboSignatureLength.SelectedItem as int? ?? 2;
                        bool halfPrecision = cbxHalfPrecision.Checked;
                        longWaitDlg.PerformWork(this, 1000, progressMonitor =>
                        {
                            var progressStatus = new ProgressStatus("Computing Similarity Matrix");
                            similarityMatrix = dataX.Spectra.GetSimilarityMatrix(progressMonitor, progressStatus, dataY.Spectra);
                        });
                    }

                    if (similarityMatrix != null)
                    {
                        if (cbxSparseBestPath.Checked)
                        {
                            var bestPath = new PointPairList(similarityMatrix.FindBestPath(true).ToList());
                            
                            if (cbxKdeAlignment.Checked)
                            {
                                zedGraphControl1.GraphPane.CurveList.Add(PerformKdeAlignment(bestPath));
                            }

                            zedGraphControl1.GraphPane.CurveList.Add(
                                new LineItem("Sparse Best Path", bestPath, Color.Black, SymbolType.Triangle)
                                {
                                    Line =
                                    {
                                        IsVisible = false
                                    }
                                });
                        }
                        if (cbxDenseBestPath.Checked)
                        {
                            var bestPath = new PointPairList(similarityMatrix.FindBestPath(false).ToList());
                            if (cbxKdeAlignment.Checked)
                            {
                                zedGraphControl1.GraphPane.CurveList.Add(PerformKdeAlignment(bestPath));
                            }
                            zedGraphControl1.GraphPane.CurveList.Add(
                                new LineItem("Dense Best Path", bestPath, Color.Black, SymbolType.Circle)
                                {
                                    Line =
                                    {
                                        IsVisible = false
                                    }
                                });
                        }

                        if (cbxSimilarityMatrix.Checked)
                        {
                            zedGraphControl1.GraphPane.CurveList.Add(MakeSimilarityMatrix(similarityMatrix));
                        }
                    }
                }

                zedGraphControl1.GraphPane.XAxis.Title.Text = xFileItem.ToString();
                zedGraphControl1.GraphPane.YAxis.Title.Text = yFileItem.ToString();
            }


            zedGraphControl1.GraphPane.AxisChange();
            zedGraphControl1.Invalidate();
        }

        public void UpdateControls()
        {
            var document = SkylineWindow.DocumentUI;
            var fileItems = new List<RetentionTimeSource>();
            if (document.Settings.HasResults)
            {
                fileItems.AddRange(document.Settings.MeasuredResults.Chromatograms
                    .SelectMany(chromatogramSet => chromatogramSet.MSDataFilePaths).Distinct()
                    .Select(path => new RetentionTimeSource(path.GetFileName(), path, null)));
            }
            ComboHelper.ReplaceItems(comboXAxis, fileItems);
            ComboHelper.ReplaceItems(comboYAxis, fileItems);
        }

        private IList<DigestedSpectrumMetadata> FilterMetadatas(IList<DigestedSpectrumMetadata> metadatas, int? msLevel)
        {
            if (!msLevel.HasValue)
            {
                return metadatas;
            }

            return metadatas.Where(metadata => metadata.SpectrumMetadata.MsLevel == msLevel).ToList();
        }

        private LineItem PerformKdeAlignment(IList<PointPair> pointPairList)
        {
            var kdeAligner = new KdeAligner();
            kdeAligner.Train(pointPairList.Select(point=>point.X).ToArray(), pointPairList.Select(point=>point.Y).ToArray(), CancellationToken.None);
            kdeAligner.GetSmoothedValues(out var xArr, out var yArr);
            return new LineItem(null, new PointPairList(xArr, yArr), Color.Black, SymbolType.None);
        }

        private ClusteredHeatMapItem MakeSimilarityMatrix(SimilarityMatrix similarityMatrix)
        {
            var pointPairList = new PointPairList();
            pointPairList.AddRange(similarityMatrix.Points.Select(point=>new PointPair(point.X, point.Y) {Tag = GetColor(point.Z)}));
            return new ClusteredHeatMapItem("Similarity Matrix", pointPairList);
        }

        private static Color GetColor(double similarity)
        {
            var value = (int) (127 + similarity * 128);
            return Color.FromArgb(value, value, value);
        }


        public static bool AreCompatible(SpectrumMetadata spectrum1, SpectrumMetadata spectrum2)
        {
            return Equals(spectrum1.GetPrecursors(0), spectrum2.GetPrecursors(0));
        }

        public static double? DotProduct(IList<float> x, IList<float> y)
        {
            if (x.Count != y.Count)
            {
                return null;
            }
            double sumX2 = 0;
            double sumY2 = 0;
            double sumXY = 0;
            for (int i = 0; i < x.Count; i++)
            {
                sumX2 += x[i] * x[i];
                sumY2 += y[i] * y[i];
                sumXY += x[i] * y[i];
            }

            if (sumX2 == 0 || sumY2 == 0)
            {
                return null;
            }

            return sumXY / Math.Sqrt(sumX2 * sumY2);
        }

        public struct RetentionTimeSource
        {
            public RetentionTimeSource(string name, MsDataFileUri msDataFileUri, string filename)
            {
                Name = name;
                MsDataFileUri = msDataFileUri;
                Filename = filename;
            }

            public string Name { get; }
            public override string ToString()
            {
                return Name;
            }

            public MsDataFileUri MsDataFileUri
            {
                get;
            }

            public string Filename { get; }
        }

        public RetentionTimeData GetRetentionTimeData(SrmDocument document, RetentionTimeSource retentionTimeSource)
        {
            SpectrumMetadataList spectrumMetadataList = null;
            if (retentionTimeSource.MsDataFileUri != null)
            {
                var resultFileMetadata =
                    document.Settings.MeasuredResults?.GetResultFileMetaData(retentionTimeSource.MsDataFileUri);
                if (resultFileMetadata != null)
                {
                    spectrumMetadataList = new SpectrumMetadataList(resultFileMetadata.SpectrumMetadatas,
                        ImmutableList.Empty<SpectrumClassColumn>());
                }
            }

            return new RetentionTimeData(Array.Empty<MeasuredRetentionTime>(), spectrumMetadataList, null);
        }

        public class CurveRow
        {
            public CurveRow(RetentionTimeData retentionTimeData)
            {
                RetentionTimeData = retentionTimeData;
            }

            public RetentionTimeData RetentionTimeData { get; }
            public string Caption { get; set; }
            public string Description { get; set; }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (comboYAxis.SelectedItem is RetentionTimeSource source)
            {
                _rows.Add(new CurveRow(GetRetentionTimeData(SkylineWindow.Document, source)));
            }
            
        }
    }
}
