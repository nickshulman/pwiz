using System;
using System.Collections.Generic;
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
        private List<CurveSettings> _curveList;
        private CurveSettings _currentCurveSettings;
        private Cache _cache = new Cache();
        public RunAlignmentForm(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            SkylineWindow = skylineWindow;
            _currentCurveSettings = new CurveSettings(skylineWindow);
            propertyGrid.SelectedObject = _currentCurveSettings;
        }

        public SkylineWindow SkylineWindow { get; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SkylineWindow.DocumentUIChangedEvent += SkylineWindowOnDocumentUIChangedEvent;
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
                UpdateGraph(_cache);
            }
            finally
            {
                _cache.DumpStaleObjects();
                _inUpdate = false;
            }
        }

        private void UpdateGraph(Cache cache)
        {
            zedGraphControl1.GraphPane.CurveList.Clear();
            var document = SkylineWindow.Document;
            var dataX = cache.GetValue(Tuple.Create(document, _currentCurveSettings.XAxis),
                () => GetRetentionTimeData(document, _currentCurveSettings.XAxis));
            if (dataX != null)
            {
                var curveSettings = _currentCurveSettings;
                var dataY = cache.GetValue(Tuple.Create(document, _currentCurveSettings.YAxis),
                    ()=> GetRetentionTimeData(document, _currentCurveSettings.YAxis));

                if (dataY != null)
                {
                    var similarityMatrixKey = Tuple.Create(dataX, dataY);
                    if (!cache.TryGetValue(similarityMatrixKey, out SimilarityMatrix similarityMatrix))
                    {
                        using (var longWaitDlg = new LongWaitDlg())
                        {
                            longWaitDlg.PerformWork(this, 1000, progressMonitor =>
                            {
                                var progressStatus = new ProgressStatus("Computing Similarity Matrix");
                                similarityMatrix =
                                    dataX.Spectra.GetSimilarityMatrix(progressMonitor, progressStatus, dataY.Spectra);
                                cache.AddValue(similarityMatrixKey, similarityMatrix);
                            });
                        }
                    }

                    if (similarityMatrix != null)
                    {
                        var bestPath = new PointPairList(similarityMatrix.FindBestPath(false).ToList());
                        if (curveSettings.RegressionMethod.HasValue && curveSettings.LineDashStyle.HasValue)
                        {
                            var lineItem = PerformKdeAlignment(bestPath);
                            lineItem.Line.Style = curveSettings.LineDashStyle.Value;
                            lineItem.Line.Color = curveSettings.LineColor;
                            zedGraphControl1.GraphPane.CurveList.Add(lineItem);
                        }

                        zedGraphControl1.GraphPane.CurveList.Add(
                            new LineItem(null, bestPath, curveSettings.SymbolColor, curveSettings.SymbolType)
                            {
                                Line =
                                {
                                    IsVisible = false
                                }
                            });
                    }
                }

                if (!string.IsNullOrEmpty(curveSettings.Caption))
                {
                    var legendItem = new LineItem(curveSettings.Caption)
                    {
                        Symbol = new Symbol(curveSettings.SymbolType, curveSettings.SymbolColor),
                    };
                    if (curveSettings.LineDashStyle.HasValue)
                    {
                        legendItem.Line.Color = curveSettings.LineColor;
                        legendItem.Line.Style = curveSettings.LineDashStyle.Value;
                    }
                    else
                    {
                        legendItem.Line.IsVisible = false;
                    }
                    zedGraphControl1.GraphPane.CurveList.Add(legendItem);
                }
            }

            zedGraphControl1.GraphPane.AxisChange();
            zedGraphControl1.Invalidate();
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


        public RetentionTimeData GetRetentionTimeData(SrmDocument document, RetentionTimeSource retentionTimeSource)
        {
            if (retentionTimeSource == null)
            {
                return null;
            }

            
            SpectrumMetadataList spectrumMetadataList = null;
            if (retentionTimeSource.MsDataFileUri != null)
            {
                var resultFileMetadata =
                    document.Settings.MeasuredResults?.GetResultFileMetaData(retentionTimeSource.MsDataFileUri);
                if (resultFileMetadata != null)
                {
                    spectrumMetadataList = new SpectrumMetadataList(resultFileMetadata.SpectrumMetadatas,
                        ImmutableList.Empty<SpectrumClassColumn>());
                    spectrumMetadataList = ReduceMetadataList(spectrumMetadataList);
                }
            }

            return new RetentionTimeData(Array.Empty<MeasuredRetentionTime>(), spectrumMetadataList, null);
        }

        private SpectrumMetadataList ReduceMetadataList(SpectrumMetadataList metadataList)
        {
            int? msLevel = null;
            var spectra = new List<DigestedSpectrumMetadata>();
            int? digestLength = 16;
            bool halfPrecision = true;
            foreach (var spectrum in metadataList.AllSpectra)
            {
                if (msLevel.HasValue && msLevel != spectrum.SpectrumMetadata.MsLevel)
                {
                    continue;
                }

                SpectrumDigest digest = spectrum.Digest;
                if (digestLength.HasValue)
                {
                    digest = digest.ShortenTo(digestLength.Value);
                }

                if (halfPrecision)
                {
                    digest = new SpectrumDigest(digest.Select(v => (double)((HalfPrecisionFloat)v)));
                }
                spectra.Add(new DigestedSpectrumMetadata(spectrum.SpectrumMetadata, digest));
            }

            return new SpectrumMetadataList(spectra, metadataList.Columns);
        }

        private void propertyGrid_PropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e)
        {
            UpdateUI();
        }
    }
}
