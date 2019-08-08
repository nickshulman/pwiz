using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Databinding.SettingsEntities
{
    public class NeutralLoss
    {
        public NeutralLoss(FragmentLoss fragmentLoss)
        {
            LossFormula = fragmentLoss.Formula;
            AverageLoss = fragmentLoss.AverageMass;
            MonoisotopicLoss = fragmentLoss.MonoisotopicMass;
            LossInclusion = fragmentLoss.Inclusion;
        }
        public string LossFormula { get; private set; }
        public double AverageLoss { get; private set; }
        public double MonoisotopicLoss { get; private set; }
        public LossInclusion LossInclusion { get; private set; }
    }
}
