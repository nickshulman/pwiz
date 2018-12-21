using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.ElementLocators
{
    public class ElementLocatorsExporter
    {
        private static readonly char[] spaces = new string(' ', 32).ToCharArray();
        public ElementLocatorsExporter(ElementRefs elementRefs)
        {
            ElementRefs = elementRefs;
        }

        public ElementRefs ElementRefs { get; private set; }
        public SrmDocument Document
        {
            get { return ElementRefs.Document; }
        }

        public void WriteElementRefs(TextWriter writer)
        {
            foreach (var moleculeGroup in Document.MoleculeGroups)
            {
                WriteNodeRefs(writer, 0, IdentityPath.ROOT, moleculeGroup);
            }

            if (Document.Settings.HasResults)
            {
                foreach (var chromatogramSet in Document.Settings.MeasuredResults.Chromatograms)
                {
                    var replicateRef = ReplicateRef.FromChromatogramSet(chromatogramSet);
                    WriteElementRef(writer, 0, replicateRef);
                    foreach (var resultFileRef in ResultFileRef.PROTOTYPE.ChangeParent(replicateRef)
                        .ListChildrenOfParent(Document))
                    {
                        WriteElementRef(writer, 1, resultFileRef);
                    }
                }
            }
        }

        public void WriteNodeRefs(TextWriter writer, int indentLevel, IdentityPath parent, DocNode docNode)
        {
            var identityPath = new IdentityPath(parent, docNode.Id);
            var nodeRef = ElementRefs.GetNodeRef(identityPath);
            WriteElementRef(writer, indentLevel, nodeRef);
            var docNodeParent = docNode as DocNodeParent;
            if (docNodeParent != null)
            {
                foreach (var child in docNodeParent.Children)
                {
                    WriteNodeRefs(writer, indentLevel + 1, identityPath, child);
                }
            }

            ResultRef resultPrototype = null;
            if (nodeRef is MoleculeRef)
            {
                resultPrototype = MoleculeResultRef.PROTOTYPE;
            }
            else if (nodeRef is PrecursorRef)
            {
                resultPrototype = PrecursorResultRef.PROTOTYPE;
            }
            else if (nodeRef is TransitionRef)
            {
                resultPrototype = TransitionResultRef.PROTOTYPE;
            }

            if (resultPrototype != null)
            {
                foreach (var resultRef in resultPrototype.ChangeParent(nodeRef).ListChildrenOfParent(Document))
                {
                    WriteElementRef(writer, indentLevel + 1, resultRef);
                }
            }
        }

        private void WriteElementRef(TextWriter writer, int indentLevel, ElementRef elementRef)
        {
            writer.Write(new string(' ', indentLevel));
            var locator = elementRef.ChangeParent(null).ToElementLocator();
            writer.WriteLine(locator);
        }
    }
}
