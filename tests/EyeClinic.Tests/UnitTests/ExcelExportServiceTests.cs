using System;
using System.Collections.Generic;
using System.IO;
using EyeClinic.Business.Services;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class TestExportItem
    {
        public int TurnNumber { get; set; }
        public string PatientName { get; set; }
        public string Notes { get; set; }
        public decimal Amount { get; set; }
    }

    public class ExcelExportServiceTests
    {
        public void Test_ExportListToCsv_GeneratesUtf8BomHeader()
        {
            var service = new ExcelExportService();
            string tempFile = Path.Combine(Path.GetTempPath(), "test_export_" + Guid.NewGuid().ToString("N") + ".csv");

            try
            {
                var list = new List<TestExportItem>
                {
                    new TestExportItem { TurnNumber = 1, PatientName = "أحمد محمد محمود", Notes = "ملاحظات عادية", Amount = 300.00m },
                    new TestExportItem { TurnNumber = 2, PatientName = "عمر علي, مصطفى", Notes = "مريض \"مستعجل\"\nيعاني من حساسية", Amount = 450.00m }
                };

                var headers = new Dictionary<string, string>
                {
                    { "TurnNumber", "الدور" },
                    { "PatientName", "اسم المريض" },
                    { "Notes", "الملاحظات" },
                    { "Amount", "المدفوع" }
                };

                var res = service.ExportListToCsv(list, tempFile, headers);
                Assert.IsTrue(res.IsSuccess, "CSV export should succeed.");
                Assert.IsTrue(File.Exists(tempFile), "Exported CSV file must exist.");

                // Verify UTF-8 BOM bytes (0xEF, 0xBB, 0xBF)
                byte[] bytes = File.ReadAllBytes(tempFile);
                Assert.IsTrue(bytes.Length >= 3, "File should have at least 3 bytes.");
                Assert.AreEqual((byte)0xEF, bytes[0], "Byte 0 must be 0xEF for UTF-8 BOM.");
                Assert.AreEqual((byte)0xBB, bytes[1], "Byte 1 must be 0xBB for UTF-8 BOM.");
                Assert.AreEqual((byte)0xBF, bytes[2], "Byte 2 must be 0xBF for UTF-8 BOM.");

                // Verify file content contains Arabic headers and escaped quotes
                string content = File.ReadAllText(tempFile);
                Assert.IsTrue(content.Contains("اسم المريض"), "CSV must contain Arabic column header.");
                Assert.IsTrue(content.Contains("\"عمر علي, مصطفى\""), "Commas inside values must be wrapped in quotes.");
                Assert.IsTrue(content.Contains("\"\"مستعجل\"\""), "Quotes inside values must be double-escaped.");
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }
    }
}
