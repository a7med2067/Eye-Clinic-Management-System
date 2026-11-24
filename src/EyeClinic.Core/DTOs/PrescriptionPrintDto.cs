using System;
using System.Collections.Generic;

namespace EyeClinic.Core.DTOs
{
    public class PrescriptionPrintDto
    {
        public int VisitId { get; set; }
        public string PatientName { get; set; }
        public string Age { get; set; }
        public string Diagnosis { get; set; }
        public DateTime Date { get; set; }
        public string FormattedDate => Date.ToString("dd/MM/yyyy");
        public List<PrescriptionPrintItemDto> Medicines { get; set; } = new List<PrescriptionPrintItemDto>();
    }

    public class PrescriptionPrintItemDto
    {
        public string MedicineName { get; set; }
        public string DoseInstructions { get; set; }
    }
}
