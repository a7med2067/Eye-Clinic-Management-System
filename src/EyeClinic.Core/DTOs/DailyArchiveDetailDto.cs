using System;
using System.Collections.Generic;
using EyeClinic.Core.Enums;

namespace EyeClinic.Core.DTOs
{
    public class DailyArchiveDetailDto
    {
        public DateTime Date { get; set; }
        public DailyRevenueDto Summary { get; set; }
        public string ClosedByName { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string ClosingNotes { get; set; }

        public List<DailyPatientRecordDto> Patients { get; set; } = new List<DailyPatientRecordDto>();
        public List<DailyPrescriptionItemRecordDto> Prescriptions { get; set; } = new List<DailyPrescriptionItemRecordDto>();
    }

    public class DailyPatientRecordDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int TurnNumber { get; set; }
        public string FileNumber { get; set; }
        public string FullName { get; set; }
        public int? Age { get; set; }
        public string Mobile { get; set; }
        public ExamType ExamType { get; set; }
        public decimal AmountPaid { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public AppointmentStatus Status { get; set; }
        public string Diagnosis { get; set; }
        public string ChiefComplaint { get; set; }
        public DateTime Time { get; set; }
    }

    public class DailyPrescriptionItemRecordDto
    {
        public int VisitId { get; set; }
        public string PatientName { get; set; }
        public string FileNumber { get; set; }
        public string Diagnosis { get; set; }
        public string MedicineName { get; set; }
        public string DoseInstructions { get; set; }
    }
}
