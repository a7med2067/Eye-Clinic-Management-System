using System;

namespace EyeClinic.Core.DTOs
{
    public class PatientSearchResultDto
    {
        public int Id { get; set; }
        public string FileNumber { get; set; }
        public string FullName { get; set; }
        public string Mobile { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public bool IsEligibleForReExam { get; set; }
        public int? DaysSinceLastExam { get; set; }
        public int? OriginalExamPaymentId { get; set; }
    }
}
