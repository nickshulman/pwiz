using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.RetentionTimes
{
    [XmlRoot("spectral_alignments")]
    public class SpectralAlignments : Immutable, IXmlSerializable, IAlignmentTarget
    {
        private ImmutableList<KeyValuePair<MsDataFileUri, PiecewiseLinearRegression>> _alignments;
        public SpectralAlignments(MsDataFileUri target,
            IEnumerable<KeyValuePair<MsDataFileUri, PiecewiseLinearRegression>> alignments)
        {
            Target = target;
            _alignments = ImmutableList.ValueOf(alignments);
        }

        public MsDataFileUri Target { get; private set; }

        public string Name
        {
            get { return Target.GetFileName(); }
        }

        public IEnumerable<KeyValuePair<MsDataFileUri, PiecewiseLinearRegression>> Alignments
        {
            get
            {
                return _alignments.AsEnumerable();
            }
        }

        public PiecewiseLinearRegression GetAlignment(MsDataFileUri source)
        {
            return _alignments.FirstOrDefault(kvp => Equals(kvp.Key, source)).Value;
        }

        enum EL
        {
            spectral_alignment,
            point
        }

        enum ATTR
        {
            path,
            x,
            y
        }

        private SpectralAlignments()
        {
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (_alignments != null)
            {
                throw new InvalidOperationException();
            }
            var xElement = (XElement)XNode.ReadFrom(reader);
            Target = MsDataFileUri.Parse(xElement.Attribute(ATTR.path).Value);
            var alignments = new List<KeyValuePair<MsDataFileUri, PiecewiseLinearRegression>>();
            foreach (var el in xElement.Elements(EL.spectral_alignment))
            {
                var xValues = new List<double>();
                var yValues = new List<double>();
                foreach (var elPoint in el.Elements(EL.point))
                {
                    xValues.Add(elPoint.GetNullableDouble(ATTR.x).Value);
                    yValues.Add(elPoint.GetNullableDouble(ATTR.y).Value);
                }
                var linearRegression = new PiecewiseLinearRegression(xValues, yValues);
                alignments.Add(
                    new KeyValuePair<MsDataFileUri, PiecewiseLinearRegression>(
                        MsDataFileUri.Parse(el.Attribute(ATTR.path).Value), linearRegression));
            }

            _alignments = ImmutableList.ValueOf(alignments);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttribute(ATTR.path, Target.ToString());
            foreach (var kvp in _alignments)
            {
                writer.WriteStartElement(EL.spectral_alignment);
                writer.WriteAttribute(ATTR.path, kvp.Key);
                for (int i = 0; i < kvp.Value.XValues.Count; i++)
                {
                    writer.WriteStartElement(EL.point);
                    writer.WriteAttribute(ATTR.x, kvp.Value.XValues[i]);
                    writer.WriteAttribute(ATTR.y, kvp.Value.YValues[i]);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        protected bool Equals(SpectralAlignments other)
        {
            return _alignments.Equals(other._alignments) && Target.Equals(other.Target);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SpectralAlignments)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_alignments.GetHashCode() * 397) ^ Target.GetHashCode();
            }
        }

        public static SpectralAlignments Deserialize(XmlReader reader)
        {
            return reader.Deserialize(new SpectralAlignments());
        }
    }
}
