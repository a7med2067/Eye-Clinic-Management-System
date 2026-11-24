using System;
using System.Collections.Generic;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;

namespace EyeClinic.Core.DTOs
{
    public class PatientProfileDto
    {
        public Patient Patient { get; set; }
        public int TotalVisits { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? FirstVisitDate { get; set; }
        public DateTime? LastVisitDate { get; set; }

        public List<PatientVisitRecordDto> Visits { get; set; } = new List<PatientVisitRecordDto>();
        public List<PatientMedicationRecordDto> AllMedications { get; set; } = new List<PatientMedicationRecordDto>();
        public List<PatientPaymentRecordDto> Payments { get; set; } = new List<PatientPaymentRecordDto>();
    }

    public class PatientVisitRecordDto
    {
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public string ChiefComplaint { get; set; }
        public string Diagnosis { get; set; }
        public string VA_OD { get; set; }
        public string VA_OS { get; set; }
        public string IOP_OD { get; set; }
        public string IOP_OS { get; set; }
        public string DoctorNotes { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public List<PrescriptionPrintItemDto> Medicines { get; set; } = new List<PrescriptionPrintItemDto>();
    }

    public class PatientMedicationRecordDto
    {
        public DateTime PrescribedDate { get; set; }
        public string MedicineName { get; set; }
        public string DoseInstructions { get; set; }
        public string Diagnosis { get; set; }
    }

    public class PatientPaymentRecordDto
    {
        public int PaymentId { get; set; }
        public DateTime Date { get; set; }
        public ExamType ExamType { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string ReceiptNumber { get; set; }
        public bool IsRefunded { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string RefundReason { get; set; }
    }
}
