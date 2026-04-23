using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WhatKey.Services
{
    public class UpdateService
    {
        private const string ApiUrl = "https://api.github.com/repos/TheTitanrain/whatkey/releases/latest";

        private static readonly HttpClient _httpClient = new HttpClient();

        static UpdateService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WhatKey");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        private readonly Func<Task<string>> _fetchJson;

        public UpdateService() : this(DefaultFetch) { }

        public UpdateService(Func<Task<string>> fetchJson)
        {
            _fetchJson = fetchJson;
        }

        private static async Task<string> DefaultFetch()
        {
            return await _httpClient.GetStringAsync(ApiUrl).ConfigureAwait(false);
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(Version currentVersion)
        {
            string json = await _fetchJson().ConfigureAwait(false);

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("tag_name", out JsonElement tagElem))
                    throw new FormatException("GitHub release tag_name is missing.");
                string tagName = tagElem.GetString();
                if (string.IsNullOrEmpty(tagName))
                    throw new FormatException("GitHub release tag_name is missing or null.");

                string htmlUrl = root.TryGetProperty("html_url", out JsonElement urlElem)
                    ? (urlElem.GetString() ?? string.Empty)
                    : string.Empty;

                string versionStr = tagName.TrimStart('v', 'V');
                int dashIdx = versionStr.IndexOf('-');
                if (dashIdx >= 0)
                    versionStr = versionStr.Substring(0, dashIdx);

                if (!Version.TryParse(versionStr, out Version latestVersion))
                    throw new FormatException($"Cannot parse version from tag '{tagName}'.");

                bool updateAvailable = latestVersion > currentVersion;
                return new UpdateCheckResult(updateAvailable, versionStr, htmlUrl);
            }
        }
    }
}
