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
        private bool _inUpdateControls;
        private List<CurveSettings> _curves = new List<CurveSettings>{new CurveSettings()};
        private RunAlignmentProperties _runAlignmentProperties;
        private Cache _cache = new Cache();
        public RunAlignmentForm(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            SkylineWindow = skylineWindow;
            IfNotUpdating(UpdateComboCurves);
            _runAlignmentProperties = new RunAlignmentProperties(skylineWindow);
            propertyGrid.SelectedObject = _runAlignmentProperties;
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

        private void IfNotUpdating(Action action)
        {
            if (!_inUpdateControls)
            {
                try
                {
                    _inUpdateControls = true;
                    action();
                }
                finally
                {
                    _inUpdateControls = false;
                }
            }
        }

        public void UpdateUI()
        {
            try
            {
                UpdateGraph(_cache);
                propertyGrid.Refresh();
            }
            finally
            {
                _cache.DumpStaleObjects();
            }
        }

        private void UpdateGraph(Cache cache)
        {
            zedGraphControl1.GraphPane.CurveList.Clear();
            var document = SkylineWindow.Document;
            var dataX = cache.GetValue(Tuple.Create(document, _runAlignmentProperties.XAxis),
                () => GetRetentionTimeData(document, _runAlignmentProperties.XAxis));
            if (dataX != null)
            {
                foreach (var curveSettings in _curves)
                {
                    var dataY = cache.GetValue(Tuple.Create(document, curveSettings.YAxis),
                        () => GetRetentionTimeData(document, curveSettings.YAxis));
                    if (dataY == null)
                    {
                        continue;
                    }

                    var similarityMatrixKey = Tuple.Create(dataX, dataY);
                    if (!cache.TryGetValue(similarityMatrixKey, out SimilarityMatrix similarityMatrix))
                    {
                        using var longWaitDlg = new LongWaitDlg();
                        longWaitDlg.PerformWork(this, 1000, progressMonitor =>
                        {
                            var progressStatus = new ProgressStatus("Computing Similarity Matrix");
                            similarityMatrix =
                                dataX.Spectra.GetSimilarityMatrix(progressMonitor, progressStatus, dataY.Spectra);
                            cache.AddValue(similarityMatrixKey, similarityMatrix);
                        });
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
            IfNotUpdating(() =>
            {
                _curves[comboCurves.SelectedIndex] = _runAlignmentProperties.CurveSettings;
                UpdateComboCurves();
                UpdateUI();
            });
        }

        private void comboCurves_SelectedIndexChanged(object sender, EventArgs e)
        {
            IfNotUpdating(() =>
            {
                if (comboCurves.SelectedIndex >= 0)
                {
                    _runAlignmentProperties.CurveSettings = _curves[comboCurves.SelectedIndex];
                    UpdateUI();
                }
            });
        }

        private void toolButtonAddCurve_Click(object sender, EventArgs e)
        {
            IfNotUpdating(() =>
            {
                _curves.Add(CurveSettings.Default);
                UpdateComboCurves();
                comboCurves.SelectedIndex = _curves.Count - 1;
                _runAlignmentProperties.CurveSettings = _curves[comboCurves.SelectedIndex];
                UpdateUI();
            });
        }

        private void toolButtonDelete_Click(object sender, EventArgs e)
        {
            IfNotUpdating(() =>
            {
                if (comboCurves.SelectedIndex > 0 && comboCurves.SelectedIndex < _curves.Count)
                {
                    _curves.RemoveAt(comboCurves.SelectedIndex);
                    if (_curves.Count == 0)
                    {
                        _curves.Add(new CurveSettings());
                    }
                }

                UpdateComboCurves();
                _runAlignmentProperties.CurveSettings = _curves[comboCurves.SelectedIndex];
                UpdateUI();

            });
        }

        private void UpdateComboCurves()
        {
            Assume.IsTrue(_inUpdateControls);
            if (_curves.Count == 0)
            {
                _curves.Add(new CurveSettings());
            }

            int newSelectedIndex = Math.Max(0, Math.Min(_curves.Count - 1, comboCurves.SelectedIndex));
            comboCurves.Items.Clear();
            comboCurves.Items.AddRange(_curves.Select(curve=>curve.ToString()).ToArray());
            comboCurves.SelectedIndex = newSelectedIndex;
        }
    }
}
