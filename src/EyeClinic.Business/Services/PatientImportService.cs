using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using EyeClinic.Core.Common;
using EyeClinic.Core.DTOs;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class PatientImportService
    {
        private readonly AuditService _auditService;
        private readonly ExcelExportService _excelExportService;

        public PatientImportService()
        {
            _auditService = new AuditService();
            _excelExportService = new ExcelExportService();
        }

        public PatientImportPreviewResult PreviewImport(string filePath, HashSet<string> existingPhoneNumbers = null)
        {
            var result = new PatientImportPreviewResult();

            if (!File.Exists(filePath))
            {
                return result;
            }

            // Read lines with UTF-8
            string[] rawLines;
            using (var reader = new StreamReader(filePath, Encoding.UTF8, true))
            {
                var linesList = new List<string>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        linesList.Add(line);
                    }
                }
                rawLines = linesList.ToArray();
            }

            if (rawLines.Length == 0)
            {
                return result;
            }

            // Detect delimiter from first row (, or ; or \t)
            char delimiter = DetectDelimiter(rawLines[0]);

            var headerTokens = SplitCsvLine(rawLines[0], delimiter);
            var headerMap = MapHeaders(headerTokens);

            // Fetch existing phones from DB for duplicate detection
            HashSet<string> existingMobiles = existingPhoneNumbers;
            if (existingMobiles == null)
            {
                existingMobiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using (var db = new EyeClinicDbContext())
                    {
                        var mobilesList = db.Patients
                            .Where(p => !p.IsArchived && p.Mobile != null)
                            .Select(p => p.Mobile.Trim())
                            .ToList();
                        existingMobiles = new HashSet<string>(mobilesList, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // DB offline or running in test context without DB connection
                }
            }

            var seenFileMobiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < rawLines.Length; i++)
            {
                string rawLine = rawLines[i].Trim();
                if (string.IsNullOrEmpty(rawLine))
                    continue;

                var fields = SplitCsvLine(rawLine, delimiter);
                var row = ParseRow(fields, headerMap, i + 1);

                // Validation rules
                if (string.IsNullOrWhiteSpace(row.FullName))
                {
                    row.IsValid = false;
                    row.ErrorMessage = "اسم المريض مطلوب ولا يمكن أن يكون فارغاً.";
                }
                else if (string.IsNullOrWhiteSpace(row.Mobile))
                {
                    row.IsValid = false;
                    row.ErrorMessage = "رقم هاتف المريض مطلوب.";
                }
                else
                {
                    string normMobile = row.Mobile.Trim();
                    if (existingMobiles.Contains(normMobile))
                    {
                        row.IsDuplicateInDb = true;
                        row.IsValid = true;
                        row.ErrorMessage = "تنبيه: مسجل مسبقاً في قاعدة البيانات بنفس رقم الهاتف.";
                    }
                    else if (seenFileMobiles.Contains(normMobile))
                    {
                        row.IsValid = false;
                        row.ErrorMessage = "خطأ: مكرر بنفس رقم الهاتف داخل ملف الاستيراد.";
                    }
                    else
                    {
                        row.IsValid = true;
                        seenFileMobiles.Add(normMobile);
                    }
                }

                result.Rows.Add(row);
            }

            result.TotalRows = result.Rows.Count;
            result.ValidRowsCount = result.Rows.Count(r => r.IsValid && !r.IsDuplicateInDb);
            result.DuplicateRowsCount = result.Rows.Count(r => r.IsDuplicateInDb);
            result.ErrorRowsCount = result.Rows.Count(r => !r.IsValid);

            return result;
        }

        public PatientImportCommitResult CommitImport(List<ImportPatientRow> rowsToCommit, bool skipDuplicates, int currentUserId)
        {
            var result = new PatientImportCommitResult();

            if (!SessionContext.IsDoctor)
            {
                result.Success = false;
                result.ErrorMessage = "عملية استيراد المرضى مخصصة لحساب الطبيب فقط.";
                return result;
            }

            var eligible = rowsToCommit
                .Where(r => r.IsValid && (!r.IsDuplicateInDb || !skipDuplicates))
                .ToList();

            if (eligible.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "لا توجد سجلات صالحة للاستيراد بعد تصفية الأخطاء والمكررات.";
                return result;
            }

            try
            {
                using (var db = new EyeClinicDbContext())
                using (var tx = db.Database.BeginTransaction())
                {
                    // Calculate starting file number
                    int maxNumber = 1000;
                    var fileNumbers = db.Patients.Select(p => p.FileNumber).ToList();
                    foreach (var fn in fileNumbers)
                    {
                        int num;
                        if (int.TryParse(fn, out num) && num > maxNumber)
                        {
                            maxNumber = num;
                        }
                    }

                    int count = 0;
                    foreach (var r in eligible)
                    {
                        maxNumber++;
                        var patient = new Patient
                        {
                            FileNumber = maxNumber.ToString(),
                            FullName = r.FullName.Trim(),
                            Mobile = r.Mobile.Trim(),
                            NationalId = string.IsNullOrWhiteSpace(r.NationalId) ? null : r.NationalId.Trim(),
                            Gender = string.IsNullOrWhiteSpace(r.Gender) ? "غير محدد" : r.Gender,
                            BirthDate = r.BirthDate,
                            Address = string.IsNullOrWhiteSpace(r.Address) ? null : r.Address.Trim(),
                            ChronicDiseases = string.IsNullOrWhiteSpace(r.ChronicDiseases) ? null : r.ChronicDiseases.Trim(),
                            DrugAllergies = string.IsNullOrWhiteSpace(r.DrugAllergies) ? null : r.DrugAllergies.Trim(),
                            PastSurgeries = string.IsNullOrWhiteSpace(r.PastSurgeries) ? null : r.PastSurgeries.Trim(),
                            GeneralNotes = string.IsNullOrWhiteSpace(r.GeneralNotes) ? null : r.GeneralNotes.Trim(),
                            IsArchived = false,
                            CreatedAt = DateTime.Now
                        };

                        db.Patients.Add(patient);
                        count++;
                    }

                    db.AuditLogs.Add(new AuditLog
                    {
                        UserId = currentUserId > 0 ? (int?)currentUserId : null,
                        Username = SessionContext.CurrentUsername ?? "Doctor",
                        ActionType = AuditActionType.ImportPatients,
                        EntityName = "Patient",
                        Description = string.Format("استيراد مجمع لـ {0} مريض من ملف خارجي.", count),
                        Timestamp = DateTime.Now
                    });

                    db.SaveChanges();
                    tx.Commit();

                    result.Success = true;
                    result.ImportedCount = count;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "حدث خطأ أثناء حفظ المرضى في قاعدة البيانات: " + ex.Message;
                return result;
            }
        }

        public Result ExportErrorsToCsv(List<ImportPatientRow> errorRows, string exportFilePath)
        {
            var list = new List<ImportPatientRow>();
            foreach (var r in errorRows)
            {
                list.Add(r);
            }

            var headers = new Dictionary<string, string>
            {
                { "RowNumber", "رقم السطر" },
                { "FullName", "الاسم" },
                { "Mobile", "الموبايل" },
                { "ErrorMessage", "سبب الخطأ" }
            };

            return _excelExportService.ExportListToCsv(list, exportFilePath, headers);
        }

        private static char DetectDelimiter(string headerLine)
        {
            int commaCount = headerLine.Count(c => c == ',');
            int semicolonCount = headerLine.Count(c => c == ';');
            int tabCount = headerLine.Count(c => c == '\t');

            if (tabCount > commaCount && tabCount > semicolonCount) return '\t';
            if (semicolonCount > commaCount) return ';';
            return ',';
        }

        private static List<string> SplitCsvLine(string line, char delimiter)
        {
            var tokens = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    tokens.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            tokens.Add(current.ToString().Trim());
            return tokens;
        }

        private static Dictionary<string, int> MapHeaders(List<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                string h = headers[i].Trim().ToLowerInvariant();

                if (h.Contains("اسم") || h.Contains("name"))
                    AssignIfNotSet(map, "name", i);
                else if (h.Contains("موبايل") || h.Contains("هاتف") || h.Contains("تليفون") || h.Contains("phone") || h.Contains("mobile"))
                    AssignIfNotSet(map, "mobile", i);
                else if (h.Contains("قومي") || h.Contains("national"))
                    AssignIfNotSet(map, "nationalid", i);
                else if (h.Contains("نوع") || h.Contains("جنس") || h.Contains("gender"))
                    AssignIfNotSet(map, "gender", i);
                else if (h.Contains("ميلاد") || h.Contains("birth") || h.Contains("dob"))
                    AssignIfNotSet(map, "birthdate", i);
                else if (h.Contains("سن") || h.Contains("عمر") || h.Contains("age"))
                    AssignIfNotSet(map, "age", i);
                else if (h.Contains("عنوان") || h.Contains("address") || h.Contains("مدينة") || h.Contains("city"))
                    AssignIfNotSet(map, "address", i);
                else if (h.Contains("مزمن") || h.Contains("chronic"))
                    AssignIfNotSet(map, "chronic", i);
                else if (h.Contains("حساس") || h.Contains("allerg"))
                    AssignIfNotSet(map, "allergy", i);
                else if (h.Contains("سابق") || h.Contains("مرضي") || h.Contains("history"))
                    AssignIfNotSet(map, "history", i);
                else if (h.Contains("ملاحظ") || h.Contains("note"))
                    AssignIfNotSet(map, "notes", i);
            }

            // Fallback for unlabeled columns (Col 0: Name, Col 1: Mobile)
            if (!map.ContainsKey("name") && headers.Count > 0) map["name"] = 0;
            if (!map.ContainsKey("mobile") && headers.Count > 1) map["mobile"] = 1;

            return map;
        }

        private static void AssignIfNotSet(Dictionary<string, int> map, string key, int index)
        {
            if (!map.ContainsKey(key))
            {
                map[key] = index;
            }
        }

        private static ImportPatientRow ParseRow(List<string> fields, Dictionary<string, int> headerMap, int rowNumber)
        {
            var row = new ImportPatientRow { RowNumber = rowNumber };

            string GetVal(string key)
            {
                int idx;
                if (headerMap.TryGetValue(key, out idx) && idx >= 0 && idx < fields.Count)
                {
                    return fields[idx].Trim();
                }
                return "";
            }

            row.FullName = GetVal("name");
            row.Mobile = GetVal("mobile");
            row.NationalId = GetVal("nationalid");
            row.Address = GetVal("address");
            row.ChronicDiseases = GetVal("chronic");
            row.DrugAllergies = GetVal("allergy");
            row.PastSurgeries = GetVal("history");
            row.GeneralNotes = GetVal("notes");

            // Gender
            string genderStr = GetVal("gender").ToLowerInvariant();
            if (genderStr.Contains("أنث") || genderStr.Contains("انث") || genderStr == "f" || genderStr.Contains("female"))
            {
                row.Gender = "أنثى";
            }
            else if (genderStr.Contains("ذكر") || genderStr == "m" || genderStr.Contains("male"))
            {
                row.Gender = "ذكر";
            }
            else
            {
                row.Gender = "غير محدد";
            }

            // BirthDate / Age
            string birthStr = GetVal("birthdate");
            string ageStr = GetVal("age");

            DateTime bDate;
            if (!string.IsNullOrEmpty(birthStr) && DateTime.TryParse(birthStr, out bDate))
            {
                row.BirthDate = bDate;
                int age = DateTime.Today.Year - bDate.Year;
                if (bDate.Date > DateTime.Today.AddYears(-age)) age--;
                row.AgeYears = age;
            }
            else if (!string.IsNullOrEmpty(ageStr))
            {
                int ageVal;
                if (int.TryParse(ageStr, out ageVal) && ageVal >= 0 && ageVal <= 130)
                {
                    row.AgeYears = ageVal;
                    row.BirthDate = new DateTime(DateTime.Today.Year - ageVal, 1, 1);
                }
            }

            return row;
        }
    }
}
