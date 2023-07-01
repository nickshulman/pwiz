namespace pwiz.Skyline.Model.Results
{
    public class FileNameAndSample
    {
        public FileNameAndSample(string fileName, string sampleName)
        {
            FileName = fileName;
            SampleName = sampleName;
        }

        public string FileName { get; }
        public string SampleName { get; }

        protected bool Equals(FileNameAndSample other)
        {
            return FileName == other.FileName && SampleName == other.SampleName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileNameAndSample)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (FileName.GetHashCode() * 397) ^ (SampleName != null ? SampleName.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            if (SampleName == null)
            {
                return FileName;
            }

            return FileName + '|' + SampleName;
        }

        public static FileNameAndSample FromMsDataFileUri(MsDataFileUri msDataFileUri)
        {
            if (msDataFileUri == null)
            {
                return null;
            }

            return new FileNameAndSample(msDataFileUri.GetFileName(), msDataFileUri.GetSampleName());
        }
    }
}
