using System;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results.Scoring.Tric
{
    public class RunToRunAlignmentOption
    {
        public static RunToRunAlignmentOption NONE = new RunToRunAlignmentOption(@"none", () => "None");

        public static RunToRunAlignmentOption COPY_BEST_PEAK =
            new RunToRunAlignmentOption(@"copy", () => "Copy best peak");

        public static RunToRunAlignmentOption RESCORE =
            new RunToRunAlignmentOption(@"rescore", ()=>"Rescore peaks");
        private readonly Func<string> _getLabelFunc;
        private RunToRunAlignmentOption(string name, Func<string> getLabelFunc)
        {
            Name = name;
            _getLabelFunc = getLabelFunc;
        }

        public string Name { get; }
        public string Label
        {
            get { return _getLabelFunc(); }
        }

        public override string ToString()
        {
            return Label;
        }

        public static readonly ImmutableList<RunToRunAlignmentOption> ALL =
            ImmutableList.ValueOf(new[] { NONE, COPY_BEST_PEAK, RESCORE });

        public static RunToRunAlignmentOption FromName(string name)
        {
            return ALL.FirstOrDefault(x => x.Name == name) ?? NONE;
        }
    }
}
