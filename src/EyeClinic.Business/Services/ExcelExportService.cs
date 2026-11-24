using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using EyeClinic.Core.Common;

namespace EyeClinic.Business.Services
{
    public class ExcelExportService
    {
        public Result ExportDataGridViewToCsv(DataGridView dgv, string filePath)
        {
            if (dgv == null || dgv.Rows.Count == 0)
                return Result.Failure("لا توجد بيانات متاحة للتصدير في الجدول.");

            if (string.IsNullOrWhiteSpace(filePath))
                return Result.Failure("مسار حفظ الملف غير صالح.");

            try
            {
                // Ensure target directory exists
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var sb = new StringBuilder();

                // 1. Collect Visible Columns in Display Index order
                var columns = new List<DataGridViewColumn>();
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible && !string.IsNullOrEmpty(col.HeaderText))
                    {
                        columns.Add(col);
                    }
                }

                // In WinForms RTL, DisplayIndex order matches visual columns
                columns.Sort((a, b) => a.DisplayIndex.CompareTo(b.DisplayIndex));

                // 2. Write Headers
                var headerValues = new List<string>();
                foreach (var col in columns)
                {
                    headerValues.Add(EscapeCsvValue(col.HeaderText));
                }
                sb.AppendLine(string.Join(",", headerValues.ToArray()));

                // 3. Write Data Rows
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;

                    var rowValues = new List<string>();
                    foreach (var col in columns)
                    {
                        var cell = row.Cells[col.Index];
                        string val = cell.FormattedValue != null ? cell.FormattedValue.ToString() : "";
                        rowValues.Add(EscapeCsvValue(val));
                    }
                    sb.AppendLine(string.Join(",", rowValues.ToArray()));
                }

                // Write file with UTF-8 BOM (Byte Order Mark) so Microsoft Excel opens Arabic text flawlessly!
                File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure("فشل تصدير البيانات إلى ملف: " + ex.Message);
            }
        }

        public Result ExportListToCsv<T>(IEnumerable<T> data, string filePath, Dictionary<string, string> columnHeaders = null)
        {
            if (data == null)
                return Result.Failure("لا توجد بيانات متاحة للتصدير.");

            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var sb = new StringBuilder();
                var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                var activeProps = new List<PropertyInfo>();
                var headers = new List<string>();

                if (columnHeaders != null && columnHeaders.Count > 0)
                {
                    foreach (var kvp in columnHeaders)
                    {
                        var prop = typeof(T).GetProperty(kvp.Key);
                        if (prop != null)
                        {
                            activeProps.Add(prop);
                            headers.Add(EscapeCsvValue(kvp.Value));
                        }
                    }
                }
                else
                {
                    foreach (var prop in props)
                    {
                        activeProps.Add(prop);
                        headers.Add(EscapeCsvValue(prop.Name));
                    }
                }

                sb.AppendLine(string.Join(",", headers.ToArray()));

                foreach (var item in data)
                {
                    var rowValues = new List<string>();
                    foreach (var prop in activeProps)
                    {
                        object val = prop.GetValue(item, null);
                        string str = "";
                        if (val != null)
                        {
                            if (val is DateTime dt)
                                str = dt.ToString("yyyy-MM-dd HH:mm");
                            else if (val is decimal dec)
                                str = dec.ToString("N2");
                            else
                                str = val.ToString();
                        }
                        rowValues.Add(EscapeCsvValue(str));
                    }
                    sb.AppendLine(string.Join(",", rowValues.ToArray()));
                }

                File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure("فشل تصدير البيانات: " + ex.Message);
            }
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            // Normalize newlines to spaces so CSV records don't break across lines
            string clean = value.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            // If it contains quote or comma, escape quotes by doubling them
            if (clean.Contains("\"") || clean.Contains(",") || clean.Contains(";"))
            {
                clean = clean.Replace("\"", "\"\"");
                return "\"" + clean + "\"";
            }

            return "\"" + clean + "\"";
        }
    }
}
