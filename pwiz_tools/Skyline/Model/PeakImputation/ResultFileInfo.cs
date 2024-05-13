using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Model.PeakImputation
{
    public class ResultFileInfo
    {
        public ResultFileInfo(ReplicateFileId replicateFileId, MsDataFileUri path,
            AlignmentFunction alignmentFunction)
        {
            ReplicateFileId = replicateFileId;
            Path = path;
            AlignmentFunction = alignmentFunction;
        }
        public ReplicateFileId ReplicateFileId { get; }
        public int ReplicateIndex
        {
            get { return ReplicateFileId.ReplicateIndex; }
        }
        public MsDataFileUri Path { get; }
        public ChromFileInfoId ChromFileInfoId
        {
            get { return ReplicateFileId.FileId; }
        }
        public AlignmentFunction AlignmentFunction { get; }
    }
}