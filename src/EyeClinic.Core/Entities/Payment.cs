using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using EyeClinic.Core.Enums;

namespace EyeClinic.Core.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public ExamType ExamType { get; set; } = ExamType.Exam;
        public decimal StandardPrice { get; set; }
        public decimal ActualPrice { get; set; }
        public bool PriceOverridden => ActualPrice != StandardPrice;
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string ReferenceNumber { get; set; }

        public bool IsRefunded { get; set; } = false;
        public string RefundReason { get; set; }
        public DateTime? RefundedAt { get; set; }
        public int? RefundedByUserId { get; set; }

        // Link for re-examinations to original paid exam
        public int? OriginalExamPaymentId { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Appointment Appointment { get; set; }
        public virtual Patient Patient { get; set; }
        public virtual ICollection<Receipt> Receipts { get; set; } = new HashSet<Receipt>();
        public virtual User CreatedByUser { get; set; }
        public virtual User RefundedByUser { get; set; }

        [NotMapped]
        public Receipt Receipt => Receipts?.FirstOrDefault();
    }
}
