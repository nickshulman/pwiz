using System.IO;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.DataBinding;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Sharing;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class DocumentReportsTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestDocumentReports()
        {
            var srmDocument = (SrmDocument) new XmlSerializer(typeof(SrmDocument)).Deserialize(new StringReader(DOCUMENT_XML));

            using (var documentReports = new DocumentReports(new SilentProgressMonitor(),
                SkylineDataSchema.MemoryDataSchema(srmDocument, DataSchemaLocalizer.INVARIANT)))
            {
                foreach (var exporter in documentReports.GetReportExporters(CancellationToken.None, srmDocument.Settings.DataSettings
                    .ViewSpecList))
                {
                    Assert.IsNotNull(exporter.TableInfo);
                }
            }

        }

        private const string DOCUMENT_XML = @"<srm_settings format_version='4.21' software_version='Skyline-daily (64-bit) 4.2.1.18334'>
  <settings_summary name='Default'>
    <peptide_settings>
      <enzyme name='Trypsin' cut='KR' no_cut='P' sense='C' />
    </peptide_settings>
    <transition_settings>
      <transition_prediction precursor_mass_type='Monoisotopic' fragment_mass_type='Monoisotopic' optimize_by='None'>
      </transition_prediction>
      <transition_full_scan precursor_isotopes='Percent' precursor_isotope_filter='20' precursor_mass_analyzer='ft_icr' precursor_res='50000' precursor_res_mz='400'>
      </transition_full_scan>
    </transition_settings>
    <data_settings document_guid='58313a6d-58d4-48a5-afd5-0e0fad90cff5'>
      <views>
        <view name='PeptidesView' rowsource='pwiz.Skyline.Model.Databinding.Entities.Peptide' sublist='Results!*'>
          <column name='' />
          <column name='Protein' />
          <column name='ModifiedSequence' />
        </view>
      </views>
    </data_settings>
  </settings_summary>
  <peptide_list label_name='peptides1' websearch_status='X#Upeptides1' auto_manage_children='false'>
    <peptide sequence='ELVIS' modified_sequence='ELVIS' calc_neutral_pep_mass='559.321728' num_missed_cleavages='0'>
      <precursor charge='2' calc_neutral_mass='559.321728' precursor_mz='280.66814' collision_energy='11.325044' modified_sequence='ELVIS'>
        <transition fragment_type='precursor' isotope_dist_rank='1' isotope_dist_proportion='0.72684294'>
          <precursor_mz>280.66814</precursor_mz>
          <product_mz>280.66814</product_mz>
          <collision_energy>11.325044</collision_energy>
        </transition>
        <transition fragment_type='precursor' mass_index='1' isotope_dist_rank='2' isotope_dist_proportion='0.219711885'>
          <precursor_mz>280.66814</precursor_mz>
          <product_mz>281.169662</product_mz>
          <collision_energy>11.325044</collision_energy>
        </transition>
      </precursor>
      <precursor charge='3' calc_neutral_mass='559.321728' precursor_mz='187.447852' collision_energy='9.404018' modified_sequence='ELVIS'>
        <transition fragment_type='precursor' isotope_dist_rank='1' isotope_dist_proportion='0.72684294'>
          <precursor_mz>187.447852</precursor_mz>
          <product_mz>187.447852</product_mz>
          <collision_energy>9.404018</collision_energy>
        </transition>
        <transition fragment_type='precursor' mass_index='1' isotope_dist_rank='2' isotope_dist_proportion='0.206361651'>
          <precursor_mz>187.447852</precursor_mz>
          <product_mz>187.7822</product_mz>
          <collision_energy>9.404018</collision_energy>
        </transition>
      </precursor>
    </peptide>
  </peptide_list>
</srm_settings>";
    }
}
