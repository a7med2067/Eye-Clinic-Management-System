using System;
using System.Collections.Generic;

namespace EyeClinic.Core.DTOs
{
    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalVodafoneCash { get; set; }
        public decimal TotalInstaPay { get; set; }
        public decimal TotalRevenue => TotalCash + TotalVodafoneCash + TotalInstaPay;

        public int TotalExamCount { get; set; }
        public int TotalReExamCount { get; set; }
        public int TotalUrgentCount { get; set; }
        public int TotalPatients => TotalExamCount + TotalReExamCount + TotalUrgentCount;

        public int RefundedCount { get; set; }
        public decimal RefundedAmount { get; set; }

        public int NoShowCount { get; set; }
        public bool IsClosed { get; set; }
        public decimal? ActualCashCounted { get; set; }
        public decimal? CashDifference { get; set; }
    }
}
