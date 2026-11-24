using System;
using System.Collections.Generic;

namespace EyeClinic.Core.DTOs
{
    public class ImportPatientRow
    {
        public int RowNumber { get; set; }
        public string FullName { get; set; }
        public string Mobile { get; set; }
        public string NationalId { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? AgeYears { get; set; }
        public string Address { get; set; }
        public string ChronicDiseases { get; set; }
        public string DrugAllergies { get; set; }
        public string PastSurgeries { get; set; }
        public string GeneralNotes { get; set; }

        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsDuplicateInDb { get; set; }
    }

    public class PatientImportPreviewResult
    {
        public int TotalRows { get; set; }
        public int ValidRowsCount { get; set; }
        public int ErrorRowsCount { get; set; }
        public int DuplicateRowsCount { get; set; }
        public List<ImportPatientRow> Rows { get; set; }

        public PatientImportPreviewResult()
        {
            Rows = new List<ImportPatientRow>();
        }
    }

    public class PatientImportCommitResult
    {
        public bool Success { get; set; }
        public int ImportedCount { get; set; }
        public string ErrorMessage { get; set; }
    }
}
