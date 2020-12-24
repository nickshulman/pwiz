using System;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Find;
using Serilog;
using Serilog.Core;

namespace pwiz.Skyline.Model.EventLog
{
    public class AnalysisContext : Immutable
    {
        public const string NAME = "AnalysisContext";
        public SrmDocument Document { get; private set; }
        public PeptideGroupDocNode MoleculeGroup { get; private set; }
        public PeptideDocNode Molecule { get; private set; }
        public TransitionGroupDocNode Precursor { get; private set; }
        public 

        public ILogger QualifyLogger(ILogger logger)
        {
            return logger.ForContext(NAME, this);
        }
    }
}
