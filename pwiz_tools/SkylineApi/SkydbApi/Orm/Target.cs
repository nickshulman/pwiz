using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class(Lazy = false, Table = nameof(Target))]
    public class Target : Entity
    {
        [Property]
        public string ModifiedPeptideSequence { get; set; }
        [Property]
        public string Formula { get; set; }
        [Property]
        public string Name { get; set; }
        [Property]
        public double MonoMass { get; set; }
        [Property]
        public double AverageMass { get; set; }
        [Property]
        public string InChiKey { get; set; }
        [Property]
        public string Cas { get; set; }
        [Property]
        public string Hmdb { get; set; }
        [Property]
        public string InChi { get; set; }
        [Property]
        public string Smiles { get; set; }
        [Property]
        public string Kegg { get; set; }
    }
}
