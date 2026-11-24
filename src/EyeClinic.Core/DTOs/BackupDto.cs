using System;

namespace EyeClinic.Core.DTOs
{
    public class BackupFileInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; }
        public string SizeDisplay { get; set; }
    }

    public class BackupResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string BackupFilePath { get; set; }
        public string FileName { get; set; }
        public TimeSpan Duration { get; set; }

        public static BackupResult Success(string filePath, string fileName, TimeSpan duration)
        {
            return new BackupResult
            {
                IsSuccess = true,
                BackupFilePath = filePath,
                FileName = fileName,
                Duration = duration
            };
        }

        public static BackupResult Failure(string error)
        {
            return new BackupResult
            {
                IsSuccess = false,
                ErrorMessage = error
            };
        }
    }
}
