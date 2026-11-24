using System;
using System.Data.Entity;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class PaymentService
    {
        public (decimal examPrice, decimal reExamPrice, decimal urgentPrice, int reExamValidDays) GetPricingConfig()
        {
            using (var db = new EyeClinicDbContext())
            {
                decimal exam = 300;
                decimal reExam = 50;
                decimal urgent = 450;
                int days = 14;

                var sExam = db.ClinicSettings.Find("Price_Exam");
                if (sExam != null && decimal.TryParse(sExam.SettingValue, out decimal p1)) exam = p1;

                var sReExam = db.ClinicSettings.Find("Price_ReExam");
                if (sReExam != null && decimal.TryParse(sReExam.SettingValue, out decimal p2)) reExam = p2;

                var sUrgent = db.ClinicSettings.Find("Price_Urgent");
                if (sUrgent != null && decimal.TryParse(sUrgent.SettingValue, out decimal p3)) urgent = p3;

                var sDays = db.ClinicSettings.Find("ReExam_ValidDays");
                if (sDays != null && int.TryParse(sDays.SettingValue, out int d)) days = d;

                return (exam, reExam, urgent, days);
            }
        }

        public (bool isEligible, int? daysSinceLast, int? originalPaymentId) CheckReExamEligibility(int patientId)
        {
            using (var db = new EyeClinicDbContext())
            {
                var pricing = GetPricingConfig();

                var lastPaidExam = db.Payments
                    .Where(p => p.PatientId == patientId && !p.IsRefunded && p.ExamType == ExamType.Exam)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();

                if (lastPaidExam == null)
                    return (false, null, null);

                int days = (int)(DateTime.Today - lastPaidExam.CreatedAt.Date).TotalDays;
                if (days >= 0 && days <= pricing.reExamValidDays)
                {
                    return (true, days, lastPaidExam.Id);
                }

                return (false, days, null);
            }
        }

        public Result<Receipt> ProcessPayment(
            int appointmentId,
            ExamType examType,
            decimal actualPrice,
            PaymentMethod method,
            string referenceNumber = null)
        {
            using (var db = new EyeClinicDbContext())
            {
                var appt = db.Appointments.Include(a => a.Patient).FirstOrDefault(a => a.Id == appointmentId);
                if (appt == null)
                    return Result<Receipt>.Failure("الحجز غير موجود.");

                // Check if day is already closed
                DateTime apptDate = appt.AppointmentDate.Date;
                bool isClosed = db.DailyClosings.Any(c => DbFunctions.TruncateTime(c.ClosingDate) == apptDate && !c.IsReopened);
                if (isClosed)
                    return Result<Receipt>.Failure("لا يمكن تسجيل دفع في يوم تم إقفال حساباته.");

                // Check if already paid
                var existingPay = db.Payments.FirstOrDefault(p => p.AppointmentId == appointmentId && !p.IsRefunded);
                if (existingPay != null)
                    return Result<Receipt>.Failure("تم تسجيل الدفع لهذا الحجز مسبقاً.");

                var pricing = GetPricingConfig();
                decimal standardPrice;
                int? originalExamId = null;

                if (examType == ExamType.Exam)
                {
                    standardPrice = pricing.examPrice;
                }
                else if (examType == ExamType.ReExam)
                {
                    standardPrice = pricing.reExamPrice;
                    var elig = CheckReExamEligibility(appt.PatientId);
                    if (elig.isEligible)
                    {
                        originalExamId = elig.originalPaymentId;
                    }
                }
                else
                {
                    standardPrice = pricing.urgentPrice;
                    appt.IsUrgent = true;
                }

                var payment = new Payment
                {
                    AppointmentId = appointmentId,
                    PatientId = appt.PatientId,
                    ExamType = examType,
                    StandardPrice = standardPrice,
                    ActualPrice = actualPrice,
                    Method = method,
                    ReferenceNumber = referenceNumber,
                    OriginalExamPaymentId = originalExamId,
                    CreatedByUserId = SessionContext.CurrentUserId > 0 ? SessionContext.CurrentUserId : 1,
                    CreatedAt = DateTime.Now
                };

                db.Payments.Add(payment);

                // Update appointment status to Waiting (entered queue)
                appt.Status = AppointmentStatus.Waiting;

                // Gapless sequential receipt numbering
                long maxReceipt = db.Receipts.Select(r => (long?)r.ReceiptNumber).Max() ?? 10000;
                long nextReceiptNumber = maxReceipt + 1;

                var receipt = new Receipt
                {
                    Payment = payment,
                    ReceiptNumber = nextReceiptNumber,
                    IssuedAt = DateTime.Now,
                    PrintCount = 0
                };

                db.Receipts.Add(receipt);

                // If price was modified by secretary, log explicit audit
                if (actualPrice != standardPrice)
                {
                    db.AuditLogs.Add(new AuditLog
                    {
                        UserId = SessionContext.CurrentUserId > 0 ? (int?)SessionContext.CurrentUserId : null,
                        Username = SessionContext.CurrentUsername,
                        ActionType = AuditActionType.PriceOverride,
                        EntityName = "Payment",
                        RecordId = appt.Id.ToString(),
                        OldValuesJson = $"{{\"StandardPrice\": {standardPrice}}}",
                        NewValuesJson = $"{{\"ActualPrice\": {actualPrice}}}",
                        Description = $"تعديل سعر {examType} للمريض {appt.Patient.FullName} من {standardPrice} إلى {actualPrice}",
                        Timestamp = DateTime.Now
                    });
                }

                db.SaveChanges();

                return Result<Receipt>.Success(receipt);
            }
        }

        public Result RefundPayment(int paymentId, string refundReason)
        {
            if (string.IsNullOrWhiteSpace(refundReason))
                return Result.Failure("سبب الاسترداد إجباري ولا يمكن إتمام العملية بدونه.");

            using (var db = new EyeClinicDbContext())
            {
                var payment = db.Payments
                    .Include(p => p.Appointment)
                    .Include(p => p.Patient)
                    .FirstOrDefault(p => p.Id == paymentId);

                if (payment == null)
                    return Result.Failure("الدفعة غير موجودة.");

                if (payment.IsRefunded)
                    return Result.Failure("تم استرداد هذه الدفعة مسبقاً.");

                // Permissions check:
                // If patient already entered exam (InExam or Completed), doctor ONLY can refund
                if (payment.Appointment.Status == AppointmentStatus.InExam ||
                    payment.Appointment.Status == AppointmentStatus.Completed)
                {
                    if (!SessionContext.IsDoctor)
                    {
                        return Result.Failure("المريض دخل الكشف بالفعل. عملية الاسترداد بعد دخول الكشف مقتصرة على الطبيب فقط.");
                    }
                }

                payment.IsRefunded = true;
                payment.RefundReason = refundReason.Trim();
                payment.RefundedAt = DateTime.Now;
                payment.RefundedByUserId = SessionContext.CurrentUserId > 0 ? SessionContext.CurrentUserId : 1;

                // Mark appointment cancelled
                payment.Appointment.Status = AppointmentStatus.Cancelled;

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = SessionContext.CurrentUserId > 0 ? (int?)SessionContext.CurrentUserId : null,
                    Username = SessionContext.CurrentUsername,
                    ActionType = AuditActionType.RefundPayment,
                    EntityName = "Payment",
                    RecordId = payment.Id.ToString(),
                    OldValuesJson = $"{{\"IsRefunded\": false, \"Amount\": {payment.ActualPrice}}}",
                    NewValuesJson = $"{{\"IsRefunded\": true, \"Reason\": \"{refundReason.Trim()}\"}}",
                    Description = $"استرداد مبلغ {payment.ActualPrice} ج للمريض {payment.Patient.FullName}. السبب: {refundReason.Trim()}",
                    Timestamp = DateTime.Now
                });

                db.SaveChanges();

                return Result.Success();
            }
        }
    }
}
