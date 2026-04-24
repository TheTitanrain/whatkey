using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WhatKey.Tests
{
    [TestClass]
    public class AppVersionTests
    {
        [TestMethod]
        public void ParseInformationalVersion_StripsBuildMetadata()
        {
            Version version = VersionParser.ParseInformationalVersion("1.7.2+0a6fe309267ba92b0d3fe034b3f7e274a924a939");

            Assert.AreEqual(new Version(1, 7, 2), version);
        }
    }
}
