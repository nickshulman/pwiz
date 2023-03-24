﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2015 University of Washington - Seattle, WA
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
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.DocSettings.AbsoluteQuantification
{
    /// <summary>
    /// Options for the display of the calibration curve window, which get persisted in Settings.
    /// </summary>
    [XmlRoot("calibration_curve")]
    public class CalibrationCurveOptions : Immutable, IXmlSerializable
    {
        public static readonly CalibrationCurveOptions DEFAULT = new CalibrationCurveOptions()
        {
            DisplaySampleTypes = ImmutableList.ValueOf(
                new[] { SampleType.STANDARD, SampleType.BLANK, SampleType.QC, SampleType.UNKNOWN}),
            ShowLegend = true,
            ShowSelection = true,
            ShowFiguresOfMerit = true
            
        };
        public bool LogXAxis { get; private set; }

        public CalibrationCurveOptions ChangeLogXAxis(bool value)
        {
            return ChangeProp(ImClone(this), im => im.LogXAxis = value);
        }

        public bool LogYAxis { get; private set; }

        public CalibrationCurveOptions ChangeLogYAxis(bool value)
        {
            return ChangeProp(ImClone(this), im => im.LogYAxis = value);
        }

        public ImmutableList<SampleType> DisplaySampleTypes { get; private set; }

        public CalibrationCurveOptions ChangeDisplaySampleTypes(IEnumerable<SampleType> value)
        {
            return ChangeProp(ImClone(this), im => im.DisplaySampleTypes = ImmutableList.ValueOf(value));
        }
        public bool SingleBatch { get; private set; }

        public CalibrationCurveOptions ChangeSingleBatch(bool value)
        {
            return ChangeProp(ImClone(this), im => im.SingleBatch = value);
        }

        public bool DisplaySampleType(SampleType sampleType)
        {
            return DisplaySampleTypes.Contains(sampleType);
        }

        public bool ShowLegend { get; private set; }

        public CalibrationCurveOptions ChangeShowLegend(bool value)
        {
            return ChangeProp(ImClone(this), im => im.ShowLegend = value);
        }
        public bool ShowSelection { get; private set; }

        public CalibrationCurveOptions ChangeShowSelection(bool value)
        {
            return ChangeProp(ImClone(this), im => im.ShowSelection = value);
        }
        public bool ShowFiguresOfMerit { get; private set; }

        public CalibrationCurveOptions ChangeShowFiguresOfMerit(bool value)
        {
            return ChangeProp(ImClone(this), im => im.ShowFiguresOfMerit = value);
        }

        public CalibrationCurveOptions SetDisplaySampleType(SampleType sampleType, bool display)
        {
            if (display)
            {
                return ChangeDisplaySampleTypes(DisplaySampleTypes.Concat(new[] { sampleType }).Distinct()
                    .ToArray());
            }
            else
            {
                return ChangeDisplaySampleTypes(DisplaySampleTypes.Except(new[] {sampleType}).ToArray());
            }
        }

        #region serialization

        private enum EL
        {
            display_sample_type,
        }

        private enum Attr
        {
            log_x_axis,
            log_y_axis,
            single_batch,
            show_legend,
            show_figures_of_merit
        }

        private CalibrationCurveOptions()
        {
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (DisplaySampleTypes != null)
            {
                throw new InvalidOperationException();
            }

            LogXAxis = reader.GetBoolAttribute(Attr.log_x_axis);
            LogYAxis = reader.GetBoolAttribute(Attr.log_y_axis);
            SingleBatch = reader.GetBoolAttribute(Attr.single_batch);
            ShowLegend = reader.GetBoolAttribute(Attr.show_legend);
            ShowFiguresOfMerit = reader.GetBoolAttribute(Attr.show_legend);
            var xElement = (XElement) XNode.ReadFrom(reader);
            var displaySampleTypes = new List<SampleType>();
            foreach (var el in xElement.Elements(EL.display_sample_type))
            {
                var sampleType = SampleType.FromName(el.Value);
                if (sampleType != null)
                {
                    displaySampleTypes.Add(sampleType);
                }
            }
            DisplaySampleTypes = ImmutableList.ValueOf(displaySampleTypes.Distinct());
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttribute(Attr.log_x_axis, LogXAxis);
            writer.WriteAttribute(Attr.log_y_axis, LogYAxis);
            writer.WriteAttribute(Attr.single_batch, SingleBatch);
            writer.WriteAttribute(Attr.show_legend, ShowLegend);
            writer.WriteAttribute(Attr.show_figures_of_merit, ShowFiguresOfMerit);
            foreach (var displaySampleType in DisplaySampleTypes)
            {
                writer.WriteElementString(EL.display_sample_type, displaySampleType);
            }
        }

        #endregion

        protected bool Equals(CalibrationCurveOptions other)
        {
            return LogXAxis == other.LogXAxis && LogYAxis == other.LogYAxis &&
                   DisplaySampleTypes.Equals(other.DisplaySampleTypes) && SingleBatch == other.SingleBatch &&
                   ShowLegend == other.ShowLegend && ShowSelection == other.ShowSelection &&
                   ShowFiguresOfMerit == other.ShowFiguresOfMerit;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CalibrationCurveOptions)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = LogXAxis.GetHashCode();
                hashCode = (hashCode * 397) ^ LogYAxis.GetHashCode();
                hashCode = (hashCode * 397) ^ DisplaySampleTypes.GetHashCode();
                hashCode = (hashCode * 397) ^ SingleBatch.GetHashCode();
                hashCode = (hashCode * 397) ^ ShowLegend.GetHashCode();
                hashCode = (hashCode * 397) ^ ShowSelection.GetHashCode();
                hashCode = (hashCode * 397) ^ ShowFiguresOfMerit.GetHashCode();
                return hashCode;
            }
        }
    }
}
