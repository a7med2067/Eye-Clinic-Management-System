namespace EyeClinic.Core.Entities
{
    public class PrescriptionItem
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        public string MedicineName { get; set; }
        public string DoseInstructions { get; set; }
        public int OrderIndex { get; set; } = 0;

        public virtual Prescription Prescription { get; set; }
    }
}
