using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Services;

namespace WhatKey.Tests
{
    [TestClass]
    public class UpdateServiceTests
    {
        private static UpdateService MakeService(string json)
            => new UpdateService(() => Task.FromResult(json));

        [TestMethod]
        public async Task CheckForUpdate_NewerVersionAvailable_ReturnsUpdateAvailable()
        {
            string json = "{\"tag_name\":\"v2.0.0\",\"html_url\":\"https://github.com/releases/tag/v2.0.0\"}";
            var svc = MakeService(json);

            var result = await svc.CheckForUpdateAsync(new Version(1, 0, 0));

            Assert.IsTrue(result.UpdateAvailable);
            Assert.AreEqual("2.0.0", result.LatestVersion);
            Assert.AreEqual("https://github.com/releases/tag/v2.0.0", result.ReleaseUrl);
        }

        [TestMethod]
        public async Task CheckForUpdate_SameVersion_ReturnsNotAvailable()
        {
            string json = "{\"tag_name\":\"v1.0.0\",\"html_url\":\"https://github.com/releases/tag/v1.0.0\"}";
            var svc = MakeService(json);

            var result = await svc.CheckForUpdateAsync(new Version(1, 0, 0));

            Assert.IsFalse(result.UpdateAvailable);
            Assert.AreEqual("1.0.0", result.LatestVersion);
        }

        [TestMethod]
        public async Task CheckForUpdate_OlderRemoteVersion_ReturnsNotAvailable()
        {
            string json = "{\"tag_name\":\"v0.9.0\",\"html_url\":\"https://github.com/releases/tag/v0.9.0\"}";
            var svc = MakeService(json);

            var result = await svc.CheckForUpdateAsync(new Version(1, 0, 0));

            Assert.IsFalse(result.UpdateAvailable);
        }

        [TestMethod]
        public async Task CheckForUpdate_TagWithoutVPrefix_ParsesCorrectly()
        {
            string json = "{\"tag_name\":\"2.1.0\",\"html_url\":\"https://github.com/\"}";
            var svc = MakeService(json);

            var result = await svc.CheckForUpdateAsync(new Version(1, 0, 0));

            Assert.IsTrue(result.UpdateAvailable);
            Assert.AreEqual("2.1.0", result.LatestVersion);
        }

        [TestMethod]
        public async Task CheckForUpdate_TagWithUppercaseV_ParsesCorrectly()
        {
            string json = "{\"tag_name\":\"V1.5.0\",\"html_url\":\"https://github.com/\"}";
            var svc = MakeService(json);

            var result = await svc.CheckForUpdateAsync(new Version(1, 0, 0));

            Assert.IsTrue(result.UpdateAvailable);
            Assert.AreEqual("1.5.0", result.LatestVersion);
        }

        [TestMethod]
        public async Task CheckForUpdate_NetworkError_Throws()
        {
            var svc = new UpdateService(() => throw new System.Net.Http.HttpRequestException("network error"));

            await Assert.ThrowsExceptionAsync<System.Net.Http.HttpRequestException>(
                () => svc.CheckForUpdateAsync(new Version(1, 0, 0)));
        }

        [TestMethod]
        public async Task CheckForUpdate_MalformedJson_Throws()
        {
            var svc = MakeService("not json at all");

            Exception caught = null;
            try { await svc.CheckForUpdateAsync(new Version(1, 0, 0)); }
            catch (Exception ex) { caught = ex; }

            Assert.IsNotNull(caught);
            Assert.IsInstanceOfType(caught, typeof(JsonException));
        }

        [TestMethod]
        public async Task CheckForUpdate_NoHtmlUrl_ReturnsEmptyReleaseUrl()
        {
            string json = "{\"tag_name\":\"v2.0.0\"}";
            var svc = MakeService(json);

            var result = await svc.CheckForUpdateAsync(new Version(1, 0, 0));

            Assert.IsTrue(result.UpdateAvailable);
            Assert.AreEqual(string.Empty, result.ReleaseUrl);
        }
    }
}
