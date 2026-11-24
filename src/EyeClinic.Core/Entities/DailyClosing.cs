using System;

namespace EyeClinic.Core.Entities
{
    public class DailyClosing
    {
        public int Id { get; set; }
        public DateTime ClosingDate { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal CashDifference { get; set; }
        public decimal VodafoneCashTotal { get; set; }
        public decimal InstaPayTotal { get; set; }
        public string Notes { get; set; }

        public int ClosedByUserId { get; set; }
        public DateTime ClosedAt { get; set; } = DateTime.Now;

        public bool IsReopened { get; set; } = false;
        public int? ReopenedByUserId { get; set; }
        public DateTime? ReopenedAt { get; set; }

        public virtual User ClosedByUser { get; set; }
        public virtual User ReopenedByUser { get; set; }
    }
}
