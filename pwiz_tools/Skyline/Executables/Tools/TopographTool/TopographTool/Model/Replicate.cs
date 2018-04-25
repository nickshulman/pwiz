using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class Replicate : Immutable
    {
        public Replicate(string name)
        {
            Name = name;
        }

        public Replicate(ResultRow resultRow)
        {
            Name = resultRow.Replicate;
            Locator = resultRow.ReplicateLocator;
            Cohort = resultRow.Condition;
            TimePoint = resultRow.TimePoint;
        }
        public string Name { get; private set; }
        public string Locator { get; private set; }

        public Replicate ChangeLocator(string locator)
        {
            return ChangeProp(ImClone(this), im => im.Locator = locator);
        }
        public string Cohort { get; private set; }
        public double? TimePoint { get; private set; }
    }
}
