/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2013 University of Washington - Seattle, WA
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

using System.Collections;
using System.Linq;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class ReplicateList : SkylineObjectList<Replicate>
    {
        public ReplicateList(SkylineDataSchema dataSchema) : base(dataSchema)
        {
        }
        public override IEnumerable GetItems()
        {
            if (!DataSchema.Document.Settings.HasResults)
            {
                yield break;
            }

            var measuredResults = DataSchema.Document.Settings.MeasuredResults;
            var multiplexMatrix = DataSchema.Document.Settings.PeptideSettings.Quantification.MultiplexMatrix ?? MultiplexMatrix.NONE;
            for (int iReplicate = 0; iReplicate < measuredResults.Chromatograms.Count; iReplicate++)
            {
                if (multiplexMatrix?.Replicates.Count > 0)
                {
                    foreach (var multiplexName in multiplexMatrix.Replicates.Select(replicate => replicate.Name))
                    {
                        yield return new Replicate(DataSchema, iReplicate, multiplexName);
                    }
                }
                else
                {
                    yield return new Replicate(DataSchema, iReplicate, string.Empty);
                }
            }
        }
    }
}
