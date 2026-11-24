using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace EyeClinic.Core.Entities
{
    public class Visit
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? AppointmentId { get; set; }

        public string ChiefComplaint { get; set; }
        public string Diagnosis { get; set; }

        // Eye measurements (OD = Right eye, OS = Left eye)
        public string VA_OD { get; set; }
        public string VA_OS { get; set; }
        public string IOP_OD { get; set; }
        public string IOP_OS { get; set; }

        public string DoctorNotes { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.Today;

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsArchived { get; set; } = false;

        public virtual Patient Patient { get; set; }
        public virtual Appointment Appointment { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new HashSet<Prescription>();

        [NotMapped]
        public Prescription Prescription => Prescriptions?.FirstOrDefault();
    }
}
