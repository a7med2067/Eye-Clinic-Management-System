using System;
using System.Data.Entity;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.DTOs;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class DailyClosingService
    {
        public DailyRevenueDto GetDailySummary(DateTime date)
        {
            DateTime dateOnly = date.Date;

            using (var db = new EyeClinicDbContext())
            {
                // Payments issued today that were not refunded today
                var payments = db.Payments
                    .Where(p => DbFunctions.TruncateTime(p.CreatedAt) == dateOnly)
                    .ToList();

                // Refunds executed today (regardless of when original payment was created)
                var refundsExecutedToday = db.Payments
                    .Where(p => p.IsRefunded && p.RefundedAt.HasValue && DbFunctions.TruncateTime(p.RefundedAt.Value) == dateOnly)
                    .ToList();

                decimal totalCash = payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.ActualPrice)
                                  - refundsExecutedToday.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.ActualPrice);

                decimal totalVodafone = payments.Where(p => p.Method == PaymentMethod.VodafoneCash).Sum(p => p.ActualPrice)
                                      - refundsExecutedToday.Where(p => p.Method == PaymentMethod.VodafoneCash).Sum(p => p.ActualPrice);

                decimal totalInstaPay = payments.Where(p => p.Method == PaymentMethod.InstaPay).Sum(p => p.ActualPrice)
                                      - refundsExecutedToday.Where(p => p.Method == PaymentMethod.InstaPay).Sum(p => p.ActualPrice);

                int examCount = payments.Count(p => p.ExamType == ExamType.Exam && !p.IsRefunded);
                int reExamCount = payments.Count(p => p.ExamType == ExamType.ReExam && !p.IsRefunded);
                int urgentCount = payments.Count(p => p.ExamType == ExamType.Urgent && !p.IsRefunded);

                int noShows = db.Appointments.Count(a => DbFunctions.TruncateTime(a.AppointmentDate) == dateOnly && a.Status == AppointmentStatus.NoShow);

                var closing = db.DailyClosings.FirstOrDefault(c => DbFunctions.TruncateTime(c.ClosingDate) == dateOnly);

                return new DailyRevenueDto
                {
                    Date = dateOnly,
                    TotalCash = totalCash < 0 ? 0 : totalCash,
                    TotalVodafoneCash = totalVodafone < 0 ? 0 : totalVodafone,
                    TotalInstaPay = totalInstaPay < 0 ? 0 : totalInstaPay,
                    TotalExamCount = examCount,
                    TotalReExamCount = reExamCount,
                    TotalUrgentCount = urgentCount,
                    RefundedCount = refundsExecutedToday.Count,
                    RefundedAmount = refundsExecutedToday.Sum(p => p.ActualPrice),
                    NoShowCount = noShows,
                    IsClosed = closing != null && !closing.IsReopened,
                    ActualCashCounted = closing?.ActualCash,
                    CashDifference = closing?.CashDifference
                };
            }
        }

        public Result<DailyClosing> CloseDay(DateTime date, decimal actualCash, string notes = null)
        {
            DateTime dateOnly = date.Date;

            using (var db = new EyeClinicDbContext())
            {
                var existing = db.DailyClosings.FirstOrDefault(c => DbFunctions.TruncateTime(c.ClosingDate) == dateOnly);
                if (existing != null && !existing.IsReopened)
                    return Result<DailyClosing>.Failure("تم إقفال حسابات هذا اليوم مسبقاً.");

                var summary = GetDailySummary(dateOnly);
                decimal expectedCash = summary.TotalCash;
                decimal diff = actualCash - expectedCash;

                DailyClosing closing;
                if (existing != null)
                {
                    closing = existing;
                    closing.ExpectedCash = expectedCash;
                    closing.ActualCash = actualCash;
                    closing.CashDifference = diff;
                    closing.VodafoneCashTotal = summary.TotalVodafoneCash;
                    closing.InstaPayTotal = summary.TotalInstaPay;
                    closing.Notes = notes;
                    closing.ClosedByUserId = SessionContext.CurrentUserId > 0 ? SessionContext.CurrentUserId : 1;
                    closing.ClosedAt = DateTime.Now;
                    closing.IsReopened = false;
                }
                else
                {
                    closing = new DailyClosing
                    {
                        ClosingDate = dateOnly,
                        ExpectedCash = expectedCash,
                        ActualCash = actualCash,
                        CashDifference = diff,
                        VodafoneCashTotal = summary.TotalVodafoneCash,
                        InstaPayTotal = summary.TotalInstaPay,
                        Notes = notes,
                        ClosedByUserId = SessionContext.CurrentUserId > 0 ? SessionContext.CurrentUserId : 1,
                        ClosedAt = DateTime.Now,
                        IsReopened = false
                    };
                    db.DailyClosings.Add(closing);
                }

                db.SaveChanges();
                return Result<DailyClosing>.Success(closing);
            }
        }

        public Result ReopenDay(DateTime date, string reason)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("إعادة فتح يوم مقفول مقتصر على حساب الطبيب/المدير فقط.");

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure("سبب فتح اليومية إجباري.");

            DateTime dateOnly = date.Date;

            using (var db = new EyeClinicDbContext())
            {
                var closing = db.DailyClosings.FirstOrDefault(c => DbFunctions.TruncateTime(c.ClosingDate) == dateOnly);
                if (closing == null)
                    return Result.Failure("لم يتم العثور على سجل إقفال لهذا اليوم.");

                closing.IsReopened = true;
                closing.ReopenedByUserId = SessionContext.CurrentUserId;
                closing.ReopenedAt = DateTime.Now;

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = SessionContext.CurrentUserId,
                    Username = SessionContext.CurrentUsername,
                    ActionType = AuditActionType.ReopenDailyClose,
                    EntityName = "DailyClosing",
                    RecordId = closing.Id.ToString(),
                    Description = $"إعادة فتح يوم مقفول بتاريخ {dateOnly:dd/MM/yyyy}. السبب: {reason.Trim()}",
                    Timestamp = DateTime.Now
                });

                db.SaveChanges();
                return Result.Success();
            }
        }

        public DailyArchiveDetailDto GetDailyArchiveDetail(DateTime date)
        {
            DateTime dateOnly = date.Date;

            using (var db = new EyeClinicDbContext())
            {
                var summary = GetDailySummary(dateOnly);

                var closing = db.DailyClosings
                    .Include(c => c.ClosedByUser)
                    .FirstOrDefault(c => DbFunctions.TruncateTime(c.ClosingDate) == dateOnly);

                var appts = db.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Payments)
                    .Include(a => a.Visits)
                    .Where(a => DbFunctions.TruncateTime(a.AppointmentDate) == dateOnly && !a.Patient.IsArchived)
                    .OrderBy(a => a.TurnNumber)
                    .ToList();

                var visits = db.Visits
                    .Include(v => v.Patient)
                    .Include("Prescriptions.Items")
                    .Where(v => DbFunctions.TruncateTime(v.VisitDate) == dateOnly && !v.IsArchived)
                    .OrderBy(v => v.Id)
                    .ToList();

                var detail = new DailyArchiveDetailDto
                {
                    Date = dateOnly,
                    Summary = summary,
                    ClosedByName = closing != null && closing.ClosedByUser != null ? closing.ClosedByUser.FullName : (closing != null ? "مستخدم" : "لم يتم الإقفال"),
                    ClosedAt = closing != null ? (DateTime?)closing.ClosedAt : null,
                    ClosingNotes = closing != null ? closing.Notes : ""
                };

                foreach (var a in appts)
                {
                    var v = a.Visits != null ? a.Visits.FirstOrDefault() : null;
                    var p = a.Payment;

                    detail.Patients.Add(new DailyPatientRecordDto
                    {
                        AppointmentId = a.Id,
                        PatientId = a.PatientId,
                        TurnNumber = a.TurnNumber,
                        FileNumber = a.Patient.FileNumber,
                        FullName = a.Patient.FullName,
                        Age = a.Patient.CalculatedAge,
                        Mobile = a.Patient.Mobile,
                        ExamType = p != null ? p.ExamType : (a.IsUrgent ? ExamType.Urgent : ExamType.Exam),
                        AmountPaid = p != null ? p.ActualPrice : 0,
                        PaymentMethod = p != null ? (PaymentMethod?)p.Method : null,
                        Status = a.Status,
                        Diagnosis = v != null ? v.Diagnosis : "",
                        ChiefComplaint = v != null ? v.ChiefComplaint : "",
                        Time = a.CreatedAt
                    });
                }

                foreach (var v in visits)
                {
                    if (v.Prescriptions != null)
                    {
                        foreach (var rx in v.Prescriptions)
                        {
                            if (rx.Items != null)
                            {
                                foreach (var item in rx.Items.OrderBy(i => i.OrderIndex))
                                {
                                    detail.Prescriptions.Add(new DailyPrescriptionItemRecordDto
                                    {
                                        VisitId = v.Id,
                                        PatientName = v.Patient != null ? v.Patient.FullName : "",
                                        FileNumber = v.Patient != null ? v.Patient.FileNumber : "",
                                        Diagnosis = v.Diagnosis,
                                        MedicineName = item.MedicineName,
                                        DoseInstructions = item.DoseInstructions
                                    });
                                }
                            }
                        }
                    }
                }

                return detail;
            }
        }
    }
}
