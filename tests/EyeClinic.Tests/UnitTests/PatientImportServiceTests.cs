using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EyeClinic.Business.Services;
using EyeClinic.Core.DTOs;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class PatientImportServiceTests
    {
        public void Test_ImportPreview_ValidatesRequiredFieldsAndHeaders()
        {
            var service = new PatientImportService();
            string tempCsv = Path.Combine(Path.GetTempPath(), "test_import_" + Guid.NewGuid().ToString("N") + ".csv");

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("الاسم,الموبايل,النوع,السن,العنوان");
                sb.AppendLine("محمد أحمد حسن,01012345678,ذكر,35,القاهرة");
                sb.AppendLine(",01198765432,أنثى,28,الجيزة"); // Missing name
                sb.AppendLine("محمود علي,,ذكر,42,الإسكندرية");   // Missing phone
                sb.AppendLine("سارة إبراهيم,01233445566,أنثى,19,المنصورة");

                File.WriteAllText(tempCsv, sb.ToString(), Encoding.UTF8);

                var preview = service.PreviewImport(tempCsv, new HashSet<string>());

                Assert.AreEqual(4, preview.TotalRows, "Total rows parsed should be 4");
                Assert.AreEqual(2, preview.ValidRowsCount, "Valid rows count should be 2");
                Assert.AreEqual(2, preview.ErrorRowsCount, "Error rows count should be 2");

                var row2 = preview.Rows[1];
                Assert.IsFalse(row2.IsValid, "Row 2 without name should be invalid");
                Assert.IsTrue(row2.ErrorMessage.Contains("اسم المريض"), "Row 2 error should mention patient name");

                var row3 = preview.Rows[2];
                Assert.IsFalse(row3.IsValid, "Row 3 without phone should be invalid");
                Assert.IsTrue(row3.ErrorMessage.Contains("هاتف"), "Row 3 error should mention phone");
            }
            finally
            {
                if (File.Exists(tempCsv)) File.Delete(tempCsv);
            }
        }

        public void Test_ImportPreview_DetectsDuplicatePhoneNumbersInFile()
        {
            var service = new PatientImportService();
            string tempCsv = Path.Combine(Path.GetTempPath(), "test_dup_file_" + Guid.NewGuid().ToString("N") + ".csv");

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("الاسم,الموبايل,النوع");
                sb.AppendLine("علي كمال,01001112233,ذكر");
                sb.AppendLine("كمال علي,01001112233,ذكر"); // Duplicate phone

                File.WriteAllText(tempCsv, sb.ToString(), Encoding.UTF8);

                var preview = service.PreviewImport(tempCsv, new HashSet<string>());

                Assert.AreEqual(2, preview.TotalRows, "Total rows should be 2");
                Assert.AreEqual(1, preview.ValidRowsCount, "First row should be valid");
                Assert.AreEqual(1, preview.ErrorRowsCount, "Second row with duplicate phone should be error");
                Assert.IsTrue(preview.Rows[1].ErrorMessage.Contains("مكرر"), "Error message should mention duplicate");
            }
            finally
            {
                if (File.Exists(tempCsv)) File.Delete(tempCsv);
            }
        }

        public void Test_ExportErrorsToCsv_GeneratesUtf8BomFile()
        {
            var service = new PatientImportService();
            string tempOut = Path.Combine(Path.GetTempPath(), "test_err_out_" + Guid.NewGuid().ToString("N") + ".csv");

            try
            {
                var errors = new List<ImportPatientRow>
                {
                    new ImportPatientRow { RowNumber = 3, FullName = "مريض مجهول", Mobile = "", ErrorMessage = "رقم هاتف المريض مطلوب." }
                };

                var res = service.ExportErrorsToCsv(errors, tempOut);
                Assert.IsTrue(res.IsSuccess, "Exporting errors should succeed");
                Assert.IsTrue(File.Exists(tempOut), "Exported error file must exist");

                byte[] bytes = File.ReadAllBytes(tempOut);
                Assert.IsTrue(bytes.Length >= 3, "File should have at least 3 bytes");
                Assert.AreEqual((byte)0xEF, bytes[0], "First byte must be UTF-8 BOM");
                Assert.AreEqual((byte)0xBB, bytes[1], "Second byte must be UTF-8 BOM");
                Assert.AreEqual((byte)0xBF, bytes[2], "Third byte must be UTF-8 BOM");
            }
            finally
            {
                if (File.Exists(tempOut)) File.Delete(tempOut);
            }
        }
    }
}
