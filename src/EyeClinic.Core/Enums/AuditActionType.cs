namespace EyeClinic.Core.Enums
{
    public enum AuditActionType
    {
        Login = 1,
        Logout = 2,
        CreatePatient = 3,
        UpdatePatient = 4,
        ArchivePatient = 5,
        BookAppointment = 6,
        UpdateAppointmentStatus = 7,
        CreatePayment = 8,
        PriceOverride = 9,
        RefundPayment = 10,
        SaveVisit = 11,
        UpdateVisit = 12,
        DailyClose = 13,
        ReopenDailyClose = 14,
        SettingsUpdate = 15,
        DatabaseRestore = 16,
        UserManagement = 17,
        DatabaseBackup = 18,
        ImportPatients = 19
    }
}
