using System;
using System.Web.UI.WebControls;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model
{
    public class ProteinId : Identity
    {
        public ProteinId(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class ProteinDocNode : DocNode
    {
        public ProteinDocNode(ProteinId id) : base(id)
        {
        }

        public override AnnotationDef.AnnotationTarget AnnotationTarget
        {
            get { throw new InvalidOperationException(); }
        }


    }
}
