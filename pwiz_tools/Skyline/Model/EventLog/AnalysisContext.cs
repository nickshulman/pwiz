using pwiz.Common.SystemUtil;
using Serilog;

namespace pwiz.Skyline.Model.EventLog
{
    public class AnalysisContext : Immutable
    {
        public const string NAME = "AnalysisContext";
        public static readonly AnalysisContext EMPTY = new AnalysisContext();
        public PeptideGroupDocNode MoleculeGroup { get; private set; }
        public PeptideDocNode Molecule { get; private set; }

        public AnalysisContext ChangeMolecule(PeptideDocNode peptideDocNode)
        {
            return ChangeProp(ImClone(this), im => im.Molecule = peptideDocNode);
        }
        public TransitionGroupDocNode Precursor { get; private set; }

        public Target Target { get; private set; }

        public AnalysisContext ChangeTarget(Target target)
        {
            return ChangeProp(ImClone(this), im => im.Target = target);
        }

        public ILogger QualifyLogger(ILogger logger)
        {
            return logger.ForContext(NAME, this);
        }
    }
}
