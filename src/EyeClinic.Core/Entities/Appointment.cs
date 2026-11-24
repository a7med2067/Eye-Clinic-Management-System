using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using EyeClinic.Core.Enums;

namespace EyeClinic.Core.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int TurnNumber { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.BookedUnpaid;
        public bool IsUrgent { get; set; } = false;
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Patient Patient { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
        public virtual ICollection<Visit> Visits { get; set; } = new HashSet<Visit>();

        [NotMapped]
        public Payment Payment => Payments?.FirstOrDefault(p => !p.IsRefunded) ?? Payments?.FirstOrDefault();

        [NotMapped]
        public Visit Visit => Visits?.FirstOrDefault();
    }
}
