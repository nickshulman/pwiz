/*
 * Original author: Max Horowitz-Gelb <maxhg .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2016 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Linq;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Model.Results.Scoring.Tric
{
    class TricStarTree : TricTree
    {
        public TricStarTree(IEnumerable<PeptideFileFeatureSet> peptides,
                            ChromFileInfoIndex fileIndex,
                            double testAnchorCutoff,
                            RegressionMethodRT regressionMethod,
                            IProgressMonitor progressMonitor,
                            ref IProgressStatus status, bool verbose = false)
            : base(peptides, fileIndex, testAnchorCutoff, regressionMethod, progressMonitor, ref status,verbose)
        {
        }

        protected override void LearnTree(IProgressMonitor progressMonitor, ref IProgressStatus status, int percentRange)
        {
            LearnStarTree(progressMonitor, ref status);
        }

        protected override void CalculateEdgeWeights(IProgressMonitor pm, ref IProgressStatus status, int percentRange)
        {
            //Do nothing
        }

        protected override void ScoreFiles()
        {
            foreach (var statsGrouping in _peptides)
            {
                var first = statsGrouping.FileFeatures.Values.FirstOrDefault();
                if(first == null|| first.Features.IsDecoy)
                    continue;
                foreach (var stat in statsGrouping.FileFeatures)
                {
                    var fileIndex = stat.Key;
                    if (stat.Value.FeatureStats.QValue.HasValue && stat.Value.FeatureStats.QValue < _anchorCutoff)
                        _vertices[fileIndex].Score ++;
                }
            }
        }

        private void LearnStarTree(IProgressMonitor progressMonitor, ref IProgressStatus status)
        {
            ChromFileInfoId maxScoreFileIndex = null;
            double maxScore = double.MinValue;
            foreach (var fileId in _fileIndex.FileIds)
            {
                if (_vertices[fileId].Score > maxScore)
                {
                    maxScore = _vertices[fileId].Score;
                    maxScoreFileIndex = fileId;
                }
            }
            _tree = new List<Edge>();

            foreach(var vertex in _vertices.Values)
            {
                if(!ReferenceEquals(vertex.FileId, maxScoreFileIndex))
                {
                    _tree.Add(new Edge(_vertices[maxScoreFileIndex],vertex,0));    
                }
            }

            foreach (var edge in _tree)
            {
                edge.AVertex.NeighborRuns.Add(edge.BVertex.FileId);
                edge.BVertex.NeighborRuns.Add(edge.AVertex.FileId);
                edge.AVertex.Connections.Add(edge);
                edge.BVertex.Connections.Add(edge);
            }
        }
    }
}
