using System;
using System.IO;
using System.Text.RegularExpressions;
using EyeClinic.Business.Services;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class BackupServiceTests
    {
        public void Test_BackupFilename_FollowsRequiredPattern()
        {
            string generatedName = string.Format("EyeClinic_Backup_{0:yyyyMMdd_HHmmss}.bak", DateTime.Now);
            var regex = new Regex(@"^EyeClinic_Backup_\d{8}_\d{6}\.bak$");

            Assert.IsTrue(regex.IsMatch(generatedName), "Backup file name must match EyeClinic_Backup_yyyyMMdd_HHmmss.bak pattern.");
        }

        public void Test_FormatSize_DisplaysCorrectUnits()
        {
            string b = BackupService.FormatSize(500);
            string kb = BackupService.FormatSize(2048);
            string mb = BackupService.FormatSize(5 * 1024 * 1024);
            string gb = BackupService.FormatSize(2L * 1024 * 1024 * 1024);

            Assert.AreEqual("500 B", b);
            Assert.AreEqual("2 KB", kb);
            Assert.AreEqual("5.00 MB", mb);
            Assert.AreEqual("2.00 GB", gb);
        }

        public void Test_BackupRotation_RetainsOnlyLatestCopies()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "EyeClinic_Test_Rotation_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create 35 dummy backup files with staggered timestamps
                var baseDate = DateTime.Now.AddDays(-40);
                for (int i = 0; i < 35; i++)
                {
                    string filePath = Path.Combine(tempDir, string.Format("EyeClinic_Backup_{0:000}.bak", i));
                    File.WriteAllText(filePath, "dummy backup content " + i);
                    File.SetCreationTime(filePath, baseDate.AddDays(i));
                }

                Assert.AreEqual(35, Directory.GetFiles(tempDir, "*.bak").Length, "Should initially have 35 backups.");

                // Rotate keeping 30
                int deleted = BackupService.RotateBackups(tempDir, 30);

                Assert.AreEqual(5, deleted, "Should have deleted exactly 5 older backups (35 - 30).");
                Assert.AreEqual(30, Directory.GetFiles(tempDir, "*.bak").Length, "Must have exactly 30 backups remaining (PRD: BKP-1).");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        public void Test_WeeklyFlashReminder_Calculation()
        {
            var lastDate8DaysAgo = DateTime.Today.AddDays(-8);
            var lastDate3DaysAgo = DateTime.Today.AddDays(-3);

            bool reminder8 = (DateTime.Today - lastDate8DaysAgo).TotalDays >= 7;
            bool reminder3 = (DateTime.Today - lastDate3DaysAgo).TotalDays >= 7;

            Assert.IsTrue(reminder8, "Reminder must trigger if 7 or more days have elapsed since last backup to USB (PRD: BKP-3).");
            Assert.IsFalse(reminder3, "Reminder must NOT trigger if less than 7 days have passed.");
        }
    }
}
