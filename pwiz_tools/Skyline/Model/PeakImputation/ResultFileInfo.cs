using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Model.PeakImputation
{
    public class ResultFileInfo
    {
        public ResultFileInfo(int replicateIndex, ReplicateFileId replicateFileId, MsDataFileUri path,
            AlignmentFunction alignmentFunction)
        {
            ReplicateIndex = replicateIndex;
            ReplicateFileId = replicateFileId;
            Path = path;
            AlignmentFunction = alignmentFunction;
        }
        public int ReplicateIndex { get; }
        public ReplicateFileId ReplicateFileId { get; }
        public MsDataFileUri Path { get; }
        public ChromFileInfoId ChromFileInfoId
        {
            get { return ReplicateFileId.FileId; }
        }
        public ChromatogramSetId ChromatogramSetId
        {
            get { return ReplicateFileId.ChromatogramSetId; }
        }
        public AlignmentFunction AlignmentFunction { get; }
    }
}