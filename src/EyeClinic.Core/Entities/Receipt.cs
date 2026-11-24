using System;

namespace EyeClinic.Core.Entities
{
    public class Receipt
    {
        public int Id { get; set; }
        public long ReceiptNumber { get; set; }
        public int PaymentId { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public int PrintCount { get; set; } = 0;
        public DateTime? LastPrintedAt { get; set; }

        public virtual Payment Payment { get; set; }
    }
}
