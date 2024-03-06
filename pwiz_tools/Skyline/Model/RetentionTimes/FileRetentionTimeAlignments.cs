/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
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
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.RetentionTimes
{
    /// <summary>
    /// List of retention time alignments aligned to one particular data file.
    /// </summary>
    [XmlRoot("file_rt_alignments")]
    public class FileRetentionTimeAlignments : XmlNamedElement
    {
        public FileRetentionTimeAlignments(string dataFileName, IEnumerable<RetentionTimeAlignment> alignments) : base(dataFileName)
        {
            RetentionTimeAlignments = ResultNameMap.FromNamedElements(alignments);
        }
        
        public ResultNameMap<RetentionTimeAlignment> RetentionTimeAlignments { get; private set; }

        #region Object Overrides
        public bool Equals(FileRetentionTimeAlignments other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(other.RetentionTimeAlignments, RetentionTimeAlignments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as FileRetentionTimeAlignments);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ RetentionTimeAlignments.GetHashCode();
            }
        }
        #endregion

        #region Implementation of IXmlSerializable
        private FileRetentionTimeAlignments()
        {
        }

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            writer.WriteElements(RetentionTimeAlignments.Values);
        }

        public static FileRetentionTimeAlignments Deserialize(XmlReader reader)
        {
            return reader.Deserialize(new FileRetentionTimeAlignments());
        }

        public override void ReadXml(XmlReader reader)
        {
            if (null != RetentionTimeAlignments)
            {
                throw new InvalidOperationException();
            }
            base.ReadXml(reader);
            var retentionTimeAlignments = new List<RetentionTimeAlignment>();
            if (reader.IsEmptyElement)
            {
                reader.Read();
            }
            else
            {
                // Read past the property element
                reader.Read();
                reader.ReadElements(retentionTimeAlignments);
                reader.ReadEndElement();
            }
            RetentionTimeAlignments = ResultNameMap.FromNamedElements(retentionTimeAlignments);
        }
        #endregion
    }

    [XmlRoot("rt_alignment")]
    public class RetentionTimeAlignment : XmlNamedElement
    {
        public RetentionTimeAlignment(string name, RegressionLine libraryAlignment) : base(name)
        {
            LibraryAlignment = libraryAlignment;
        }

        public RetentionTimeAlignment(string name, RegressionLineElement regressionLineElement) 
            : this(name, new RegressionLine(regressionLineElement.Slope, regressionLineElement.Intercept))
        {
        }

        public RegressionLine LibraryAlignment { get; private set; }
        public PiecewiseLinearRegression SpectralAlignment { get; private set; }

        public RetentionTimeAlignment ChangeSpectralAlignment(PiecewiseLinearRegression spectralAlignment)
        {
            return ChangeProp(ImClone(this), im => im.SpectralAlignment = spectralAlignment);
        }
        #region Object Overrides
        public bool Equals(RetentionTimeAlignment other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(other.LibraryAlignment, LibraryAlignment);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as RetentionTimeAlignment);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ LibraryAlignment.GetHashCode();
            }
        }

        #endregion
        #region Implementation of IXmlSerializable
        enum ATTR
        {
            name,
            slope,
            intercept,
            x,
            y
        }

        enum EL
        {
            spectral_alignment,
            point
        }

        /// <summary>
        /// For serialization
        /// </summary>
        private RetentionTimeAlignment()
        {
        }

        public static RetentionTimeAlignment Deserialize(XmlReader reader)
        {
            return reader.Deserialize(new RetentionTimeAlignment());
        }

        public override void ReadXml(XmlReader reader)
        {
            var xElement = (XElement)XNode.ReadFrom(reader);
            ReadXmlName(xElement.Attribute(ATTR.name)?.Value);
            var slope = xElement.GetNullableDouble(ATTR.slope);
            if (slope.HasValue)
            {
                LibraryAlignment = new RegressionLine(slope.Value, xElement.GetNullableDouble(ATTR.intercept) ?? 0);
            }

            var elSpectralAlignment = xElement.Elements(EL.spectral_alignment).FirstOrDefault();
            if (elSpectralAlignment != null)
            {
                var xValues = new List<double>();
                var yValues = new List<double>();
                foreach (var elPoint in elSpectralAlignment.Elements(EL.point))
                {
                    xValues.Add(elPoint.GetNullableDouble(ATTR.x).Value);
                    yValues.Add(elPoint.GetNullableDouble(ATTR.y).Value);
                }

                SpectralAlignment = new PiecewiseLinearRegression(xValues, yValues);
            }
            reader.Read();
        }

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            if (LibraryAlignment != null)
            {
                LibraryAlignment.WriteXmlAttributes(writer);
            }

            if (SpectralAlignment != null)
            {
                writer.WriteStartElement(EL.spectral_alignment);
                for (int i = 0; i < SpectralAlignment.XValues.Count; i++)
                {
                    writer.WriteStartElement(EL.point);
                    writer.WriteAttribute(ATTR.x, SpectralAlignment.XValues[i]);
                    writer.WriteAttribute(ATTR.y, SpectralAlignment.YValues[i]);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }
        #endregion
    }

    public class RetentionTimeAlignmentIndices : List<RetentionTimeAlignmentIndex>
    {
        public RetentionTimeAlignmentIndices(FileRetentionTimeAlignments alignments)
        {
            if (alignments != null)
            {
                foreach (var alignment in alignments.RetentionTimeAlignments.Values)
                    Add(new RetentionTimeAlignmentIndex(alignment));
            }
        }
    }

    public class RetentionTimeAlignmentIndex
    {
        public RetentionTimeAlignmentIndex(RetentionTimeAlignment alignment)
        {
            Alignment = alignment;
        }

        public RetentionTimeAlignment Alignment { get; private set; }
        public int? FileIndex { get; set; }
    }
}
