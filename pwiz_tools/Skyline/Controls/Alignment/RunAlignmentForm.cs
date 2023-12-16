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
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;
using ZedGraph;

namespace pwiz.Skyline.Controls.Alignment
{
    public partial class RunAlignmentForm : DockableFormEx
    {
        private bool _inUpdate;
        public RunAlignmentForm(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            SkylineWindow = skylineWindow;
            for (int i = 1; i < 8; i++)
            {
                comboSignatureLength.Items.Add(1 << i);
            }
            comboSignatureLength.SelectedIndex = 0;
            comboMsLevel.Items.Add("");
            comboMsLevel.Items.Add(1);
            comboMsLevel.Items.Add(2);
        }

        public SkylineWindow SkylineWindow { get; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SkylineWindow.DocumentUIChangedEvent += SkylineWindowOnDocumentUIChangedEvent;
            UpdateGraph();
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


        private void combo_SelectedIndexChanged(object sender, EventArgs e)
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
            var document = SkylineWindow.DocumentUI;
            var fileItems = new List<FileItem>();
            if (document.Settings.HasResults)
            {
                fileItems.AddRange(document.Settings.MeasuredResults.Chromatograms
                    .SelectMany(chromatogramSet => chromatogramSet.MSDataFilePaths).Distinct()
                    .Select(path => new FileItem(path)));
            }
            ComboHelper.ReplaceItems(comboXAxis, fileItems);
            ComboHelper.ReplaceItems(comboYAxis, fileItems);
            if (comboXAxis.SelectedItem is FileItem xFileItem && comboYAxis.SelectedItem is FileItem yFileItem)
            {
                var fileMetaData1 = document.Settings.MeasuredResults?.GetResultFileMetaData(xFileItem.MsDataFilePath);
                var fileMetaData2 = document.Settings.MeasuredResults?.GetResultFileMetaData(yFileItem.MsDataFilePath);
                if (fileMetaData1 != null && fileMetaData2 != null)
                {
                    int? msLevel = comboMsLevel.SelectedItem as int?;
                    var metadatas1 = FilterMetadatas(fileMetaData1.SpectrumMetadatas, msLevel);
                    var metadatas2 = FilterMetadatas(fileMetaData2.SpectrumMetadatas, msLevel);
                    PointPairList pointPairList = null;
                    using (var longWaitDlg = new LongWaitDlg())
                    {
                        int digestLength = comboSignatureLength.SelectedItem as int? ?? 2;
                        longWaitDlg.PerformWork(this, 1000, broker =>
                        {
                            pointPairList = GetPointPairList(broker, digestLength, metadatas1,
                                metadatas2);
                        });
                    }
                    zedGraphControl1.GraphPane.CurveList.Add(new ClusteredHeatMapItem("Graph", pointPairList));
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

        private static PointPairList GetPointPairList(ILongWaitBroker longWaitBroker, int digestLength, IList<DigestedSpectrumMetadata> xList,
            IList<DigestedSpectrumMetadata> yList)
        {
            var lists = new List<PointPair>[xList.Count];
            var yDigests = yList.Select(metadata => TruncateDigest(metadata.Digest, digestLength)).ToList();
            int completedCount = 0;
            ParallelEx.For(0, xList.Count, x =>
            {
                var xMetadata = xList[x].SpectrumMetadata;
                var xDigest = TruncateDigest(xList[x].Digest, digestLength);
                var list = new List<PointPair>();
                for (int y = 0; y < yList.Count; y++)
                {
                    longWaitBroker.CancellationToken.ThrowIfCancellationRequested();
                    if (!AreCompatible(xMetadata, yList[y].SpectrumMetadata))
                    {
                        continue;
                    }
                    var similarity = DotProduct(xDigest, yDigests[y]);
                    if (similarity != null)
                    {
                        list.Add(new PointPair(x, y) {Tag = GetColor(similarity.Value)});
                    }
                }
                lists[x] = list;
                Interlocked.Increment(ref completedCount);
                longWaitBroker.ProgressValue = completedCount * 100 / xList.Count;
            });
            var pointPairList = new PointPairList();
            pointPairList.AddRange(lists.SelectMany(list=>list));
            return pointPairList;
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

        private struct FileItem
        {
            public FileItem(MsDataFileUri msDataFilePath)
            {
                MsDataFilePath = msDataFilePath;
            }

            public MsDataFileUri MsDataFilePath { get; }

            public override string ToString()
            {
                return MsDataFilePath.GetFileName();
            }
        }

        private static IList<float> TruncateDigest(IList<float> digest, int length)
        {
            if (digest.Count <= length)
            {
                return digest;
            }

            IList<double> vector = digest.Select(value => (double)value).ToList();
            while (vector.Count > length)
            {
                vector = DigestedSpectrumMetadata.DigestVector(vector);
            }

            return ImmutableList.ValueOf(vector.Select(v => (float)v));
        }
    }
}
