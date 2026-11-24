using System;
using System.Collections.Generic;

namespace EyeClinic.Core.Entities
{
    public class Prescription
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Visit Visit { get; set; }
        public virtual ICollection<PrescriptionItem> Items { get; set; } = new HashSet<PrescriptionItem>();
    }
}
