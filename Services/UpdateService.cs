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
                string tagName = root.GetProperty("tag_name").GetString();
                string htmlUrl = root.TryGetProperty("html_url", out JsonElement urlElem)
                    ? urlElem.GetString()
                    : string.Empty;

                string versionStr = tagName.TrimStart('v', 'V');
                Version latestVersion = Version.Parse(versionStr);

                bool updateAvailable = latestVersion > currentVersion;
                return new UpdateCheckResult(updateAvailable, versionStr, htmlUrl);
            }
        }
    }
}
