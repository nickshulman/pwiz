using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Controls.Alignment
{
    [TypeConverter(typeof(Converter))]
    public class RetentionTimeSource
    {
        public RetentionTimeSource(string name, MsDataFileUri msDataFileUri, string filename)
        {
            Name = name;
            MsDataFileUri = msDataFileUri;
            Filename = filename;
        }

        public string Name { get; }
        public override string ToString()
        {
            return Name;
        }

        public MsDataFileUri MsDataFileUri
        {
            get;
        }

        public string Filename { get; }

        protected bool Equals(RetentionTimeSource other)
        {
            return Name == other.Name && Equals(MsDataFileUri, other.MsDataFileUri) && Filename == other.Filename;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RetentionTimeSource)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MsDataFileUri != null ? MsDataFileUri.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Filename != null ? Filename.GetHashCode() : 0);
                return hashCode;
            }
        }

        public class Converter : TypeConverter
        {

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var document = (context.Instance as UserInterfaceObject)?.GetDocument();
                return new StandardValuesCollection(ListRetentionTimeSources(document).ToList());
}

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (!(value is string stringValue))
                {
                    return base.ConvertFrom(context, culture, value);
                }

                return GetStandardValues(context)!.Cast<RetentionTimeSource>()
                    .FirstOrDefault(item => item.Name == stringValue);
            }

        }

        public static IEnumerable<RetentionTimeSource> ListRetentionTimeSources(SrmDocument document)
        {
            return document?.MeasuredResults?.Chromatograms
                       .SelectMany(chromatogramSet => chromatogramSet.MSDataFilePaths)
                       .Select(path => new RetentionTimeSource(path.GetFileName(), path, path.GetFileName())) ??
                   Array.Empty<RetentionTimeSource>();
        }
    }
}