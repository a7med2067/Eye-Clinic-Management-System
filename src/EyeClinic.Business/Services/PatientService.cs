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
    public class PatientService
    {
        public string GenerateNextFileNumber()
        {
            using (var db = new EyeClinicDbContext())
            {
                int maxNumber = 1000;
                var fileNumbers = db.Patients
                    .Select(p => p.FileNumber)
                    .ToList();

                foreach (var fn in fileNumbers)
                {
                    if (int.TryParse(fn, out int num) && num > maxNumber)
                    {
                        maxNumber = num;
                    }
                }

                return (maxNumber + 1).ToString();
            }
        }

        public (bool hasDuplicate, Patient existingPatient, string matchReason) CheckDuplicate(string fullName, string mobile, int excludeId = 0)
        {
            using (var db = new EyeClinicDbContext())
            {
                string normName = (fullName ?? "").Trim();
                string normMobile = (mobile ?? "").Trim();

                var match = db.Patients
                    .Where(p => !p.IsArchived && p.Id != excludeId)
                    .FirstOrDefault(p => p.Mobile == normMobile || p.FullName == normName);

                if (match != null)
                {
                    string reason = match.Mobile == normMobile && match.FullName == normName
                        ? "تطابق في الاسم ورقم الهاتف معاً"
                        : (match.Mobile == normMobile ? "تطابق في رقم الهاتف" : "تطابق في الاسم بالكامل");

                    return (true, match, reason);
                }

                return (false, null, null);
            }
        }

        public Result<Patient> RegisterPatient(Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.FullName))
                return Result<Patient>.Failure("اسم المريض إجباري.");

            if (string.IsNullOrWhiteSpace(patient.Mobile))
                return Result<Patient>.Failure("رقم الموبايل إجباري.");

            using (var db = new EyeClinicDbContext())
            {
                if (string.IsNullOrWhiteSpace(patient.FileNumber))
                {
                    patient.FileNumber = GenerateNextFileNumber();
                }

                patient.FullName = patient.FullName.Trim();
                patient.Mobile = patient.Mobile.Trim();
                patient.CreatedAt = DateTime.Now;
                patient.IsArchived = false;

                db.Patients.Add(patient);
                db.SaveChanges();

                return Result<Patient>.Success(patient);
            }
        }

        public Result UpdatePatient(Patient updatedData)
        {
            if (string.IsNullOrWhiteSpace(updatedData.FullName))
                return Result.Failure("اسم المريض إجباري.");

            if (string.IsNullOrWhiteSpace(updatedData.Mobile))
                return Result.Failure("رقم الموبايل إجباري.");

            using (var db = new EyeClinicDbContext())
            {
                var patient = db.Patients.Find(updatedData.Id);
                if (patient == null)
                    return Result.Failure("المريض غير موجود.");

                patient.FullName = updatedData.FullName.Trim();
                patient.Mobile = updatedData.Mobile.Trim();
                patient.BirthDate = updatedData.BirthDate;
                patient.Gender = updatedData.Gender;
                patient.Address = updatedData.Address;
                patient.NationalId = updatedData.NationalId;
                patient.GeneralNotes = updatedData.GeneralNotes;

                // Only doctor can edit sensitive medical history
                if (SessionContext.IsDoctor)
                {
                    patient.ChronicDiseases = updatedData.ChronicDiseases;
                    patient.DrugAllergies = updatedData.DrugAllergies;
                    patient.PastSurgeries = updatedData.PastSurgeries;
                }

                patient.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                return Result.Success();
            }
        }

        public Result ArchivePatient(int patientId)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("أرشفة أو إخفاء المرضى مقتصرة على الطبيب فقط.");

            using (var db = new EyeClinicDbContext())
            {
                var patient = db.Patients.Find(patientId);
                if (patient == null)
                    return Result.Failure("المريض غير موجود.");

                patient.IsArchived = true;
                patient.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                return Result.Success();
            }
        }

        public Patient GetPatientById(int id)
        {
            using (var db = new EyeClinicDbContext())
            {
                var patient = db.Patients.FirstOrDefault(p => p.Id == id && !p.IsArchived);
                if (patient != null && !SessionContext.IsDoctor)
                {
                    // Secretary cannot view confidential medical notes
                    patient.ChronicDiseases = null;
                    patient.DrugAllergies = null;
                    patient.PastSurgeries = null;
                }
                return patient;
            }
        }

        public List<PatientSearchResultDto> SearchPatients(string query, int maxResults = 30)
        {
            using (var db = new EyeClinicDbContext())
            {
                int reExamDays = 14;
                var reExamSetting = db.ClinicSettings.Find("ReExam_ValidDays");
                if (reExamSetting != null && int.TryParse(reExamSetting.SettingValue, out int val))
                {
                    reExamDays = val;
                }

                var q = db.Patients
                    .Where(p => !p.IsArchived);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    string term = query.Trim();
                    q = q.Where(p => p.FullName.Contains(term) ||
                                     p.Mobile.Contains(term) ||
                                     p.FileNumber.Contains(term));
                }

                var rawList = q.OrderByDescending(p => p.Id)
                               .Take(maxResults)
                               .Select(p => new
                               {
                                   p.Id,
                                   p.FileNumber,
                                   p.FullName,
                                   p.Mobile,
                                   p.BirthDate,
                                   p.Gender
                               })
                               .ToList();

                var patientIds = rawList.Select(x => x.Id).ToList();

                // Get last paid Exam payment for each patient to determine ReExam eligibility
                var lastPayments = db.Payments
                    .Where(pay => patientIds.Contains(pay.PatientId) &&
                                  !pay.IsRefunded &&
                                  pay.ExamType == ExamType.Exam)
                    .GroupBy(pay => pay.PatientId)
                    .Select(g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault())
                    .ToList();

                var result = new List<PatientSearchResultDto>();
                DateTime today = DateTime.Today;

                foreach (var p in rawList)
                {
                    int? age = null;
                    if (p.BirthDate.HasValue)
                    {
                        var birth = p.BirthDate.Value;
                        age = today.Year - birth.Year;
                        if (birth.Date > today.AddYears(-age.Value)) age--;
                        if (age < 0) age = 0;
                    }

                    var lastPay = lastPayments.FirstOrDefault(x => x != null && x.PatientId == p.Id);
                    bool isEligible = false;
                    int? daysSince = null;
                    int? originalExamId = null;

                    if (lastPay != null)
                    {
                        daysSince = (int)(today - lastPay.CreatedAt.Date).TotalDays;
                        if (daysSince >= 0 && daysSince <= reExamDays)
                        {
                            isEligible = true;
                            originalExamId = lastPay.Id;
                        }
                    }

                    result.Add(new PatientSearchResultDto
                    {
                        Id = p.Id,
                        FileNumber = p.FileNumber,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Age = age,
                        Gender = p.Gender,
                        LastVisitDate = lastPay?.CreatedAt,
                        DaysSinceLastExam = daysSince,
                        IsEligibleForReExam = isEligible,
                        OriginalExamPaymentId = originalExamId
                    });
                }

                return result;
            }
        }

        public List<PatientSearchResultDto> FindDuplicateMatches(string name, string mobile, int excludeId = 0)
        {
            using (var db = new EyeClinicDbContext())
            {
                string normName = (name ?? "").Trim();
                string normMobile = (mobile ?? "").Trim();

                if (string.IsNullOrWhiteSpace(normName) && string.IsNullOrWhiteSpace(normMobile))
                {
                    return new List<PatientSearchResultDto>();
                }

                var q = db.Patients.Where(p => !p.IsArchived && p.Id != excludeId);

                if (!string.IsNullOrWhiteSpace(normName) && !string.IsNullOrWhiteSpace(normMobile))
                {
                    q = q.Where(p => p.FullName.Contains(normName) || p.Mobile.Contains(normMobile));
                }
                else if (!string.IsNullOrWhiteSpace(normName))
                {
                    q = q.Where(p => p.FullName.Contains(normName));
                }
                else
                {
                    q = q.Where(p => p.Mobile.Contains(normMobile));
                }

                var list = q.OrderByDescending(p => p.Id).Take(6).ToList();
                var result = new List<PatientSearchResultDto>();

                foreach (var p in list)
                {
                    result.Add(new PatientSearchResultDto
                    {
                        Id = p.Id,
                        FileNumber = p.FileNumber,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Age = p.CalculatedAge,
                        Gender = p.Gender
                    });
                }

                return result;
            }
        }

        public PatientProfileDto GetPatientProfile(int patientId)
        {
            using (var db = new EyeClinicDbContext())
            {
                var patient = db.Patients.FirstOrDefault(p => p.Id == patientId && !p.IsArchived);
                if (patient == null) return null;

                var visits = db.Visits
                    .Include("Prescriptions.Items")
                    .Where(v => v.PatientId == patientId && !v.IsArchived)
                    .OrderByDescending(v => v.VisitDate)
                    .ThenByDescending(v => v.Id)
                    .ToList();

                var payments = db.Payments
                    .Include(p => p.Receipts)
                    .Where(p => p.PatientId == patientId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();

                var profile = new PatientProfileDto
                {
                    Patient = patient,
                    TotalVisits = visits.Count,
                    TotalSpent = payments.Where(p => !p.IsRefunded).Sum(p => (decimal?)p.ActualPrice) ?? 0,
                    FirstVisitDate = visits.Any() ? (DateTime?)visits.Min(v => v.VisitDate) : null,
                    LastVisitDate = visits.Any() ? (DateTime?)visits.Max(v => v.VisitDate) : null
                };

                foreach (var v in visits)
                {
                    var medsList = new List<PrescriptionPrintItemDto>();
                    if (v.Prescriptions != null)
                    {
                        foreach (var rx in v.Prescriptions)
                        {
                            if (rx.Items != null)
                            {
                                foreach (var item in rx.Items.OrderBy(i => i.OrderIndex))
                                {
                                    medsList.Add(new PrescriptionPrintItemDto
                                    {
                                        MedicineName = item.MedicineName,
                                        DoseInstructions = item.DoseInstructions
                                    });

                                    profile.AllMedications.Add(new PatientMedicationRecordDto
                                    {
                                        PrescribedDate = v.VisitDate,
                                        MedicineName = item.MedicineName,
                                        DoseInstructions = item.DoseInstructions,
                                        Diagnosis = v.Diagnosis
                                    });
                                }
                            }
                        }
                    }

                    profile.Visits.Add(new PatientVisitRecordDto
                    {
                        VisitId = v.Id,
                        VisitDate = v.VisitDate,
                        ChiefComplaint = v.ChiefComplaint,
                        Diagnosis = v.Diagnosis,
                        VA_OD = v.VA_OD,
                        VA_OS = v.VA_OS,
                        IOP_OD = v.IOP_OD,
                        IOP_OS = v.IOP_OS,
                        DoctorNotes = v.DoctorNotes,
                        FollowUpDate = v.FollowUpDate,
                        Medicines = medsList
                    });
                }

                foreach (var p in payments)
                {
                    var r = p.Receipts != null ? p.Receipts.FirstOrDefault() : null;
                    profile.Payments.Add(new PatientPaymentRecordDto
                    {
                        PaymentId = p.Id,
                        Date = p.CreatedAt,
                        ExamType = p.ExamType,
                        Amount = p.ActualPrice,
                        Method = p.Method,
                        ReceiptNumber = r != null ? r.ReceiptNumber.ToString() : "-",
                        IsRefunded = p.IsRefunded,
                        RefundedAmount = p.IsRefunded ? (decimal?)p.ActualPrice : null,
                        RefundReason = p.RefundReason
                    });
                }

                return profile;
            }
        }
    }
}
