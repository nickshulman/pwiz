using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using pwiz.Common.DataAnalysis;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding
{
    public class DataWarningType
    {
        private Func<string> _getMessageFunc;

        public DataWarningType(Func<string> getMessageFunc)
        {
            _getMessageFunc = getMessageFunc;
        }

        public string Message
        {
            get { return _getMessageFunc(); }
        }
    }

    public static class DataWarningTypes
    {
        public static readonly DataWarningType MISSING_TRANSITION =
            new DataWarningType(() => "Missing transition peak area");

        public static readonly DataWarningType TRUNCATED_TRANSITION =
            new DataWarningType(() => "Truncated transition peak area");
    }

    public class DocNodeDataWarning
    {
        public DocNodeDataWarning(IdentityPath identityPath, DataWarningType warningType)
        {
            IdentityPath = identityPath;
            DataWarningType = warningType;
        }

        public IdentityPath IdentityPath { get; }
        public DataWarningType DataWarningType { get; }

        public static string GetMessage(SkylineDataSchema dataSchema, IEnumerable<DocNodeDataWarning> docNodeDataWarnings, SrmDocument.Level level)
        {
            var lines = new List<string>();
            var groups = docNodeDataWarnings.GroupBy(warning => warning.DataWarningType);
            foreach (var group in groups)
            {
                lines.Add(group.Key.Message);
                foreach (var element in group)
                {
                    lines.Add(element.GetNodeText(dataSchema, level));
                }
            }

            if (lines.Count == 0)
            {
                return null;
            }

            return TextUtil.LineSeparate(lines);
        }

        public string GetNodeText(SkylineDataSchema dataSchema, SrmDocument.Level startingLevel)
        {
            var parts = new List<string>();
            for (int iLevel = (int)startingLevel; iLevel < IdentityPath.Length; iLevel++)
            {
                parts.Add(GetNodeText(dataSchema, IdentityPath.GetPathTo(iLevel)));
            }

            if (parts.Count == 0)
            {
                return null;
            }

            return TextUtil.SpaceSeparate(parts);
        }

        public static string GetNodeText(SkylineDataSchema dataSchema, IdentityPath identityPath)
        {
            if (identityPath.Length == (int)SrmDocument.Level.MoleculeGroups)
            {
                return new Protein(dataSchema, identityPath).ToString();
            }

            if (identityPath.Length == (int)SrmDocument.Level.Molecules)
            {
                return new Entities.Peptide(dataSchema, identityPath).ToString();
            }

            if (identityPath.Length == (int)SrmDocument.Level.TransitionGroups)
            {
                return new Precursor(dataSchema, identityPath).ToString();
            }

            if (identityPath.Length == (int)SrmDocument.Level.Transitions)
            {
                return new Entities.Transition(dataSchema, identityPath).ToString();
            }

            return null;
        }
    }
}
