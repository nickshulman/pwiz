using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class MultiSequenceLcsTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestMultiSequenceLcs()
        {
            var sequences = new List<List<char>>
            {
                new List<char> { 'G', 'A', 'T', 'T', 'A', 'C', 'A' },
                new List<char> { 'G', 'C', 'A', 'T', 'G', 'C', 'U' },
                new List<char> { 'G', 'A', 'T', 'T', 'T', 'C', 'A' }
            };

            var lcs = LongestCommonSequenceFinder<char>.GetLongestCommonSubsequence(sequences);
            Console.WriteLine("Longest Common Subsequence: " + string.Join("", lcs));

        }
    }
}
