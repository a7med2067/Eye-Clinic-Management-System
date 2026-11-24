using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Configuration;
using Newtonsoft.Json;

namespace EyeClinic.Data.Context
{
    public class EyeClinicDbContext : DbContext
    {
        public EyeClinicDbContext()
            : base(ConnectionConfig.GetConnectionString())
        {
            Database.SetInitializer<EyeClinicDbContext>(null);
            Configuration.LazyLoadingEnabled = false;
        }

        public EyeClinicDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Database.SetInitializer<EyeClinicDbContext>(null);
            Configuration.LazyLoadingEnabled = false;
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Patient> Patients { get; set; }
        public virtual DbSet<Appointment> Appointments { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }
        public virtual DbSet<Receipt> Receipts { get; set; }
        public virtual DbSet<Visit> Visits { get; set; }
        public virtual DbSet<Prescription> Prescriptions { get; set; }
        public virtual DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public virtual DbSet<MedicineCatalog> MedicineCatalogs { get; set; }
        public virtual DbSet<DiagnosisCatalog> DiagnosisCatalogs { get; set; }
        public virtual DbSet<PrescriptionTemplate> PrescriptionTemplates { get; set; }
        public virtual DbSet<DailyClosing> DailyClosings { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<ClinicSetting> ClinicSettings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            // Precision for decimals
            modelBuilder.Entity<Payment>().Property(p => p.StandardPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.ActualPrice).HasPrecision(18, 2);
            modelBuilder.Entity<DailyClosing>().Property(d => d.ExpectedCash).HasPrecision(18, 2);
            modelBuilder.Entity<DailyClosing>().Property(d => d.ActualCash).HasPrecision(18, 2);
            modelBuilder.Entity<DailyClosing>().Property(d => d.CashDifference).HasPrecision(18, 2);
            modelBuilder.Entity<DailyClosing>().Property(d => d.VodafoneCashTotal).HasPrecision(18, 2);
            modelBuilder.Entity<DailyClosing>().Property(d => d.InstaPayTotal).HasPrecision(18, 2);

            // Patient
            modelBuilder.Entity<Patient>()
                .Property(p => p.FileNumber)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Patient>()
                .Property(p => p.FullName)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<Patient>()
                .Property(p => p.Mobile)
                .IsRequired()
                .HasMaxLength(50);

            // User
            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            // Receipt
            modelBuilder.Entity<Receipt>()
                .HasIndex(r => r.ReceiptNumber)
                .IsUnique();

            // Setting
            modelBuilder.Entity<ClinicSetting>()
                .HasKey(s => s.SettingKey);

            modelBuilder.Entity<ClinicSetting>()
                .Property(s => s.SettingKey)
                .HasMaxLength(128);

            // Prevent cascade deletes to safeguard data integrity and avoid SQL Server Msg 1785 cycle errors
            modelBuilder.Entity<Payment>()
                .HasRequired(p => p.Appointment)
                .WithMany(a => a.Payments)
                .HasForeignKey(p => p.AppointmentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Visit>()
                .HasOptional(v => v.Appointment)
                .WithMany(a => a.Visits)
                .HasForeignKey(v => v.AppointmentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Payment>()
                .HasRequired(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Payment>()
                .HasRequired(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Payment>()
                .HasOptional(p => p.RefundedByUser)
                .WithMany()
                .HasForeignKey(p => p.RefundedByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Visit>()
                .HasRequired(v => v.Patient)
                .WithMany(p => p.Visits)
                .HasForeignKey(v => v.PatientId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Appointment>()
                .HasRequired(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Prescription>()
                .HasRequired(p => p.Visit)
                .WithMany(v => v.Prescriptions)
                .HasForeignKey(p => p.VisitId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PrescriptionItem>()
                .HasRequired(pi => pi.Prescription)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PrescriptionId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Receipt>()
                .HasRequired(r => r.Payment)
                .WithMany(p => p.Receipts)
                .HasForeignKey(r => r.PaymentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DailyClosing>()
                .HasRequired(d => d.ClosedByUser)
                .WithMany()
                .HasForeignKey(d => d.ClosedByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DailyClosing>()
                .HasOptional(d => d.ReopenedByUser)
                .WithMany()
                .HasForeignKey(d => d.ReopenedByUserId)
                .WillCascadeOnDelete(false);
        }

        public override int SaveChanges()
        {
            var auditEntries = CaptureAuditEntries();
            int result = base.SaveChanges();

            if (auditEntries != null && auditEntries.Count > 0)
            {
                foreach (var entry in auditEntries)
                {
                    AuditLogs.Add(entry);
                }
                base.SaveChanges();
            }

            return result;
        }

        private List<AuditLog> CaptureAuditEntries()
        {
            var list = new List<AuditLog>();
            var modifiedEntities = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in modifiedEntities)
            {
                // Skip AuditLog itself to prevent recursion
                if (entry.Entity is AuditLog) continue;

                var actionType = DetermineAuditAction(entry);
                if (actionType.HasValue)
                {
                    var oldValues = new Dictionary<string, object>();
                    var newValues = new Dictionary<string, object>();

                    if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                    {
                        foreach (string propName in entry.OriginalValues.PropertyNames)
                        {
                            oldValues[propName] = entry.OriginalValues[propName];
                        }
                    }

                    if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    {
                        foreach (string propName in entry.CurrentValues.PropertyNames)
                        {
                            newValues[propName] = entry.CurrentValues[propName];
                        }
                    }

                    string recordId = "0";
                    try
                    {
                        var keyProp = entry.Entity.GetType().GetProperty("Id");
                        if (keyProp != null)
                        {
                            object val = keyProp.GetValue(entry.Entity, null);
                            if (val != null) recordId = val.ToString();
                        }
                    }
                    catch
                    {
                        // ignore key extraction failure
                    }

                    list.Add(new AuditLog
                    {
                        UserId = SessionContext.CurrentUserId > 0 ? (int?)SessionContext.CurrentUserId : null,
                        Username = SessionContext.CurrentUsername,
                        ActionType = actionType.Value,
                        EntityName = entry.Entity.GetType().Name,
                        RecordId = recordId,
                        OldValuesJson = oldValues.Count > 0 ? JsonConvert.SerializeObject(oldValues) : null,
                        NewValuesJson = newValues.Count > 0 ? JsonConvert.SerializeObject(newValues) : null,
                        Description = $"{entry.Entity.GetType().Name} {entry.State}",
                        Timestamp = DateTime.Now
                    });
                }
            }

            return list;
        }

        private AuditActionType? DetermineAuditAction(DbEntityEntry entry)
        {
            var type = entry.Entity.GetType();

            if (type == typeof(Patient))
            {
                if (entry.State == EntityState.Added) return AuditActionType.CreatePatient;
                if (entry.State == EntityState.Modified)
                {
                    var isArchivedProp = entry.Property("IsArchived");
                    if (isArchivedProp != null && (bool)isArchivedProp.CurrentValue)
                        return AuditActionType.ArchivePatient;
                    return AuditActionType.UpdatePatient;
                }
            }
            else if (type == typeof(Appointment))
            {
                if (entry.State == EntityState.Added) return AuditActionType.BookAppointment;
                return AuditActionType.UpdateAppointmentStatus;
            }
            else if (type == typeof(Payment))
            {
                if (entry.State == EntityState.Added) return AuditActionType.CreatePayment;
                if (entry.State == EntityState.Modified)
                {
                    var refundedProp = entry.Property("IsRefunded");
                    if (refundedProp != null && (bool)refundedProp.CurrentValue)
                        return AuditActionType.RefundPayment;
                    return AuditActionType.PriceOverride;
                }
            }
            else if (type == typeof(Visit))
            {
                if (entry.State == EntityState.Added) return AuditActionType.SaveVisit;
                return AuditActionType.UpdateVisit;
            }
            else if (type == typeof(DailyClosing))
            {
                if (entry.State == EntityState.Added) return AuditActionType.DailyClose;
                return AuditActionType.ReopenDailyClose;
            }
            else if (type == typeof(ClinicSetting))
            {
                return AuditActionType.SettingsUpdate;
            }
            else if (type == typeof(User))
            {
                return AuditActionType.UserManagement;
            }

            return null;
        }
    }
}
