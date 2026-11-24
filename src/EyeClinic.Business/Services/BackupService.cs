using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.DTOs;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Configuration;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class BackupService
    {
        private readonly SettingsService _settingsService;

        public BackupService()
        {
            _settingsService = new SettingsService();
        }

        public BackupService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public string GetDefaultBackupDirectory()
        {
            string savedPath = _settingsService.GetSetting("Backup_Directory", "");
            if (!string.IsNullOrWhiteSpace(savedPath))
            {
                try
                {
                    if (!Directory.Exists(savedPath))
                    {
                        Directory.CreateDirectory(savedPath);
                    }
                    return savedPath;
                }
                catch
                {
                    // If customized path is invalid/inaccessible, fallback
                }
            }

            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EyeClinic",
                "Backups"
            );

            try
            {
                if (!Directory.Exists(defaultPath))
                {
                    Directory.CreateDirectory(defaultPath);
                }
            }
            catch
            {
                // Fallback to local drive root if permissions in AppData are restricted
                defaultPath = @"C:\EyeClinicBackups";
                if (!Directory.Exists(defaultPath))
                {
                    try { Directory.CreateDirectory(defaultPath); } catch { }
                }
            }

            return defaultPath;
        }

        public Result SetBackupDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return Result.Failure("مسار مجلد النسخ الاحتياطي غير صالح.");

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var dict = new Dictionary<string, string>
                {
                    { "Backup_Directory", directoryPath }
                };
                return _settingsService.SaveSettings(dict);
            }
            catch (Exception ex)
            {
                return Result.Failure("تعذر الوصول إلى المسار المحدد: " + ex.Message);
            }
        }

        public BackupResult CreateBackup(string targetDirectory = null, string customFileName = null)
        {
            var sw = Stopwatch.StartNew();
            string dir = string.IsNullOrWhiteSpace(targetDirectory) ? GetDefaultBackupDirectory() : targetDirectory;

            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string fileName = string.IsNullOrWhiteSpace(customFileName)
                    ? string.Format("EyeClinic_Backup_{0:yyyyMMdd_HHmmss}.bak", DateTime.Now)
                    : customFileName;

                if (!fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".bak";
                }

                string fullPath = Path.Combine(dir, fileName);
                string connectionString = ConnectionConfig.GetConnectionString();

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string dbName = conn.Database;
                    if (string.IsNullOrEmpty(dbName))
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString);
                        dbName = builder.InitialCatalog;
                    }

                    string sql = string.Format(
                        "BACKUP DATABASE [{0}] TO DISK = @backupPath WITH FORMAT, INIT, NAME = 'EyeClinic Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10;",
                        dbName
                    );

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 minutes timeout for large databases
                        cmd.Parameters.AddWithValue("@backupPath", fullPath);
                        cmd.ExecuteNonQuery();
                    }
                }

                sw.Stop();

                // Rotate backups: Keep latest 30 backups automatically (PRD: BKP-1)
                RotateBackups(dir, 30);

                // Write Audit Log
                LogAudit(AuditActionType.DatabaseBackup, "Database", fullPath,
                    string.Format("تم إنشاء نسخة احتياطية بنجاح: {0} ({1:N1} ثواني)", fileName, sw.Elapsed.TotalSeconds));

                return BackupResult.Success(fullPath, fileName, sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return BackupResult.Failure("فشل إنشاء النسخة الاحتياطية: " + ex.Message);
            }
        }

        public Result RestoreBackup(string backupFilePath)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("استرجاع قاعدة البيانات مقتصر على الطبيب/المدير فقط.");

            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
                return Result.Failure("ملف النسخة الاحتياطية غير موجود أو المسار غير صحيح.");

            try
            {
                var builder = new SqlConnectionStringBuilder(ConnectionConfig.GetConnectionString());
                string targetDbName = builder.InitialCatalog;

                // To restore, we must connect to 'master' database so target database is not in use
                builder.InitialCatalog = "master";

                using (var conn = new SqlConnection(builder.ConnectionString))
                {
                    conn.Open();

                    string sql = string.Format(@"
                        ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        RESTORE DATABASE [{0}] FROM DISK = @path WITH REPLACE;
                        ALTER DATABASE [{0}] SET MULTI_USER;
                    ", targetDbName);

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 600; // 10 minutes timeout
                        cmd.Parameters.AddWithValue("@path", backupFilePath);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Write Audit Log
                LogAudit(AuditActionType.DatabaseRestore, "Database", backupFilePath,
                    string.Format("تم استرجاع قاعدة البيانات بنجاح من الملف: {0}", Path.GetFileName(backupFilePath)));

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure("فشل استرجاع قاعدة البيانات: " + ex.Message);
            }
        }

        public List<BackupFileInfo> GetExistingBackups(string targetDirectory = null)
        {
            string dir = string.IsNullOrWhiteSpace(targetDirectory) ? GetDefaultBackupDirectory() : targetDirectory;
            var list = new List<BackupFileInfo>();

            try
            {
                if (!Directory.Exists(dir))
                    return list;

                var files = new DirectoryInfo(dir).GetFiles("*.bak")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                foreach (var f in files)
                {
                    list.Add(new BackupFileInfo
                    {
                        FileName = f.Name,
                        FilePath = f.FullName,
                        CreatedAt = f.CreationTime,
                        SizeBytes = f.Length,
                        SizeDisplay = FormatSize(f.Length)
                    });
                }
            }
            catch
            {
                // Return whatever collected
            }

            return list;
        }

        public static int RotateBackups(string targetDirectory, int maxKeep = 30)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory) || !Directory.Exists(targetDirectory))
                return 0;

            int deletedCount = 0;
            try
            {
                var files = new DirectoryInfo(targetDirectory).GetFiles("EyeClinic_Backup_*.bak")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (files.Count > maxKeep)
                {
                    for (int i = maxKeep; i < files.Count; i++)
                    {
                        try
                        {
                            files[i].Delete();
                            deletedCount++;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return deletedCount;
        }

        public bool IsWeeklyFlashReminderDue()
        {
            string lastDateStr = _settingsService.GetSetting("Backup_LastUsbReminderDate", "");
            if (string.IsNullOrWhiteSpace(lastDateStr))
                return true;

            DateTime lastDate;
            if (DateTime.TryParse(lastDateStr, out lastDate))
            {
                return (DateTime.Today - lastDate.Date).TotalDays >= 7;
            }

            return true;
        }

        public void MarkWeeklyFlashReminderAcknowledged()
        {
            var dict = new Dictionary<string, string>
            {
                { "Backup_LastUsbReminderDate", DateTime.Today.ToString("yyyy-MM-dd") }
            };
            _settingsService.SaveSettings(dict);
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return string.Format("{0:N2} GB", bytes / (1024.0 * 1024 * 1024));
            if (bytes >= 1024 * 1024)
                return string.Format("{0:N2} MB", bytes / (1024.0 * 1024));
            if (bytes >= 1024)
                return string.Format("{0:N0} KB", bytes / 1024.0);
            return string.Format("{0} B", bytes);
        }

        private void LogAudit(AuditActionType action, string entity, string recordId, string details)
        {
            try
            {
                using (var db = new EyeClinicDbContext())
                {
                    db.AuditLogs.Add(new AuditLog
                    {
                        UserId = SessionContext.CurrentUserId > 0 ? (int?)SessionContext.CurrentUserId : null,
                        Username = SessionContext.CurrentUsername ?? "System",
                        ActionType = action,
                        EntityName = entity,
                        RecordId = recordId,
                        Description = details,
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            catch { }
        }
    }
}
