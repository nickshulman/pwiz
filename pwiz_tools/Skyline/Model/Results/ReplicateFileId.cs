using System.Runtime.CompilerServices;

namespace pwiz.Skyline.Model.Results
{
    public class ReplicateFileId
    {
        public ReplicateFileId(int replicateIndex, ChromFileInfoId fileId)
        {
            ReplicateIndex = replicateIndex;
            FileId = fileId;
        }

        public int ReplicateIndex { get; }
        public ChromFileInfoId FileId { get; }

        protected bool Equals(ReplicateFileId other)
        {
            return ReplicateIndex == other.ReplicateIndex && ReferenceEquals(FileId, other.FileId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ReplicateFileId)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ReplicateIndex * 397) ^ RuntimeHelpers.GetHashCode(FileId);
            }
        }

        public static ReplicateFileId Find(SrmDocument document, MsDataFileUri msDataFileUri)
        {
            var measuredResults = document.MeasuredResults;
            if (measuredResults == null)
            {
                return null;
            }
            for (int i = 0; i < measuredResults.Chromatograms.Count; i++)
            {
                var chromFileInfoId = measuredResults.Chromatograms[i].FindFile(msDataFileUri);
                if (chromFileInfoId != null)
                {
                    return new ReplicateFileId(i, chromFileInfoId);
                }
            }

            return null;
        }
    }
}