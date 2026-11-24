using System;

namespace EyeClinic.Core.Entities
{
    public class PrescriptionTemplate
    {
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public string ItemsJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
