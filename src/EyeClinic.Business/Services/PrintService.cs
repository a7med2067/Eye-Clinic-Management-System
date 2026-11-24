using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Text.RegularExpressions;
using System.Linq;
using EyeClinic.Core.DTOs;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class PrintCalibrationConfig
    {
        public float OffsetX_MM { get; set; } = 0;
        public float OffsetY_MM { get; set; } = 0;
        public float Name_X_MM { get; set; } = 35;
        public float Name_Y_MM { get; set; } = 38;
        public float Age_X_MM { get; set; } = 110;
        public float Age_Y_MM { get; set; } = 38;
        public float Diagnosis_X_MM { get; set; } = 42;
        public float Diagnosis_Y_MM { get; set; } = 46;
        public float Date_X_MM { get; set; } = 112;
        public float Date_Y_MM { get; set; } = 46;
        public float Box_X_MM { get; set; } = 17;
        public float Box_Y_MM { get; set; } = 69;
        public float Box_Width_MM { get; set; } = 130;
        public float Box_Height_MM { get; set; } = 130;
    }

    public class PrintService
    {
        private const float MmToHundredthsInch = 3.937f; // 1 mm = 0.03937 inches = 3.937 hundredths of an inch

        public PrintCalibrationConfig LoadCalibrationConfig()
        {
            var config = new PrintCalibrationConfig();
            using (var db = new EyeClinicDbContext())
            {
                var settings = db.ClinicSettings.Where(s => s.GroupName == "Calibration").ToList();
                foreach (var s in settings)
                {
                    if (float.TryParse(s.SettingValue, out float val))
                    {
                        switch (s.SettingKey)
                        {
                            case "Print_Rx_Offset_X_MM": config.OffsetX_MM = val; break;
                            case "Print_Rx_Offset_Y_MM": config.OffsetY_MM = val; break;
                            case "Print_Rx_Name_X_MM": config.Name_X_MM = val; break;
                            case "Print_Rx_Name_Y_MM": config.Name_Y_MM = val; break;
                            case "Print_Rx_Age_X_MM": config.Age_X_MM = val; break;
                            case "Print_Rx_Age_Y_MM": config.Age_Y_MM = val; break;
                            case "Print_Rx_Diagnosis_X_MM": config.Diagnosis_X_MM = val; break;
                            case "Print_Rx_Diagnosis_Y_MM": config.Diagnosis_Y_MM = val; break;
                            case "Print_Rx_Date_X_MM": config.Date_X_MM = val; break;
                            case "Print_Rx_Date_Y_MM": config.Date_Y_MM = val; break;
                            case "Print_Rx_Box_X_MM": config.Box_X_MM = val; break;
                            case "Print_Rx_Box_Y_MM": config.Box_Y_MM = val; break;
                            case "Print_Rx_Box_Width_MM": config.Box_Width_MM = val; break;
                            case "Print_Rx_Box_Height_MM": config.Box_Height_MM = val; break;
                        }
                    }
                }
            }
            return config;
        }

        public PrintDocument CreatePrescriptionPrintDocument(PrescriptionPrintDto dto, PrintCalibrationConfig config = null)
        {
            if (config == null)
            {
                config = LoadCalibrationConfig();
            }

            var doc = new PrintDocument();
            doc.DocumentName = $"Prescription_{dto.PatientName}_{dto.FormattedDate}";

            // Custom paper size: 16.7 cm x 24 cm -> 657 x 945 hundredths of an inch
            int paperWidth = (int)Math.Round(167f * MmToHundredthsInch);
            int paperHeight = (int)Math.Round(240f * MmToHundredthsInch);
            doc.DefaultPageSettings.PaperSize = new PaperSize("EyeClinic_Custom_Rx", paperWidth, paperHeight);
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            doc.PrintPage += (sender, e) =>
            {
                RenderPrescriptionPage(e.Graphics, dto, config);
                e.HasMorePages = false;
            };

            return doc;
        }

        public void RenderPrescriptionPage(Graphics g, PrescriptionPrintDto dto, PrintCalibrationConfig config)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float offsetX = config.OffsetX_MM * MmToHundredthsInch;
            float offsetY = config.OffsetY_MM * MmToHundredthsInch;

            // 1. Patient Name (Full_Name)
            float nameX = (config.Name_X_MM * MmToHundredthsInch) + offsetX;
            float nameY = (config.Name_Y_MM * MmToHundredthsInch) + offsetY;
            float nameWidth = 60f * MmToHundredthsInch; // ~6 cm
            float nameHeight = 7f * MmToHundredthsInch;

            using (var font = new Font("Arial", 11f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Black))
            {
                var sf = GetStringFormat(dto.PatientName, centerHorizontally: true);
                var rect = new RectangleF(nameX, nameY, nameWidth, nameHeight);
                g.DrawString(dto.PatientName ?? "", font, brush, rect, sf);
            }

            // 2. Age
            float ageX = (config.Age_X_MM * MmToHundredthsInch) + offsetX;
            float ageY = (config.Age_Y_MM * MmToHundredthsInch) + offsetY;
            float ageWidth = 37f * MmToHundredthsInch; // ~3.7 cm
            float ageHeight = 7f * MmToHundredthsInch;

            using (var font = new Font("Arial", 10.5f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Black))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                var rect = new RectangleF(ageX, ageY, ageWidth, ageHeight);
                string ageStr = string.IsNullOrWhiteSpace(dto.Age) ? "" : $"{dto.Age} سنة";
                g.DrawString(ageStr, font, brush, rect, sf);
            }

            // 3. Diagnosis
            float diagX = (config.Diagnosis_X_MM * MmToHundredthsInch) + offsetX;
            float diagY = (config.Diagnosis_Y_MM * MmToHundredthsInch) + offsetY;
            float diagWidth = 53f * MmToHundredthsInch; // ~5.3 cm
            float diagHeight = 7f * MmToHundredthsInch;

            using (var font = new Font("Arial", 10f, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.Black))
            {
                var sf = GetStringFormat(dto.Diagnosis, centerHorizontally: true);
                var rect = new RectangleF(diagX, diagY, diagWidth, diagHeight);
                g.DrawString(dto.Diagnosis ?? "", font, brush, rect, sf);
            }

            // 4. Date (DD/MM/YYYY)
            float dateX = (config.Date_X_MM * MmToHundredthsInch) + offsetX;
            float dateY = (config.Date_Y_MM * MmToHundredthsInch) + offsetY;
            float dateWidth = 38f * MmToHundredthsInch;
            float dateHeight = 7f * MmToHundredthsInch;

            using (var font = new Font("Arial", 10f, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.Black))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                var rect = new RectangleF(dateX, dateY, dateWidth, dateHeight);
                g.DrawString(dto.FormattedDate, font, brush, rect, sf);
            }

            // 5. Medicines Box (13 x 13 cm)
            float boxX = (config.Box_X_MM * MmToHundredthsInch) + offsetX;
            float boxY = (config.Box_Y_MM * MmToHundredthsInch) + offsetY;
            float boxW = config.Box_Width_MM * MmToHundredthsInch;
            float boxH = config.Box_Height_MM * MmToHundredthsInch;

            RenderMedicinesBox(g, dto.Medicines, boxX, boxY, boxW, boxH);

            // 6. Engineer Signature (Bottom-Right corner of 13 x 13 cm box)
            float footerW = 160f;
            float footerH = 26f;
            float footerX = boxX + boxW - footerW - 6f;
            float footerY = boxY + boxH - footerH - 4f;

            using (var footerFont = new Font("Segoe UI", 7f, FontStyle.Regular))
            using (var footerBrush = new SolidBrush(Color.FromArgb(70, 70, 70)))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Far,      // Right-aligned
                    LineAlignment = StringAlignment.Far   // Bottom-aligned
                };
                var footerRect = new RectangleF(footerX, footerY, footerW, footerH);
                g.DrawString("ENG.Ahmed Adel\n01116887242", footerFont, footerBrush, footerRect, sf);
            }
        }

        private void RenderMedicinesBox(Graphics g, System.Collections.Generic.List<PrescriptionPrintItemDto> items, float x, float y, float w, float h)
        {
            if (items == null || items.Count == 0) return;

            // Auto-scale font if items count is large
            float medFontSize = 10.5f;
            float doseFontSize = 9.0f;
            float itemSpacing = 18f;

            if (items.Count > 6)
            {
                medFontSize = 9.5f;
                doseFontSize = 8.0f;
                itemSpacing = 15f;
            }
            if (items.Count > 9)
            {
                medFontSize = 8.5f;
                doseFontSize = 7.5f;
                itemSpacing = 13f;
            }

            float currentY = y + 10f;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (currentY + 25f > (y + h - 28f))
                {
                    // Exceeded printable box height (reserve space for bottom-right footer)
                    break;
                }

                // Medicine Name
                using (var medFont = new Font("Segoe UI", medFontSize, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(20, 20, 20)))
                {
                    var sf = GetStringFormat(item.MedicineName, centerHorizontally: false);
                    string text = $"{i + 1}.  {item.MedicineName}";
                    var rect = new RectangleF(x + 5f, currentY, w - 10f, 18f);
                    g.DrawString(text, medFont, brush, rect, sf);
                }
                currentY += itemSpacing;

                // Dose instructions
                if (!string.IsNullOrWhiteSpace(item.DoseInstructions))
                {
                    using (var doseFont = new Font("Segoe UI", doseFontSize, FontStyle.Regular))
                    using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
                    {
                        var sf = GetStringFormat(item.DoseInstructions, centerHorizontally: false);
                        string doseText = $"     - {item.DoseInstructions}";
                        var rect = new RectangleF(x + 5f, currentY, w - 10f, 16f);
                        g.DrawString(doseText, doseFont, brush, rect, sf);
                    }
                    currentY += (itemSpacing - 2f);
                }

                currentY += 4f; // margin between items
            }
        }

        public PrintDocument CreateReceiptPrintDocument(Receipt receipt)
        {
            var doc = new PrintDocument();
            doc.DocumentName = $"Receipt_{receipt.ReceiptNumber}";

            // 80mm thermal paper width ~ 3.15 inches -> ~315 hundredths of an inch
            doc.DefaultPageSettings.PaperSize = new PaperSize("Thermal_80mm", 315, 600);
            doc.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

            doc.PrintPage += (sender, e) =>
            {
                RenderThermalReceipt(e.Graphics, receipt);
                e.HasMorePages = false;
            };

            return doc;
        }

        private void RenderThermalReceipt(Graphics g, Receipt receipt)
        {
            float y = 15f;
            float width = 295f;

            using (var db = new EyeClinicDbContext())
            {
                var pay = db.Payments
                    .Include("Appointment.Patient")
                    .Include("CreatedByUser")
                    .FirstOrDefault(p => p.Id == receipt.PaymentId);

                string clinicName = db.ClinicSettings.Find("Clinic_Name")?.SettingValue ?? "عيادة العيون التخصصية";
                string doctorName = db.ClinicSettings.Find("Clinic_DoctorName")?.SettingValue ?? "د. استشاري العيون";
                string phone = db.ClinicSettings.Find("Clinic_Phone")?.SettingValue ?? "";

                using (var centerSf = new StringFormat { Alignment = StringAlignment.Center })
                using (var rightSf = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft })
                using (var boldFont = new Font("Arial", 12f, FontStyle.Bold))
                using (var normFont = new Font("Arial", 9.5f, FontStyle.Regular))
                using (var smallFont = new Font("Arial", 8.5f, FontStyle.Regular))
                using (var brush = new SolidBrush(Color.Black))
                {
                    // Header
                    g.DrawString(clinicName, boldFont, brush, new RectangleF(0, y, width, 25f), centerSf);
                    y += 24f;
                    g.DrawString(doctorName, normFont, brush, new RectangleF(0, y, width, 20f), centerSf);
                    y += 20f;
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        g.DrawString($"هاتف: {phone}", smallFont, brush, new RectangleF(0, y, width, 18f), centerSf);
                        y += 18f;
                    }

                    // Separator
                    g.DrawLine(Pens.Black, 10, y, width - 10, y);
                    y += 8f;

                    if (pay != null && pay.IsRefunded)
                    {
                        using (var refundFont = new Font("Arial", 14f, FontStyle.Bold))
                        using (var redBrush = new SolidBrush(Color.Red))
                        {
                            g.DrawString("*** إيصال ملغي (مسترد) ***", refundFont, redBrush, new RectangleF(0, y, width, 26f), centerSf);
                            y += 26f;
                        }
                    }

                    // Receipt Info
                    g.DrawString($"رقم الإيصال: #{receipt.ReceiptNumber}", normFont, brush, new RectangleF(10, y, width - 20, 20f), rightSf);
                    y += 20f;
                    g.DrawString($"التاريخ: {receipt.IssuedAt:dd/MM/yyyy  hh:mm tt}", smallFont, brush, new RectangleF(10, y, width - 20, 18f), rightSf);
                    y += 18f;

                    if (pay != null)
                    {
                        g.DrawString($"المريض: {pay.Patient.FullName}", normFont, brush, new RectangleF(10, y, width - 20, 20f), rightSf);
                        y += 20f;
                        g.DrawString($"رقم الملف: {pay.Patient.FileNumber}  |  رقم الدور: {pay.Appointment.TurnNumber}", normFont, brush, new RectangleF(10, y, width - 20, 20f), rightSf);
                        y += 20f;
                        g.DrawString($"نوع الكشف: {pay.ExamType}", normFont, brush, new RectangleF(10, y, width - 20, 20f), rightSf);
                        y += 20f;
                        g.DrawString($"طريقة الدفع: {pay.Method}", normFont, brush, new RectangleF(10, y, width - 20, 20f), rightSf);
                        y += 20f;

                        // Amount
                        using (var priceFont = new Font("Arial", 13f, FontStyle.Bold))
                        {
                            g.DrawString($"المبلغ المدفوع: {pay.ActualPrice:N2} جنيه", priceFont, brush, new RectangleF(10, y, width - 20, 25f), rightSf);
                            y += 26f;
                        }

                        g.DrawString($"المستلم: {pay.CreatedByUser?.FullName ?? "السكرتارية"}", smallFont, brush, new RectangleF(10, y, width - 20, 18f), rightSf);
                        y += 20f;
                    }

                    g.DrawLine(Pens.Black, 10, y, width - 10, y);
                    y += 8f;
                    g.DrawString("مع أطيب تمنياتنا بالشفاء العاجل", smallFont, brush, new RectangleF(0, y, width, 18f), centerSf);
                }
            }
        }

        private StringFormat GetStringFormat(string text, bool centerHorizontally)
        {
            bool isArabic = ContainsArabic(text);
            var sf = new StringFormat();

            if (isArabic)
            {
                sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                sf.Alignment = centerHorizontally ? StringAlignment.Center : StringAlignment.Near;
            }
            else
            {
                sf.FormatFlags = 0;
                sf.Alignment = centerHorizontally ? StringAlignment.Center : StringAlignment.Near;
            }

            sf.LineAlignment = StringAlignment.Center;
            return sf;
        }

        private bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            return Regex.IsMatch(text, @"[\u0600-\u06FF]");
        }
    }
}
