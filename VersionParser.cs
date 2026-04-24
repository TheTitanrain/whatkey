using System;

namespace WhatKey
{
    internal static class VersionParser
    {
        internal static Version ParseInformationalVersion(string informationalVersion)
        {
            if (string.IsNullOrWhiteSpace(informationalVersion))
                return new Version(0, 0, 0);

            string versionStr = informationalVersion;

            int dashIdx = versionStr.IndexOf('-');
            if (dashIdx >= 0)
                versionStr = versionStr.Substring(0, dashIdx);

            int plusIdx = versionStr.IndexOf('+');
            if (plusIdx >= 0)
                versionStr = versionStr.Substring(0, plusIdx);

            return Version.TryParse(versionStr, out Version version)
                ? version
                : new Version(0, 0, 0);
        }
    }
}
