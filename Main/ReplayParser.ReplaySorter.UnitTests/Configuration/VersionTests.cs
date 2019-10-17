using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.UnitTests.Configuration
{
    [TestClass]
    public class VersionTests
    {
        [TestMethod]
        [DataRow("0.0.0", "0.0.1", true)]
        [DataRow("0.0.1", "0.1.1", true)]
        [DataRow("0.1.0", "0.1.1", true)]
        [DataRow("1.0.0", "1.0.1", true)]
        [DataRow("2.9.12", "2.10.11", true)]
        [DataRow("3.9.12", "2.10.11", false)]
        [DataRow("3.19.12", "3.10.11", false)]
        public void LessThan_TwoVersions_ReturnsTrueIfVersionOneIsSmaller(string versionOne, string versionTwo, bool expected)
        {

            var firstVersion = Version.Parse(versionOne);
            var secondVersion = Version.Parse(versionTwo);

            var isSmallerThan = firstVersion < secondVersion;

            Assert.AreEqual(expected, isSmallerThan);
        }
    }
}
