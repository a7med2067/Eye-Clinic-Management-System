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
    public class VisitService
    {
        public Result<Visit> SaveVisit(Visit visit, List<PrescriptionPrintItemDto> medicines)
        {
            if (!SessionContext.IsDoctor)
                return Result<Visit>.Failure("تسجيل وكتابة الكشف الطبي مقتصر على الطبيب فقط.");

            using (var db = new EyeClinicDbContext())
            {
                visit.CreatedByUserId = SessionContext.CurrentUserId > 0 ? SessionContext.CurrentUserId : 1;
                visit.CreatedAt = DateTime.Now;
                visit.VisitDate = DateTime.Today;

                db.Visits.Add(visit);

                if (medicines != null && medicines.Count > 0)
                {
                    var rx = new Prescription
                    {
                        Visit = visit,
                        CreatedAt = DateTime.Now
                    };
                    db.Prescriptions.Add(rx);

                    int order = 1;
                    foreach (var m in medicines)
                    {
                        if (string.IsNullOrWhiteSpace(m.MedicineName)) continue;

                        db.PrescriptionItems.Add(new PrescriptionItem
                        {
                            Prescription = rx,
                            MedicineName = m.MedicineName.Trim(),
                            DoseInstructions = (m.DoseInstructions ?? "").Trim(),
                            OrderIndex = order++
                        });

                        // Dynamic catalog learning for Medicines
                        string medName = m.MedicineName.Trim();
                        var catalogMed = db.MedicineCatalogs.FirstOrDefault(c => c.MedicineName == medName);
                        if (catalogMed == null)
                        {
                            db.MedicineCatalogs.Add(new MedicineCatalog
                            {
                                MedicineName = medName,
                                DefaultDose = (m.DoseInstructions ?? "").Trim(),
                                UsageCount = 1,
                                LastUsedAt = DateTime.Now
                            });
                        }
                        else
                        {
                            catalogMed.UsageCount++;
                            if (!string.IsNullOrWhiteSpace(m.DoseInstructions))
                            {
                                catalogMed.DefaultDose = m.DoseInstructions.Trim();
                            }
                            catalogMed.LastUsedAt = DateTime.Now;
                        }
                    }
                }

                // Dynamic catalog learning for Diagnosis
                if (!string.IsNullOrWhiteSpace(visit.Diagnosis))
                {
                    string diagName = visit.Diagnosis.Trim();
                    var catalogDiag = db.DiagnosisCatalogs.FirstOrDefault(c => c.DiagnosisName == diagName);
                    if (catalogDiag == null)
                    {
                        db.DiagnosisCatalogs.Add(new DiagnosisCatalog
                        {
                            DiagnosisName = diagName,
                            UsageCount = 1,
                            LastUsedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        catalogDiag.UsageCount++;
                        catalogDiag.LastUsedAt = DateTime.Now;
                    }
                }

                // If associated with appointment, update status to Completed
                if (visit.AppointmentId.HasValue)
                {
                    var appt = db.Appointments.Find(visit.AppointmentId.Value);
                    if (appt != null)
                    {
                        appt.Status = AppointmentStatus.Completed;
                    }
                }

                db.SaveChanges();
                return Result<Visit>.Success(visit);
            }
        }

        public List<Visit> GetPatientVisitHistory(int patientId)
        {
            if (!SessionContext.IsDoctor)
                return new List<Visit>(); // Secretary cannot view clinical history

            using (var db = new EyeClinicDbContext())
            {
                return db.Visits
                    .Include("Prescriptions.Items")
                    .Where(v => v.PatientId == patientId && !v.IsArchived)
                    .OrderByDescending(v => v.VisitDate)
                    .ThenByDescending(v => v.Id)
                    .ToList();
            }
        }

        public List<PrescriptionPrintItemDto> GetLastPrescription(int patientId)
        {
            using (var db = new EyeClinicDbContext())
            {
                var lastVisitWithRx = db.Visits
                    .Include("Prescriptions.Items")
                    .Where(v => v.PatientId == patientId && !v.IsArchived && v.Prescriptions.Any())
                    .OrderByDescending(v => v.VisitDate)
                    .ThenByDescending(v => v.Id)
                    .FirstOrDefault();

                if (lastVisitWithRx?.Prescription?.Items != null)
                {
                    return lastVisitWithRx.Prescription.Items
                        .OrderBy(i => i.OrderIndex)
                        .Select(i => new PrescriptionPrintItemDto
                        {
                            MedicineName = i.MedicineName,
                            DoseInstructions = i.DoseInstructions
                        })
                        .ToList();
                }

                return new List<PrescriptionPrintItemDto>();
            }
        }

        public List<string> SearchDiagnosisSuggestions(string term, int maxResults = 10)
        {
            using (var db = new EyeClinicDbContext())
            {
                var q = db.DiagnosisCatalogs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(term))
                {
                    string t = term.Trim();
                    q = q.Where(c => c.DiagnosisName.Contains(t));
                }

                return q.OrderByDescending(c => c.UsageCount)
                        .ThenByDescending(c => c.LastUsedAt)
                        .Take(maxResults)
                        .Select(c => c.DiagnosisName)
                        .ToList();
            }
        }

        public List<MedicineCatalog> SearchMedicineSuggestions(string term, int maxResults = 15)
        {
            using (var db = new EyeClinicDbContext())
            {
                var q = db.MedicineCatalogs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(term))
                {
                    string t = term.Trim();
                    q = q.Where(c => c.MedicineName.Contains(t));
                }

                return q.OrderByDescending(c => c.UsageCount)
                        .ThenByDescending(c => c.LastUsedAt)
                        .Take(maxResults)
                        .ToList();
            }
        }
    }
}
