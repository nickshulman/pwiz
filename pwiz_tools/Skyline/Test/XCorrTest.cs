﻿using System;
using System.Collections;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Collections;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.XCorr;
using pwiz.Skyline.Properties;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class XCorrTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestGetYIons()
        {
            var peptide = new Peptide("ELVIS");
            var srmSettings = SrmSettingsList.GetDefault();
            var peptideDocNode = new PeptideDocNode(peptide, srmSettings, null, null, null,
                new TransitionGroupDocNode[0], false);
            var yIons = ArrayXCorrCalculator.GetFragmentIons(peptideDocNode, IonType.y).OrderBy(frag => frag.Mass)
                .ToArray();
            Assert.AreEqual(5, yIons.Length);
            Assert.AreEqual(106.049, yIons[0].Mass, .01);
            Assert.AreEqual(560.329, yIons[4].Mass, .01);
        }

        [TestMethod]
        public void TestGetBIons()
        {
            var peptide = new Peptide("ELVIS");
            var srmSettings = SrmSettingsList.GetDefault();
            var peptideDocNode = new PeptideDocNode(peptide, srmSettings, null, null, null,
                new TransitionGroupDocNode[0], false);
            var bIons = ArrayXCorrCalculator.GetFragmentIons(peptideDocNode, IonType.b).OrderBy(frag => frag.Mass)
                .ToArray();
            Assert.AreEqual(5, bIons.Length);
            Assert.AreEqual(130.049, bIons[0].Mass, .01);
            Assert.AreEqual(542.318, bIons[4].Mass, .01);
        }

        [TestMethod]
        public void TestGetTheoreticalSpectrum()
        {
            SearchParameters searchParameters = SearchParameters.DEFAULT;
            var peptide = new Peptide("ELVIS");
            var srmSettings = SrmSettingsList.GetDefault();
            var peptideDocNode = new PeptideDocNode(peptide, srmSettings, null, null, null,
                new TransitionGroupDocNode[0], false);

            float[] spectrum =
                ArrayXCorrCalculator.getTheoreticalSpectrum(peptideDocNode, 280.6681, 2, searchParameters);
            var nonZeroValues = spectrum.Select((intensity, index) => Tuple.Create(index, intensity))
                .Where(tuple => tuple.Item2 != 0).ToArray();
            Assert.AreEqual(30, nonZeroValues.Length);
            Assert.AreEqual(Tuple.Create(9462, 25f), nonZeroValues[0]);
            Assert.AreEqual(Tuple.Create(50001, 25f), nonZeroValues[29]);
        }

        [TestMethod]
        public void TestXCorr()
        {
            var spectrum = new Spectrum(ImmutableList.ValueOf(SPECTRUM_SDFHLFGPPGKK.Select(tuple => tuple.Item1)),
                ImmutableList.ValueOf(SPECTRUM_SDFHLFGPPGKK.Select(tuple => (float) Math.Sqrt(tuple.Item2))));
            SearchParameters searchParameters = SearchParameters.DEFAULT
                .ChangeFragmentTolerance(MassTolerance.WithAmu(0.5))
                .ChangeFragmentationType(FragmentationType.HCD);
            const int charge = 2;
            const double chargedMz = (float)((1329.6335 + (charge - 1) * MassConstants.protonMass) / charge);

            var preprocessedSpectrum = new ArrayXCorrCalculator(spectrum, chargedMz, charge, searchParameters);
            var peptideDocNode = new PeptideDocNode(new Peptide("SDFHLFGPPGKK"), SrmSettingsList.GetDefault(), null, null, null, new TransitionGroupDocNode[0],false);
            float spectrumFirst = preprocessedSpectrum.score(peptideDocNode);
            Assert.AreEqual(1.7485203, spectrumFirst, .001);
        }

        [TestMethod]
        public void TestSparseXCorrCalculator()
        {
            var spectrum = new Spectrum(ImmutableList.ValueOf(SPECTRUM_SDFHLFGPPGKK.Select(tuple => tuple.Item1)),
                ImmutableList.ValueOf(SPECTRUM_SDFHLFGPPGKK.Select(tuple => (float)Math.Sqrt(tuple.Item2))));
            SearchParameters searchParameters = SearchParameters.DEFAULT
                .ChangeFragmentTolerance(MassTolerance.WithAmu(0.5))
                .ChangeFragmentationType(FragmentationType.HCD);
            var peptideDocNode = new PeptideDocNode(new Peptide("SDFHLFGPPGKK"), SrmSettingsList.GetDefault(), null, null, null, new TransitionGroupDocNode[0], false);
            const int charge = 2;
            const double chargedMz = (float)((1329.6335 + (charge - 1) * MassConstants.protonMass) / charge);
            SparseXCorrCalculator preprocessedSpectrum = new SparseXCorrCalculator(spectrum, Tuple.Create(chargedMz - 10.0, chargedMz + 10.0), searchParameters);
            var spectrumFirst = preprocessedSpectrum.score(peptideDocNode, chargedMz, charge);
            Assert.AreEqual(1.7484008, spectrumFirst, .001);
        }

        [TestMethod]
        public void TestPreprocessSpectrum()
        {
            var spectrum = SPECTRUM_SDFHLFGPPGKK.Select(tuple => (float) tuple.Item2).ToArray();
            var preproc1 = ArrayXCorrCalculator.preprocessSpectrumOld(spectrum);
            var preproc2 = ArrayXCorrCalculator.preprocessSpectrum(spectrum);
            Assert.AreEqual(preproc1.Length, preproc2.Length);
            for (int i = 0; i < preproc1.Length; i++)
            {
                Assert.AreEqual(preproc1[i], preproc2[i], 1e-5);
            }
        }

        private static readonly Tuple<double, double>[] SPECTRUM_SDFHLFGPPGKK = {
            Tuple.Create(55.9993, 1.1043),
            Tuple.Create(70.0645, 10.3129),
            Tuple.Create(83.0716, 1.1043),
            Tuple.Create(84.0789, 19.6009),
            Tuple.Create(86.0975, 5.2426),
            Tuple.Create(110.0729, 68.3379),
            Tuple.Create(111.0832, 3.1202),
            Tuple.Create(120.0822, 29.5011),
            Tuple.Create(127.0822, 3.2608),
            Tuple.Create(129.1012, 48.5011),
            Tuple.Create(129.7266, 1.1043),
            Tuple.Create(130.0869, 25.2154),
            Tuple.Create(138.0564, 3.2608),
            Tuple.Create(147.1117, 36.6032),
            Tuple.Create(148.1132, 4.1474),
            Tuple.Create(152.0816, 3.1202),
            Tuple.Create(166.065, 7.0952),
            Tuple.Create(180.0772, 4.1474),
            Tuple.Create(186.1267, 4.1655),
            Tuple.Create(203.0678, 4.1474),
            Tuple.Create(222.4295, 1.1043),
            Tuple.Create(223.156, 11.2064),
            Tuple.Create(241.5798, 2.1043),
            Tuple.Create(244.1465, 2.1723),
            Tuple.Create(251.1546, 15.2086),
            Tuple.Create(252.1456, 9.0952),
            Tuple.Create(253.1717, 3.1202),
            Tuple.Create(256.5147, 1.1043),
            Tuple.Create(257.1407, 9.4127),
            Tuple.Create(257.192, 2.7619),
            Tuple.Create(268.0887, 3.5079),
            Tuple.Create(268.1273, 3.0794),
            Tuple.Create(275.2216, 8.2154),
            Tuple.Create(276.6354, 4.1043),
            Tuple.Create(279.1522, 2.2086),
            Tuple.Create(280.1571, 6.1474),
            Tuple.Create(281.1453, 1.1043),
            Tuple.Create(283.17, 23.2063),
            Tuple.Create(283.3375, 1.0703),
            Tuple.Create(283.3811, 1.0703),
            Tuple.Create(285.1385, 29.1383),
            Tuple.Create(303.1457, 2.2086),
            Tuple.Create(309.1539, 4.1043),
            Tuple.Create(330.1453, 3.1655),
            Tuple.Create(331.2457, 2.1043),
            Tuple.Create(332.0827, 1.0522),
            Tuple.Create(332.1219, 1.0181),
            Tuple.Create(332.2072, 3.0703),
            Tuple.Create(374.238, 2.1905),
            Tuple.Create(380.1271, 3.3492),
            Tuple.Create(380.2367, 31.9456),
            Tuple.Create(381.2236, 9.1995),
            Tuple.Create(382.2439, 1.1043),
            Tuple.Create(382.6805, 2.1043),
            Tuple.Create(398.1717, 4.0408),
            Tuple.Create(398.2323, 30.0522),
            Tuple.Create(398.7964, 1.1043),
            Tuple.Create(399.2017, 10.1043),
            Tuple.Create(400.1367, 1.0703),
            Tuple.Create(400.2135, 2.0703),
            Tuple.Create(407.6875, 2.2086),
            Tuple.Create(426.2418, 2.1315),
            Tuple.Create(427.184, 2.1134),
            Tuple.Create(427.4288, 1.1043),
            Tuple.Create(429.1383, 3.1293),
            Tuple.Create(429.2892, 17.2404),
            Tuple.Create(430.2682, 2.1043),
            Tuple.Create(437.1702, 4.5261),
            Tuple.Create(437.2307, 9.5079),
            Tuple.Create(437.2858, 2.1406),
            Tuple.Create(437.3437, 1.0272),
            Tuple.Create(438.2165, 6.254),
            Tuple.Create(438.2689, 4.3764),
            Tuple.Create(439.2866, 1.1043),
            Tuple.Create(441.1853, 10.1293),
            Tuple.Create(442.1764, 6.2517),
            Tuple.Create(443.0254, 1.1043),
            Tuple.Create(443.2326, 2.1043),
            Tuple.Create(451.0197, 1.1043),
            Tuple.Create(451.2202, 9.0862),
            Tuple.Create(452.2247, 2.1723),
            Tuple.Create(454.1895, 2.0862),
            Tuple.Create(454.9215, 1.1043),
            Tuple.Create(455.2636, 6.1565),
            Tuple.Create(456.6951, 1.1043),
            Tuple.Create(457.3955, 2.1043),
            Tuple.Create(459.1941, 39.1202),
            Tuple.Create(459.5334, 1.0952),
            Tuple.Create(460.1872, 9.8322),
            Tuple.Create(460.2436, 6.2721),
            Tuple.Create(469.1752, 6.1043),
            Tuple.Create(469.4064, 2.1814),
            Tuple.Create(470.2073, 3.1043),
            Tuple.Create(481.7751, 4.2086),
            Tuple.Create(482.2934, 3.1202),
            Tuple.Create(482.4799, 1.1043),
            Tuple.Create(483.2281, 4.1043),
            Tuple.Create(483.3905, 3.1043),
            Tuple.Create(485.2213, 2.1723),
            Tuple.Create(486.8616, 3.1043),
            Tuple.Create(487.0648, 1.9569),
            Tuple.Create(487.1297, 4.9501),
            Tuple.Create(487.2012, 85.1406),
            Tuple.Create(488.0448, 1.2971),
            Tuple.Create(488.1228, 5.8549),
            Tuple.Create(488.1922, 23.8299),
            Tuple.Create(489.1817, 3.9819),
            Tuple.Create(489.2533, 3.4195),
            Tuple.Create(489.5708, 1.1043),
            Tuple.Create(489.9288, 2.0862),
            Tuple.Create(490.2957, 1.059),
            Tuple.Create(490.6611, 2.2449),
            Tuple.Create(490.7854, 66.3379),
            Tuple.Create(491.0938, 1.068),
            Tuple.Create(491.2648, 35.5737),
            Tuple.Create(491.3113, 10.6553),
            Tuple.Create(491.3618, 2.7574),
            Tuple.Create(491.7568, 9.1224),
            Tuple.Create(491.9549, 1.1043),
            Tuple.Create(492.2857, 4.0884),
            Tuple.Create(496.3032, 2.068),
            Tuple.Create(500.7578, 2.1723),
            Tuple.Create(501.454, 2.1043),
            Tuple.Create(508.3411, 6.2404),
            Tuple.Create(517.7815, 3.1565),
            Tuple.Create(523.2245, 2.1043),
            Tuple.Create(525.8682, 4.1293),
            Tuple.Create(526.3253, 34.1043),
            Tuple.Create(526.4307, 2.0),
            Tuple.Create(527.3469, 14.8934),
            Tuple.Create(527.4077, 3.3129),
            Tuple.Create(532.2488, 2.1043),
            Tuple.Create(536.7666, 2.1043),
            Tuple.Create(537.2595, 2.0862),
            Tuple.Create(537.7569, 1.1406),
            Tuple.Create(537.8157, 2.0703),
            Tuple.Create(541.2333, 3.0862),
            Tuple.Create(542.2748, 2.1043),
            Tuple.Create(543.252, 2.1043),
            Tuple.Create(545.3381, 2.2086),
            Tuple.Create(546.8393, 0.0091),
            Tuple.Create(547.317, 5.1293),
            Tuple.Create(553.1635, 2.1043),
            Tuple.Create(554.2675, 5.1043),
            Tuple.Create(555.2169, 3.0249),
            Tuple.Create(555.2991, 3.254),
            Tuple.Create(555.6202, 1.1043),
            Tuple.Create(555.8151, 4.1905),
            Tuple.Create(560.3041, 2.1043),
            Tuple.Create(560.8757, 2.1723),
            Tuple.Create(562.6242, 3.1043),
            Tuple.Create(562.9001, 4.1655),
            Tuple.Create(564.1757, 2.6304),
            Tuple.Create(564.3076, 53.8073),
            Tuple.Create(564.5995, 3.068),
            Tuple.Create(564.8082, 45.1927),
            Tuple.Create(565.1517, 2.1565),
            Tuple.Create(565.252, 3.0952),
            Tuple.Create(565.3311, 19.4717),
            Tuple.Create(565.4385, 4.5079),
            Tuple.Create(569.2596, 4.3855),
            Tuple.Create(569.3047, 3.2449),
            Tuple.Create(569.655, 1.1746),
            Tuple.Create(569.7268, 1.9977),
            Tuple.Create(570.2278, 2.1043),
            Tuple.Create(572.04, 2.0703),
            Tuple.Create(572.2624, 23.6032),
            Tuple.Create(572.3265, 14.2404),
            Tuple.Create(573.1904, 1.2971),
            Tuple.Create(573.2466, 4.2562),
            Tuple.Create(573.3115, 7.3175),
            Tuple.Create(582.1937, 3.322),
            Tuple.Create(582.3027, 5.0091),
            Tuple.Create(583.2244, 2.2177),
            Tuple.Create(583.3529, 70.4059),
            Tuple.Create(583.4452, 10.0635),
            Tuple.Create(583.629, 2.254),
            Tuple.Create(583.691, 2.0363),
            Tuple.Create(583.8242, 3.1134),
            Tuple.Create(584.2427, 2.5011),
            Tuple.Create(584.335, 31.0181),
            Tuple.Create(584.4376, 3.9751),
            Tuple.Create(584.5014, 0.9819),
            Tuple.Create(585.8071, 3.1565),
            Tuple.Create(589.2236, 2.1134),
            Tuple.Create(592.8562, 3.2608),
            Tuple.Create(598.2714, 3.1043),
            Tuple.Create(599.5961, 4.1655),
            Tuple.Create(600.1809, 6.6939),
            Tuple.Create(600.2755, 66.2494),
            Tuple.Create(601.1538, 2.966),
            Tuple.Create(601.247, 12.5533),
            Tuple.Create(601.3002, 22.0839),
            Tuple.Create(601.6824, 4.0794),
            Tuple.Create(601.7852, 3.1225),
            Tuple.Create(602.2762, 6.6848),
            Tuple.Create(602.3393, 2.9637),
            Tuple.Create(604.3155, 3.1383),
            Tuple.Create(606.1281, 2.0181),
            Tuple.Create(606.2216, 2.1224),
            Tuple.Create(607.4611, 2.1043),
            Tuple.Create(607.7983, 1.1043),
            Tuple.Create(610.3087, 2.1043),
            Tuple.Create(612.2322, 3.1655),
            Tuple.Create(612.7525, 2.1043),
            Tuple.Create(613.0962, 3.1043),
            Tuple.Create(613.7551, 1.1043),
            Tuple.Create(614.0671, 3.1565),
            Tuple.Create(616.8265, 7.1565),
            Tuple.Create(617.3437, 4.1905),
            Tuple.Create(618.2057, 3.1565),
            Tuple.Create(621.8342, 5.1905),
            Tuple.Create(621.9965, 1.0794),
            Tuple.Create(622.3388, 5.1293),
            Tuple.Create(625.792, 2.0862),
            Tuple.Create(626.8209, 2.068),
            Tuple.Create(628.3732, 3.1202),
            Tuple.Create(629.0159, 2.1043),
            Tuple.Create(629.7411, 3.2608),
            Tuple.Create(642.5325, 2.1043),
            Tuple.Create(642.895, 1.1043),
            Tuple.Create(647.8181, 2.1043),
            Tuple.Create(648.1352, 1.1043),
            Tuple.Create(648.3749, 3.1134),
            Tuple.Create(650.1998, 2.0612),
            Tuple.Create(650.3008, 2.0522),
            Tuple.Create(653.5533, 1.1043),
            Tuple.Create(654.5064, 2.1134),
            Tuple.Create(654.6762, 3.1134),
            Tuple.Create(654.9456, 1.1043),
            Tuple.Create(655.8965, 3.1043),
            Tuple.Create(656.0515, 2.1043),
            Tuple.Create(656.3026, 11.4195),
            Tuple.Create(656.4018, 1.966),
            Tuple.Create(656.8343, 10.1315),
            Tuple.Create(656.9374, 0.9841),
            Tuple.Create(657.0061, 2.1224),
            Tuple.Create(657.3358, 6.1678),
            Tuple.Create(657.4254, 2.9456),
            Tuple.Create(657.7433, 1.0522),
            Tuple.Create(657.8486, 6.0522),
            Tuple.Create(661.3605, 2.2086),
            Tuple.Create(663.4733, 5.0522),
            Tuple.Create(663.7089, 2.2177),
            Tuple.Create(663.9099, 7.0884),
            Tuple.Create(664.6599, 5.5147),
            Tuple.Create(664.7172, 1.7551),
            Tuple.Create(664.7834, 3.7211),
            Tuple.Create(664.8461, 4.7483),
            Tuple.Create(664.9304, 6.161),
            Tuple.Create(665.0489, 6.3107),
            Tuple.Create(665.3315, 276.508),
            Tuple.Create(665.5107, 8.3855),
            Tuple.Create(665.572, 2.4649),
            Tuple.Create(665.65, 1.7914),
            Tuple.Create(665.8348, 205.5215),
            Tuple.Create(666.0325, 7.932),
            Tuple.Create(666.3128, 78.9796),
            Tuple.Create(666.3797, 46.9093),
            Tuple.Create(666.4427, 8.4671),
            Tuple.Create(666.5762, 7.8345),
            Tuple.Create(666.7386, 4.0952),
            Tuple.Create(666.8368, 36.7256),
            Tuple.Create(666.9183, 5.7642),
            Tuple.Create(667.0396, 3.5714),
            Tuple.Create(667.1044, 3.1315),
            Tuple.Create(668.388, 2.1043),
            Tuple.Create(668.5468, 2.1043),
            Tuple.Create(669.3988, 2.1723),
            Tuple.Create(670.9902, 2.3492),
            Tuple.Create(671.0584, 3.0952),
            Tuple.Create(675.3389, 3.1043),
            Tuple.Create(685.6368, 2.1043),
            Tuple.Create(689.3867, 2.0862),
            Tuple.Create(690.972, 1.1043),
            Tuple.Create(691.812, 2.1043),
            Tuple.Create(697.3419, 6.1565),
            Tuple.Create(698.2413, 2.1905),
            Tuple.Create(711.4145, 2.1043),
            Tuple.Create(712.3525, 4.0839),
            Tuple.Create(713.348, 2.0862),
            Tuple.Create(714.4131, 1.1043),
            Tuple.Create(715.3547, 2.1043),
            Tuple.Create(715.4915, 3.1043),
            Tuple.Create(719.3215, 4.1859),
            Tuple.Create(719.4109, 7.0771),
            Tuple.Create(720.319, 6.2268),
            Tuple.Create(728.1404, 4.2086),
            Tuple.Create(728.3716, 1.1043),
            Tuple.Create(728.6315, 2.1043),
            Tuple.Create(728.9446, 3.1202),
            Tuple.Create(730.1498, 5.1927),
            Tuple.Create(730.2178, 0.9048),
            Tuple.Create(730.4232, 141.4603),
            Tuple.Create(731.02, 2.1542),
            Tuple.Create(731.1734, 3.2358),
            Tuple.Create(731.3983, 41.7483),
            Tuple.Create(731.456, 22.0839),
            Tuple.Create(731.5421, 5.737),
            Tuple.Create(740.408, 3.1043),
            Tuple.Create(742.0305, 1.1043),
            Tuple.Create(743.2635, 2.1043),
            Tuple.Create(743.6923, 1.1043),
            Tuple.Create(744.8652, 2.1043),
            Tuple.Create(745.2878, 1.0522),
            Tuple.Create(745.3464, 1.0181),
            Tuple.Create(745.4142, 1.0703),
            Tuple.Create(747.3405, 21.034),
            Tuple.Create(747.4027, 4.2993),
            Tuple.Create(748.0912, 1.0181),
            Tuple.Create(748.1527, 2.2971),
            Tuple.Create(748.3811, 11.1655),
            Tuple.Create(749.2866, 1.2971),
            Tuple.Create(749.3729, 4.1655),
            Tuple.Create(758.3728, 2.1043),
            Tuple.Create(771.4629, 3.1383),
            Tuple.Create(786.3571, 4.1565),
            Tuple.Create(788.3912, 4.4717),
            Tuple.Create(788.4962, 1.9229),
            Tuple.Create(796.3875, 3.1383),
            Tuple.Create(802.8114, 3.1202),
            Tuple.Create(803.0585, 2.1043),
            Tuple.Create(803.447, 2.1043),
            Tuple.Create(804.2003, 3.4286),
            Tuple.Create(804.3193, 19.4014),
            Tuple.Create(804.3796, 35.1587),
            Tuple.Create(804.5279, 2.9297),
            Tuple.Create(805.2244, 1.6304),
            Tuple.Create(805.3371, 14.034),
            Tuple.Create(805.4307, 5.3492),
            Tuple.Create(806.4074, 1.068),
            Tuple.Create(807.36, 3.1043),
            Tuple.Create(808.4484, 2.1723),
            Tuple.Create(826.5018, 4.0952),
            Tuple.Create(831.8314, 1.0703),
            Tuple.Create(831.9061, 1.0703),
            Tuple.Create(834.4854, 2.1043),
            Tuple.Create(840.6498, 3.2086),
            Tuple.Create(843.1672, 2.059),
            Tuple.Create(843.3151, 3.3084),
            Tuple.Create(843.4782, 93.059),
            Tuple.Create(843.5424, 52.8889),
            Tuple.Create(844.0868, 5.1655),
            Tuple.Create(844.377, 8.0159),
            Tuple.Create(844.4626, 16.6122),
            Tuple.Create(844.524, 49.1497),
            Tuple.Create(844.674, 1.9229),
            Tuple.Create(845.5133, 8.1383),
            Tuple.Create(846.5802, 2.0703),
            Tuple.Create(887.5107, 2.0862),
            Tuple.Create(896.0469, 2.1043),
            Tuple.Create(897.4969, 0.9456),
            Tuple.Create(897.5737, 2.1746),
            Tuple.Create(901.2878, 1.9569),
            Tuple.Create(901.3824, 3.5624),
            Tuple.Create(901.4933, 2.8435),
            Tuple.Create(903.4241, 2.1723),
            Tuple.Create(962.5514, 3.0884),
            Tuple.Create(970.6058, 2.1723),
            Tuple.Create(979.8937, 2.0703),
            Tuple.Create(980.0072, 1.0703),
            Tuple.Create(980.5701, 20.1905),
            Tuple.Create(981.4562, 5.4444),
            Tuple.Create(981.632, 6.805),
            Tuple.Create(981.7165, 1.3311),
            Tuple.Create(982.4251, 1.9819),
            Tuple.Create(982.5695, 4.229),
            Tuple.Create(983.4538, 2.0703),
            Tuple.Create(983.5588, 1.0794),
            Tuple.Create(999.7496, 2.1224),
            Tuple.Create(1000.4876, 3.1043),
            Tuple.Create(1055.3597, 3.6848),
            Tuple.Create(1055.4299, 8.9751),
            Tuple.Create(1055.5284, 6.0317),
            Tuple.Create(1055.635, 4.5147),
            Tuple.Create(1056.3767, 5.7007),
            Tuple.Create(1056.5034, 7.8866),
            Tuple.Create(1056.5859, 2.483),
            Tuple.Create(1057.502, 7.1225),
            Tuple.Create(1073.5131, 4.1383),
            Tuple.Create(1074.5001, 2.1723),
            Tuple.Create(1088.5435, 3.1043),
            Tuple.Create(1089.4951, 3.1043),
            Tuple.Create(1098.3671, 2.1043),
            Tuple.Create(1100.5928, 3.1565),
            Tuple.Create(1127.479, 2.0703),
            Tuple.Create(1127.5829, 1.0635),
            Tuple.Create(1127.7003, 6.1066),
            Tuple.Create(1128.2417, 1.1043),
            Tuple.Create(1128.6362, 5.1043),
            Tuple.Create(1134.5321, 1.1043),
            Tuple.Create(1135.6837, 2.0862),
            Tuple.Create(1162.6857, 2.2086),
            Tuple.Create(1183.3735, 0.9297),
            Tuple.Create(1183.4602, 5.4127),
            Tuple.Create(1183.6226, 11.2721),
            Tuple.Create(1184.0847, 2.1043),
            Tuple.Create(1184.4084, 2.1043),
            Tuple.Create(1184.5939, 4.7891),
            Tuple.Create(1184.698, 4.8413),
            Tuple.Create(1184.9564, 3.1202),
            Tuple.Create(1185.5859, 3.1927),
            Tuple.Create(1185.6693, 0.9478),
            Tuple.Create(1185.7546, 3.3084),
            Tuple.Create(1203.5687, 2.0726),
            Tuple.Create(1203.6633, 1.0181),
            Tuple.Create(1231.8566, 2.1043),
            Tuple.Create(1233.7826, 2.1043),
            Tuple.Create(1242.616, 1.1043),
            Tuple.Create(1249.652, 2.1043),
            Tuple.Create(1263.5773, 2.2086)
        };
    }
}
