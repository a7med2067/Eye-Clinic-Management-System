using System;
using EyeClinic.Core.Enums;

namespace EyeClinic.Core.DTOs
{
    public class QueueItemDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int TurnNumber { get; set; }
        public string FileNumber { get; set; }
        public string PatientName { get; set; }
        public string Mobile { get; set; }
        public int? Age { get; set; }
        public ExamType ExamType { get; set; }
        public AppointmentStatus Status { get; set; }
        public bool IsUrgent { get; set; }
        public decimal AmountPaid { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasChronicOrAllergies { get; set; }
        public bool IsNextInLine { get; set; }
        public string Diagnosis { get; set; }
    }
}
