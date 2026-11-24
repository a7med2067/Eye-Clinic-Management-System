using System;

namespace EyeClinic.Core.Entities
{
    public class MedicineCatalog
    {
        public int Id { get; set; }
        public string MedicineName { get; set; }
        public string DefaultDose { get; set; }
        public int UsageCount { get; set; } = 1;
        public DateTime LastUsedAt { get; set; } = DateTime.Now;
    }
}
