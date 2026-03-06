using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Services;

namespace WhatKey.Tests
{
    [TestClass]
    public class BackupDataFileTests
    {
        [TestMethod]
        public void CreateBackupOfDataFile_WhenFileExists_ReturnsExistingPath()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Load(); // creates hotkeys.json

                var backupPath = storage.CreateBackupOfDataFile();

                Assert.IsTrue(File.Exists(backupPath));
            }
        }

        [TestMethod]
        public void CreateBackupOfDataFile_BackupContentMatchesOriginal()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Load();

                var original = File.ReadAllText(storage.DataFilePath);
                var backupPath = storage.CreateBackupOfDataFile();
                var backup = File.ReadAllText(backupPath);

                Assert.AreEqual(original, backup);
            }
        }

        [TestMethod]
        public void CreateBackupOfDataFile_BackupFilenameMatchesPattern()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Load();

                var backupPath = storage.CreateBackupOfDataFile();
                var filename = Path.GetFileName(backupPath);

                // Expected: hotkeys.yyyyMMdd-HHmmss.json.bak
                var pattern = @"^hotkeys\.\d{8}-\d{6}\.json\.bak$";
                Assert.IsTrue(Regex.IsMatch(filename, pattern),
                    $"Unexpected backup filename: {filename}");
            }
        }

        [TestMethod]
        public void CreateBackupOfDataFile_BackupPlacedInSameDirectory()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Load();

                var backupPath = storage.CreateBackupOfDataFile();

                Assert.AreEqual(
                    Path.GetDirectoryName(storage.DataFilePath),
                    Path.GetDirectoryName(backupPath));
            }
        }

        [TestMethod]
        public void CreateBackupOfDataFile_WhenFileNotFound_ThrowsFileNotFoundException()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                // Do NOT call Load() — data file doesn't exist

                Assert.ThrowsException<FileNotFoundException>(() =>
                    storage.CreateBackupOfDataFile());
            }
        }
    }
}
