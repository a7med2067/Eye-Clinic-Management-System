using System;
using EyeClinic.Core.Enums;

namespace EyeClinic.Core.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; }
        public AuditActionType ActionType { get; set; }
        public string EntityName { get; set; }
        public string RecordId { get; set; }
        public string OldValuesJson { get; set; }
        public string NewValuesJson { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
