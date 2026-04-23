namespace WhatKey.Services
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; }
        public string LatestVersion { get; }
        public string ReleaseUrl { get; }

        public UpdateCheckResult(bool updateAvailable, string latestVersion, string releaseUrl)
        {
            UpdateAvailable = updateAvailable;
            LatestVersion = latestVersion;
            ReleaseUrl = releaseUrl;
        }
    }
}
