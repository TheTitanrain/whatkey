using System;
using System.IO;
using WhatKey.Services;

namespace WhatKey.Tests
{
    internal sealed class TestStorageScope : IDisposable
    {
        private readonly string _dataDir;

        public TestStorageScope()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "WhatKey.Tests", Guid.NewGuid().ToString("N"));
        }

        public string DataDir => _dataDir;

        public HotkeysStorageService CreateStorage()
        {
            return new HotkeysStorageService(_dataDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dataDir))
                Directory.Delete(_dataDir, true);
        }
    }
}
