namespace WhatKey.Services
{
    public enum HotkeysLoadStatus
    {
        Success,
        MissingFileLoadedDefaults,
        InvalidFormat
    }

    public class HotkeysLoadResult
    {
        public HotkeysLoadStatus Status { get; set; }
        public string DataFilePath { get; set; }
        public string ErrorMessage { get; set; }

        public static HotkeysLoadResult Ok(string path)
        {
            return new HotkeysLoadResult { Status = HotkeysLoadStatus.Success, DataFilePath = path };
        }

        public static HotkeysLoadResult MissingFile(string path)
        {
            return new HotkeysLoadResult { Status = HotkeysLoadStatus.MissingFileLoadedDefaults, DataFilePath = path };
        }

        public static HotkeysLoadResult Invalid(string path, string errorMessage)
        {
            return new HotkeysLoadResult
            {
                Status = HotkeysLoadStatus.InvalidFormat,
                DataFilePath = path,
                ErrorMessage = errorMessage
            };
        }
    }
}
