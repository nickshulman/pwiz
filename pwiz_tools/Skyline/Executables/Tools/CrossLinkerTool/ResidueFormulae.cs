using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.Chemistry;

namespace CrossLinkerTool
{
    public class ResidueFormulae
    {
        public static ResidueFormulae GetDefault()
        {
            var residueFormula = new ResidueFormulae(AminoAcidFormulas.Default);
            var overridesFile = Path.Combine(Path.GetDirectoryName(typeof(ResidueFormulae).Assembly.Location),
                "ResidueFormulae.txt");
            if (File.Exists(overridesFile))
            {
                using (var stream = File.OpenRead(overridesFile))
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        residueFormula.ApplyOverrides(reader);
                    }
                }
            }

            return residueFormula;
        }

        private AminoAcidFormulas _aminoAcidFormulas;
        private Dictionary<char, string> _overrideFormulae = new Dictionary<char, string>();
        private Dictionary<char, string> _modificationNames = new Dictionary<char, string>();
        public ResidueFormulae(AminoAcidFormulas aminoAcidFormulas)
        {
            _aminoAcidFormulas = aminoAcidFormulas;
        }

        public void ApplyOverrides(TextReader overrideReader)
        {
            string line;
            while (null != (line = overrideReader.ReadLine()))
            {
                line = line.Trim();
                if (line.StartsWith("#"))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var values = line.Split('\t');
                var aa = values[0][0];
                var formula = values[1];
                _overrideFormulae[aa] = formula;
                if (values.Length > 2)
                {
                    _modificationNames[aa] = values[2];
                }
            }
        }

        public string GetResidueFormula(char ch)
        {
            string formula;
            if (_overrideFormulae.TryGetValue(ch, out formula))
            {
                return formula;
            }

            if (_aminoAcidFormulas.Formulas.TryGetValue(ch, out formula))
            {
                return formula;
            }

            return string.Empty;
        }

        public string GetModificationName(char c)
        {
            string modificationName;
            _modificationNames.TryGetValue(c, out modificationName);
            return modificationName;
        }

        public Molecule GetPeptideFormula(string sequence)
        {
            var formula = string.Join(string.Empty, sequence.Select(GetResidueFormula)) + "H2O";
            return Molecule.Parse(formula);
        }


    }
}
