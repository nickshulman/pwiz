using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class EnzymeTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestIsCleavageMatch()
        {
            string proteinSequence =
                "MAVSKVYARSVYDSRGNPTVEVELTTEKGVFRSIVPSGASTGVHEALEMRDGDKSKWMGKGVLH" +
                "AVKNVNDVIAPAFVKANIDVKDQKAVDDFLISLDGTANKSKLGANAILGVSLAASRAAAAEKNV" +
                "PLYKHLADLSKSKTSPYVLPVPFLNVLNGGSHAGGALALQEFMIAPTGAKTFAEALRIGSEVYH" +
                "NLKSLTKKRYGASAGNVGDEGGVAPNIQTAEEALDLIVDAIKAAGHDGKVKIGLDCASSEFFKD" +
                "GKYDLDFKNPNSDKSKWLTGPQLADLYHSLMKRYPIVSIEDPFAEDDWEAWSHFFKTAGIQIVA" +
                "DDLTVTNPKRIATAIEKKAADALLLKVNQIGTLSESIKAAQDSFAAGWGVMVSHRSGETEDTFI" +
                "ADLVVGLRTGQIKTGAPARSERLAKLNQLLRIEEELGDNAVFAGENFHHGDKL";
            var fastaSequence = new FastaSequence(null, null, null, proteinSequence);
            var digestSettings = new DigestSettings(int.MaxValue, false);
            foreach (var enzyme in new EnzymeList().GetDefaults())
            {
                var peptides = new HashSet<KeyValuePair<int, int>>();
                foreach (var peptide in enzyme.Digest(fastaSequence, digestSettings))
                {
                    Assert.AreEqual(peptide.End.Value - peptide.Begin.Value, peptide.Sequence.Length);
                    var key = new KeyValuePair<int, int>(peptide.Begin.Value, peptide.End.Value);
                    Assert.IsTrue(peptides.Add(key));
                }

                for (int begin = 0; begin < proteinSequence.Length; begin++)
                {
                    for (int end = begin + 2; end <= proteinSequence.Length; end++)
                    {
                        var key = new KeyValuePair<int, int>(begin, end);
                        string peptideSequence = proteinSequence.Substring(begin, end - begin);
                        char prevAa = begin > 0 ? proteinSequence[begin - 1] : '-';
                        char nextAa = end < proteinSequence.Length ? proteinSequence[end] : '-';

                        bool expectedIsCleavageSite = peptides.Contains(key);
                        bool actualIsCleavageSite = enzyme.IsCleavageMatch(begin, end, proteinSequence);
                        if (expectedIsCleavageSite != actualIsCleavageSite)
                        {
                            Assert.AreEqual(expectedIsCleavageSite, actualIsCleavageSite,
                                "{0}.{1}.{2} not correctly cleaved by {3}", prevAa, peptideSequence, nextAa, enzyme.Name);
                        }
                    }
                }
            }
        }
    }
}
