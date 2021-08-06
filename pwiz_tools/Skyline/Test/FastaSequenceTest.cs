﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2021 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class FastaSequenceTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestCalculateSequenceCoverage()
        {
            Assert.AreEqual(0, FastaSequence.CalculateSequenceCoverage("ELVISLIVES", new[] {"PEPTIDE"}));
            Assert.AreEqual(0.5, FastaSequence.CalculateSequenceCoverage("ELVISLIVES", new []{"ELVIS"}));
            Assert.AreEqual(0.2, FastaSequence.CalculateSequenceCoverage("ELVISLIVES", new []{"E"}));
            Assert.AreEqual(0, FastaSequence.CalculateSequenceCoverage("ELVISLIVES", new[] { "" }));
            Assert.AreEqual(0.4, FastaSequence.CalculateSequenceCoverage("ELVISLIVES", new[] {"VIS", "ISL"}));
            Assert.AreEqual(17.0 / 21,
                FastaSequence.CalculateSequenceCoverage("PEPTIDEPEPTIDEPEPTIDE", new[] {"PEPTIDEPEP"}));
        }
    }
}
