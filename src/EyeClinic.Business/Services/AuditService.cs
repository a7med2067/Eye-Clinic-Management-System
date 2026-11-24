using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class AuditService
    {
        public List<AuditLog> GetLogs(DateTime? fromDate = null, DateTime? toDate = null, AuditActionType? actionType = null, int maxResults = 100)
        {
            if (!SessionContext.IsDoctor)
                return new List<AuditLog>(); // Secretary cannot view audit logs

            using (var db = new EyeClinicDbContext())
            {
                var q = db.AuditLogs.AsQueryable();

                if (fromDate.HasValue)
                {
                    DateTime from = fromDate.Value.Date;
                    q = q.Where(l => DbFunctions.TruncateTime(l.Timestamp) >= from);
                }

                if (toDate.HasValue)
                {
                    DateTime to = toDate.Value.Date;
                    q = q.Where(l => DbFunctions.TruncateTime(l.Timestamp) <= to);
                }

                if (actionType.HasValue)
                {
                    q = q.Where(l => l.ActionType == actionType.Value);
                }

                return q.OrderByDescending(l => l.Timestamp)
                        .Take(maxResults)
                        .ToList();
            }
        }
    }
}
