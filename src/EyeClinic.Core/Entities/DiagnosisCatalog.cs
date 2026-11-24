using System;

namespace EyeClinic.Core.Entities
{
    public class DiagnosisCatalog
    {
        public int Id { get; set; }
        public string DiagnosisName { get; set; }
        public int UsageCount { get; set; } = 1;
        public DateTime LastUsedAt { get; set; } = DateTime.Now;
    }
}
