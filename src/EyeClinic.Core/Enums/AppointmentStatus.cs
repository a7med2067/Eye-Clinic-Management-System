namespace EyeClinic.Core.Enums
{
    public enum AppointmentStatus
    {
        BookedUnpaid = 1,  // محجوز - لم يُدفع
        Waiting = 2,       // منتظر
        InExam = 3,        // داخل الكشف
        Completed = 4,     // خلّص
        NoShow = 5,        // ما حضرش
        Cancelled = 6      // ملغي
    }
}
