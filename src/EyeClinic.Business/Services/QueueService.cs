using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.DTOs;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class QueueService
    {
        public Result<Appointment> BookAppointment(int patientId, DateTime targetDate, bool isUrgent = false, string notes = null)
        {
            DateTime dateOnly = targetDate.Date;
            DateTime today = DateTime.Today;

            if (dateOnly < today)
                return Result<Appointment>.Failure("لا يمكن الحجز في تاريخ ماضٍ.");

            if ((dateOnly - today).TotalDays > 3)
                return Result<Appointment>.Failure("الحجز المسبق متاح حتى 3 أيام قادمة فقط.");

            using (var db = new EyeClinicDbContext())
            {
                var patient = db.Patients.FirstOrDefault(p => p.Id == patientId && !p.IsArchived);
                if (patient == null)
                    return Result<Appointment>.Failure("المريض غير موجود.");

                // Check if patient already has an active appointment for this day
                bool alreadyBooked = db.Appointments.Any(a =>
                    a.PatientId == patientId &&
                    DbFunctions.TruncateTime(a.AppointmentDate) == dateOnly &&
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.NoShow);

                if (alreadyBooked)
                    return Result<Appointment>.Failure("المريض مسجل بالفعل في قائمة انتظار هذا اليوم.");

                // Calculate next turn number for the day
                int currentMaxTurn = db.Appointments
                    .Where(a => DbFunctions.TruncateTime(a.AppointmentDate) == dateOnly)
                    .Select(a => (int?)a.TurnNumber)
                    .Max() ?? 0;

                int nextTurn = currentMaxTurn + 1;

                // Initial status: if booked today and no payment yet, BookedUnpaid.
                var appointment = new Appointment
                {
                    PatientId = patientId,
                    AppointmentDate = dateOnly,
                    TurnNumber = nextTurn,
                    Status = AppointmentStatus.BookedUnpaid,
                    IsUrgent = isUrgent,
                    Notes = notes,
                    CreatedAt = DateTime.Now
                };

                db.Appointments.Add(appointment);
                db.SaveChanges();

                return Result<Appointment>.Success(appointment);
            }
        }

        public Result UpdateStatus(int appointmentId, AppointmentStatus newStatus)
        {
            using (var db = new EyeClinicDbContext())
            {
                var appt = db.Appointments.Find(appointmentId);
                if (appt == null)
                    return Result.Failure("الحجز غير موجود.");

                appt.Status = newStatus;
                db.SaveChanges();

                return Result.Success();
            }
        }

        public Result CallPatientToExam(int appointmentId)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("استدعاء المريض للكشف مقتصر على الطبيب.");

            using (var db = new EyeClinicDbContext())
            {
                var appt = db.Appointments.Find(appointmentId);
                if (appt == null)
                    return Result.Failure("الحجز غير موجود.");

                if (appt.Status != AppointmentStatus.Waiting)
                    return Result.Failure("يمكن فقط استدعاء مريض بحالة (منتظر).");

                // If another patient is currently InExam, mark them as Completed
                DateTime today = appt.AppointmentDate.Date;
                var currentInExam = db.Appointments
                    .Where(a => DbFunctions.TruncateTime(a.AppointmentDate) == today && a.Status == AppointmentStatus.InExam)
                    .ToList();

                foreach (var cur in currentInExam)
                {
                    cur.Status = AppointmentStatus.Completed;
                }

                appt.Status = AppointmentStatus.InExam;
                db.SaveChanges();

                return Result.Success();
            }
        }

        public Result CompleteExam(int appointmentId)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("إنهاء الكشف مقتصر على الطبيب.");

            using (var db = new EyeClinicDbContext())
            {
                var appt = db.Appointments.Find(appointmentId);
                if (appt == null)
                    return Result.Failure("الحجز غير موجود.");

                appt.Status = AppointmentStatus.Completed;
                db.SaveChanges();

                return Result.Success();
            }
        }

        public Result MarkNoShow(int appointmentId)
        {
            using (var db = new EyeClinicDbContext())
            {
                var appt = db.Appointments.Find(appointmentId);
                if (appt == null)
                    return Result.Failure("الحجز غير موجود.");

                appt.Status = AppointmentStatus.NoShow;
                db.SaveChanges();

                return Result.Success();
            }
        }

        public List<QueueItemDto> GetDailyQueue(DateTime date)
        {
            DateTime dateOnly = date.Date;

            using (var db = new EyeClinicDbContext())
            {
                var appts = db.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Payments)
                    .Include(a => a.Visits)
                    .Where(a => DbFunctions.TruncateTime(a.AppointmentDate) == dateOnly && !a.Patient.IsArchived)
                    .ToList();

                // Strict FIFO Sorting:
                // 1. Patient currently in examination (InExam) -> Top (Priority 1)
                // 2. Waiting Urgent patients -> Priority 2 (by TurnNumber ASC)
                // 3. Waiting regular patients -> Priority 3 (by TurnNumber ASC - strictly First In First Out)
                // 4. Booked but unpaid patients -> Priority 4 (by TurnNumber ASC)
                // 5. Completed patients -> Priority 5 (by TurnNumber ASC - bottom)
                // 6. NoShow & Cancelled -> Priority 6 & 7 (very bottom)
                var sorted = appts
                    .OrderBy(a => GetStatusPriority(a))
                    .ThenBy(a => a.TurnNumber)
                    .ToList();

                // Identify the exact next patient in line (first waiting patient)
                var nextWaiting = sorted.FirstOrDefault(a => a.Status == AppointmentStatus.Waiting);
                int nextWaitingId = nextWaiting != null ? nextWaiting.Id : 0;

                DateTime today = DateTime.Today;
                var list = new List<QueueItemDto>();

                foreach (var a in sorted)
                {
                    int? age = null;
                    if (a.Patient.BirthDate.HasValue)
                    {
                        var b = a.Patient.BirthDate.Value;
                        age = today.Year - b.Year;
                        if (b.Date > today.AddYears(-age.Value)) age--;
                        if (age < 0) age = 0;
                    }

                    bool hasMedicalFlags = !string.IsNullOrWhiteSpace(a.Patient.ChronicDiseases) ||
                                           !string.IsNullOrWhiteSpace(a.Patient.DrugAllergies);

                    var v = a.Visits != null ? a.Visits.FirstOrDefault() : null;

                    list.Add(new QueueItemDto
                    {
                        AppointmentId = a.Id,
                        PatientId = a.PatientId,
                        TurnNumber = a.TurnNumber,
                        FileNumber = a.Patient.FileNumber,
                        PatientName = a.Patient.FullName,
                        Mobile = a.Patient.Mobile,
                        Age = age,
                        ExamType = a.Payment != null ? a.Payment.ExamType : (a.IsUrgent ? ExamType.Urgent : ExamType.Exam),
                        Status = a.Status,
                        IsUrgent = a.IsUrgent,
                        AmountPaid = a.Payment != null ? a.Payment.ActualPrice : 0,
                        PaymentMethod = a.Payment != null ? (PaymentMethod?)a.Payment.Method : null,
                        CreatedAt = a.CreatedAt,
                        HasChronicOrAllergies = hasMedicalFlags,
                        IsNextInLine = (a.Id == nextWaitingId),
                        Diagnosis = v != null ? v.Diagnosis : ""
                    });
                }

                return list;
            }
        }

        public int GetNextInLineAppointmentId(DateTime date)
        {
            var queue = GetDailyQueue(date);
            var next = queue.FirstOrDefault(q => q.IsNextInLine);
            return next != null ? next.AppointmentId : 0;
        }

        private int GetStatusPriority(Appointment a)
        {
            switch (a.Status)
            {
                case AppointmentStatus.InExam: return 1;
                case AppointmentStatus.Waiting: return a.IsUrgent ? 2 : 3;
                case AppointmentStatus.BookedUnpaid: return 4;
                case AppointmentStatus.Completed: return 5;
                case AppointmentStatus.NoShow: return 6;
                case AppointmentStatus.Cancelled: return 7;
                default: return 8;
            }
        }
    }
}
