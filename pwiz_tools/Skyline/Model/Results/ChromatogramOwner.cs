using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Results
{
    public class ChromatogramOwner : Immutable
    {
        public ChromatogramOwner(PeptideGroup peptideGroup, PeptideDocNode peptideDocNode, TransitionGroupDocNode transitionGroupDocNode,
            TransitionDocNode transitionDocNode)
        {
            PeptideGroup = peptideGroup;
            PeptideDocNode = peptideDocNode;
            TransitionGroupDocNode = transitionGroupDocNode;
            TransitionDocNode = transitionDocNode;
        }
        public PeptideGroup PeptideGroup { get; }
        public PeptideDocNode PeptideDocNode { get; }
        public TransitionGroupDocNode TransitionGroupDocNode { get; }
        public TransitionDocNode TransitionDocNode { get; }
        public MultiplexMatrix.Replicate MultiplexReplicate { get; private set; }

        public ChromatogramOwner ChangeMultiplexReplicate(MultiplexMatrix.Replicate value)
        {
            return ChangeProp(ImClone(this), im => im.MultiplexReplicate = value);
        }
        public bool IsMs1
        {
            get { return TransitionDocNode?.IsMs1 ?? false; }
        }
    }
}
