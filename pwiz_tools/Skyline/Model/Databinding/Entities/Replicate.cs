/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.AbsoluteQuantification;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    [AnnotationTarget(AnnotationDef.AnnotationTarget.replicate)]
    public class Replicate : RootSkylineObject, ILinkValue, IComparable, IReplicateValue
    {
        private static readonly ChromatogramSet EMPTY_CHROMATOGRAM_SET = (ChromatogramSet) new ChromatogramSet(
            XmlNamedElement.NAME_INTERNAL, new MsDataFileUri[0]).ChangeName(string.Empty);
        private readonly CachedValue<ChromatogramSet> _chromatogramSet;
        public Replicate(SkylineDataSchema dataSchema, int replicateIndex, string multiplexName = "") : base(dataSchema)
        {
            ReplicateIndex = replicateIndex;
            MultiplexName = multiplexName;
            _chromatogramSet = CachedValue.Create(DataSchema, FindChromatogramSet);
        }

        [Browsable(false)]
        public int ReplicateIndex { get; private set; }

        [Browsable(false)]
        public ChromatogramSet ChromatogramSet { get { return _chromatogramSet.Value; } }
        public void ChangeChromatogramSet(EditDescription editDescription, ChromatogramSet chromatogramSet)
        {
            ModifyDocument(editDescription.ChangeElementRef(GetElementRef()), document =>
                {
                    var measuredResults = document.Settings.MeasuredResults;
                    var chromatograms = measuredResults.Chromatograms.ToArray();
                    chromatograms[ReplicateIndex] = chromatogramSet;
                    measuredResults = measuredResults.ChangeChromatograms(chromatograms);
                    return document.ChangeMeasuredResults(measuredResults);
                }
            );
        }

        [InvariantDisplayName("ReplicateName")]
        public string Name
        {
            get { return ChromatogramSet.Name; }
            set
            {
                string newName = value ?? string.Empty;
                if (newName == Name)
                {
                    return;
                }
                if (SrmDocument.Settings.MeasuredResults.Chromatograms.Any(
                    chromatogramSet => newName == chromatogramSet.Name))
                {
                    throw new ArgumentException(string.Format(EntitiesResources.Replicate_Name_There_is_already_a_replicate_named___0___, newName));
                }
                ChangeChromatogramSet(EditColumnDescription(nameof(Name), newName),
                    (ChromatogramSet) ChromatogramSet.ChangeName(newName));
            }
        }

        public string MultiplexName
        {
            get; private set;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(MultiplexName))
            {
                return Name;
            }
            return TextUtil.ColonSeparate(Name, MultiplexName);
        }

        private ChromatogramSet FindChromatogramSet()
        {
            var results = SrmDocument.Settings.MeasuredResults;
            if (results == null || results.Chromatograms.Count <= ReplicateIndex)
            {
                return EMPTY_CHROMATOGRAM_SET;
            }
            return results.Chromatograms[ReplicateIndex];
        }

        public override object GetAnnotation(AnnotationDef annotationDef)
        {
            return DataSchema.AnnotationCalculator.GetAnnotation(annotationDef, this, ChromatogramSet.GetReplicateProperties(MultiplexName).Annotations);
        }

        public override void SetAnnotation(AnnotationDef annotationDef, object value)
        {
            var replicateProperties = ChromatogramSet.GetReplicateProperties(MultiplexName);
            replicateProperties =
                replicateProperties.ChangeAnnotations(
                    replicateProperties.Annotations.ChangeAnnotation(annotationDef, value));
            
            ChangeChromatogramSet(EditDescription.SetAnnotation(annotationDef, value), 
                ChromatogramSet.ChangeReplicateProperties(
                    MultiplexName, replicateProperties));
        }

        private void LinkValueOnClick(object sender, EventArgs args)
        {
            var skylineWindow = DataSchema.SkylineWindow;
            if (null == skylineWindow)
            {
                return;
            }
            skylineWindow.SelectedResultsIndex = ReplicateIndex;
        }
        object ILinkValue.Value { get { return this; } }
        EventHandler ILinkValue.ClickEventHandler { get { return LinkValueOnClick; } }
        public int CompareTo(object o)
        {
            if (null == o)
            {
                return 1;
            }
            return ReplicateIndex.CompareTo(((Replicate) o).ReplicateIndex);
        }

        [Obsolete]
        public string ReplicatePath { get { return @"/"; } }

        [HideWhen(AncestorOfType = typeof(ResultFile))]
        public IList<ResultFile> Files
        {
            get
            {
                return ChromatogramSet.MSDataFileInfos.Select(
                    chromFileInfo => new ResultFile(this, chromFileInfo.FileId, 0)).ToArray();
            }
        }

        [DataGridViewColumnType(typeof(SampleTypeDataGridViewColumn))]
        [Importable(Formatter = typeof(SampleType.PropertyFormatter))]
        public SampleType SampleType
        {
            get
            {
                return ChromatogramSet.GetReplicateProperties(MultiplexName).SampleType;
            }
            set
            {
                ChangeChromatogramSet(EditColumnDescription(nameof(SampleType), value),
                    ChromatogramSet.ChangeReplicateProperties(MultiplexName,
                        ChromatogramSet.GetReplicateProperties(MultiplexName).ChangeSampleType(value)));
            }
        }

        [Importable]
        public double? AnalyteConcentration
        {
            get { return ChromatogramSet.GetReplicateProperties(MultiplexName).AnalyteConcentration; }
            set
            {
                ChangeChromatogramSet(EditColumnDescription(nameof(AnalyteConcentration), value),
                    ChromatogramSet.ChangeReplicateProperties(MultiplexName,
                        ChromatogramSet.GetReplicateProperties(MultiplexName).ChangeAnalyteConcentration(value)));
            }
        }

        [Importable]
        public double SampleDilutionFactor
        {
            get { return ChromatogramSet.GetReplicateProperties(MultiplexName).SampleDilutionFactor; }
            set
            {
                ChangeChromatogramSet(EditColumnDescription(nameof(SampleDilutionFactor), value),
                    ChromatogramSet.ChangeReplicateProperties(MultiplexName,
                        ChromatogramSet.GetReplicateProperties(MultiplexName).ChangeAnalyteConcentration(value)));
            }
        }

        [Importable]
        public string BatchName
        {
            get { return ChromatogramSet.BatchName; }
            set
            {
                ChangeChromatogramSet(EditDescription.SetColumn(nameof(BatchName), value),
                    ChromatogramSet.ChangeBatchName(value));
            }
        }

        protected bool Equals(Replicate other)
        {
            return Equals(DataSchema, other.DataSchema) &&
                ReplicateIndex == other.ReplicateIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Replicate) obj);
        }

        public override int GetHashCode()
        {
            return DataSchema.GetHashCode() * 397 ^ ReplicateIndex;
        }

        [InvariantDisplayName("ReplicateLocator")]
        public string Locator { get { return GetLocator(); }}

        public override ElementRef GetElementRef()
        {
            return ReplicateRef.PROTOTYPE.ChangeName(ChromatogramSet.Name);
        }

        Replicate IReplicateValue.GetReplicate()
        {
            return this;
        }
    }

    public interface IReplicateValue
    {
        Replicate GetReplicate();
    }
}
