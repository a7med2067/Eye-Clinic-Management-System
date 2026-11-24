using System;
using System.Collections.Generic;

namespace EyeClinic.Core.Entities
{
    public class Patient
    {
        public int Id { get; set; }
        public string FileNumber { get; set; }
        public string FullName { get; set; }
        public string Mobile { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string NationalId { get; set; }
        public string GeneralNotes { get; set; }

        // Confidential medical fields (Doctor only)
        public string ChronicDiseases { get; set; }
        public string DrugAllergies { get; set; }
        public string PastSurgeries { get; set; }

        public bool IsArchived { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>();
        public virtual ICollection<Visit> Visits { get; set; } = new HashSet<Visit>();

        public int? CalculatedAge
        {
            get
            {
                if (!BirthDate.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                return age < 0 ? 0 : age;
            }
        }
    }
}
