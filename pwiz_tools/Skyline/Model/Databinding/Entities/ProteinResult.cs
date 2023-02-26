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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NHibernate.Util;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    [ProteomicDisplayName(nameof(ProteinResult))]
    [InvariantDisplayName("MoleculeListResult")]
    public class ProteinResult : SkylineObject, ILinkValue, IErrorTextProvider
    {
        public ProteinResult(Protein protein, Replicate replicate)
        {
            Replicate = replicate;
            Protein = protein;
        }

        protected override SkylineDataSchema GetDataSchema()
        {
            return Protein.DataSchema;
        }

        public override string ToString()
        {
            return TextUtil.SpaceSeparate(Protein.ToString(), Replicate.ToString());
        }

        [Browsable(false)]
        public Protein Protein
        {
            get;
            private set;
        }

        [HideWhen(AncestorOfType = typeof(Protein))]
        public Replicate Replicate { get; private set; }

        [InvariantDisplayName("MoleculeListAbundance")]
        [ProteomicDisplayName("ProteinAbundance")]
        [Format(Formats.GLOBAL_STANDARD_RATIO, NullValue = TextUtil.EXCEL_NA)]
        public double? Abundance
        {
            get
            {
                var abundanceValue = GetAbundanceValue();
                if (abundanceValue == null || abundanceValue.MissingValues.Any())
                {
                    return null;
                }
                return abundanceValue.Abundance;
            }
        }

        private Protein.AbundanceValue GetAbundanceValue()
        {
            Protein.GetProteinAbundances().TryGetValue(Replicate.ReplicateIndex, out var abundanceValue);
            return abundanceValue;
        }

        EventHandler ILinkValue.ClickEventHandler
        {
            get { return LinkValueOnClick; }
        }
        object ILinkValue.Value { get { return this; } }
        public void LinkValueOnClick(object sender, EventArgs args)
        {
            var skylineWindow = DataSchema.SkylineWindow;
            if (null == skylineWindow)
            {
                return;
            }

            skylineWindow.SelectedPath = Protein.IdentityPath;
            skylineWindow.SelectedResultsIndex = Replicate.ReplicateIndex;
        }

        public string GetErrorText(string columnName)
        {
            if (columnName == nameof(Abundance))
            {
                var abundanceValue = GetAbundanceValue();
                if (abundanceValue == null || !abundanceValue.MissingValues.Any())
                {
                    return null;
                }

                var errorTextLines = new List<string>{"Missing values for the following:"};
                foreach (var missingValue in abundanceValue.MissingValues)
                {
                    errorTextLines.Add(GetDescription(missingValue));
                }

                return TextUtil.LineSeparate(errorTextLines);
            }

            return null;
        }

        private string GetDescription(IdentityPath identityPath)
        {
            if (identityPath.Length == (int)SrmDocument.Level.Transitions)
            {
                var transition = new Transition(DataSchema, identityPath);
                return TextUtil.SpaceSeparate(transition.Precursor.Peptide.ToString(), transition.Precursor.ToString(),
                    transition.ToString());
            }

            return identityPath.ToString();
        }
    }
}
