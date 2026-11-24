-- =======================================================================
-- نظام إدارة عيادة العيون (Eye Clinic Management System)
-- database.sql — Complete Schema Script
-- =======================================================================
-- Target:  SQL Server 2014 Express (Compatibility Level 120)
-- Runtime: .NET Framework 4.8 / Entity Framework 6.4.4
-- Collation: Arabic_CI_AS
-- =======================================================================
-- IMPORTANT: This script is derived directly from the EF6 Code-First
-- entity models and DbContext Fluent API in src/EyeClinic.Data.
-- It mirrors exactly what EF6 generates at runtime.
-- =======================================================================

-- -----------------------------------------------------------------------
-- 1. Create Database
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT [name] FROM sys.databases WHERE [name] = N'EyeClinicDb')
BEGIN
    CREATE DATABASE [EyeClinicDb]
    COLLATE Arabic_CI_AS;
END
GO

ALTER DATABASE [EyeClinicDb] SET COMPATIBILITY_LEVEL = 120;
GO

USE [EyeClinicDb];
GO

-- -----------------------------------------------------------------------
-- 2. User  (Users / Login accounts)
--    Entity:  EyeClinic.Core.Entities.User
--    Enum:    UserRole { Doctor = 1, Secretary = 2 }
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'User')
BEGIN
    CREATE TABLE [dbo].[User] (
        [Id]                   INT            IDENTITY(1,1) NOT NULL,
        [Username]             NVARCHAR(100)  NOT NULL,
        [PasswordHash]         NVARCHAR(MAX)  NOT NULL,
        [PasswordSalt]         NVARCHAR(MAX)  NOT NULL,
        [FullName]             NVARCHAR(MAX)  NULL,
        [Role]                 INT            NOT NULL,            -- UserRole enum (1=Doctor, 2=Secretary)
        [IsActive]             BIT            NOT NULL DEFAULT 1,
        [MustChangePassword]   BIT            NOT NULL DEFAULT 1,
        [FailedLoginAttempts]  INT            NOT NULL DEFAULT 0,
        [LockoutEnd]           DATETIME       NULL,
        [LastLoginAt]          DATETIME       NULL,
        [CreatedAt]            DATETIME       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- DbContext Fluent API: Username is Required, MaxLength(100)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_User_Username]
        ON [dbo].[User] ([Username] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 3. Patient  (Patient records)
--    Entity:  EyeClinic.Core.Entities.Patient
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'Patient')
BEGIN
    CREATE TABLE [dbo].[Patient] (
        [Id]               INT            IDENTITY(1,1) NOT NULL,
        [FileNumber]       NVARCHAR(50)   NOT NULL,               -- Required, MaxLength(50)
        [FullName]         NVARCHAR(200)  NOT NULL,               -- Required, MaxLength(200)
        [Mobile]           NVARCHAR(50)   NOT NULL,               -- Required, MaxLength(50)
        [BirthDate]        DATETIME       NULL,
        [Gender]           NVARCHAR(MAX)  NULL,
        [Address]          NVARCHAR(MAX)  NULL,
        [NationalId]       NVARCHAR(MAX)  NULL,
        [GeneralNotes]     NVARCHAR(MAX)  NULL,
        [ChronicDiseases]  NVARCHAR(MAX)  NULL,                   -- Doctor-only fields
        [DrugAllergies]    NVARCHAR(MAX)  NULL,
        [PastSurgeries]    NVARCHAR(MAX)  NULL,
        [IsArchived]       BIT            NOT NULL DEFAULT 0,
        [CreatedAt]        DATETIME       NOT NULL DEFAULT GETDATE(),
        [UpdatedAt]        DATETIME       NULL,
        CONSTRAINT [PK_Patient] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_Patient_FileNumber]
        ON [dbo].[Patient] ([FileNumber] ASC);
    CREATE NONCLUSTERED INDEX [IX_Patient_FullName]
        ON [dbo].[Patient] ([FullName] ASC);
    CREATE NONCLUSTERED INDEX [IX_Patient_Mobile]
        ON [dbo].[Patient] ([Mobile] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 4. Appointment  (Queue / booking)
--    Entity:  EyeClinic.Core.Entities.Appointment
--    Enum:    AppointmentStatus { BookedUnpaid=1, Waiting=2, InExam=3,
--                                 Completed=4, NoShow=5, Cancelled=6 }
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'Appointment')
BEGIN
    CREATE TABLE [dbo].[Appointment] (
        [Id]              INT       IDENTITY(1,1) NOT NULL,
        [PatientId]       INT       NOT NULL,
        [AppointmentDate] DATETIME  NOT NULL,
        [TurnNumber]      INT       NOT NULL,
        [Status]          INT       NOT NULL DEFAULT 1,            -- AppointmentStatus enum
        [IsUrgent]        BIT       NOT NULL DEFAULT 0,
        [Notes]           NVARCHAR(MAX) NULL,
        [CreatedAt]       DATETIME  NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Appointment] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Appointment_Patient] FOREIGN KEY ([PatientId])
            REFERENCES [dbo].[Patient] ([Id])
            -- WillCascadeOnDelete(false) per DbContext
    );

    CREATE NONCLUSTERED INDEX [IX_Appointment_PatientId]
        ON [dbo].[Appointment] ([PatientId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Appointment_Date_Status]
        ON [dbo].[Appointment] ([AppointmentDate] ASC, [Status] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 5. Payment  (Financial transactions)
--    Entity:  EyeClinic.Core.Entities.Payment
--    Enums:   ExamType { Exam=1, ReExam=2, Urgent=3 }
--             PaymentMethod { Cash=1, VodafoneCash=2, InstaPay=3 }
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'Payment')
BEGIN
    CREATE TABLE [dbo].[Payment] (
        [Id]                     INT            IDENTITY(1,1) NOT NULL,
        [AppointmentId]          INT            NOT NULL,
        [PatientId]              INT            NOT NULL,
        [ExamType]               INT            NOT NULL DEFAULT 1,
        [StandardPrice]          DECIMAL(18,2)  NOT NULL,          -- HasPrecision(18,2)
        [ActualPrice]            DECIMAL(18,2)  NOT NULL,          -- HasPrecision(18,2)
        [Method]                 INT            NOT NULL DEFAULT 1,
        [ReferenceNumber]        NVARCHAR(MAX)  NULL,
        [IsRefunded]             BIT            NOT NULL DEFAULT 0,
        [RefundReason]           NVARCHAR(MAX)  NULL,
        [RefundedAt]             DATETIME       NULL,
        [RefundedByUserId]       INT            NULL,
        [OriginalExamPaymentId]  INT            NULL,              -- For re-exam linkage
        [CreatedByUserId]        INT            NOT NULL,
        [CreatedAt]              DATETIME       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Payment] PRIMARY KEY CLUSTERED ([Id] ASC),
        -- FK to Appointment (no cascade)
        CONSTRAINT [FK_Payment_Appointment] FOREIGN KEY ([AppointmentId])
            REFERENCES [dbo].[Appointment] ([Id]),
        -- FK to Patient (no cascade)
        CONSTRAINT [FK_Payment_Patient] FOREIGN KEY ([PatientId])
            REFERENCES [dbo].[Patient] ([Id]),
        -- FK to User (created by, no cascade)
        CONSTRAINT [FK_Payment_CreatedByUser] FOREIGN KEY ([CreatedByUserId])
            REFERENCES [dbo].[User] ([Id]),
        -- FK to User (refunded by, optional, no cascade)
        CONSTRAINT [FK_Payment_RefundedByUser] FOREIGN KEY ([RefundedByUserId])
            REFERENCES [dbo].[User] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Payment_AppointmentId]
        ON [dbo].[Payment] ([AppointmentId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Payment_PatientId]
        ON [dbo].[Payment] ([PatientId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Payment_CreatedAt]
        ON [dbo].[Payment] ([CreatedAt] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 6. Receipt  (Gapless sequential receipt numbering)
--    Entity:  EyeClinic.Core.Entities.Receipt
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'Receipt')
BEGIN
    CREATE TABLE [dbo].[Receipt] (
        [Id]            INT      IDENTITY(1,1) NOT NULL,
        [ReceiptNumber] BIGINT   NOT NULL,
        [PaymentId]     INT      NOT NULL,
        [IssuedAt]      DATETIME NOT NULL DEFAULT GETDATE(),
        [PrintCount]    INT      NOT NULL DEFAULT 0,
        [LastPrintedAt] DATETIME NULL,
        CONSTRAINT [PK_Receipt] PRIMARY KEY CLUSTERED ([Id] ASC),
        -- FK to Payment (no cascade)
        CONSTRAINT [FK_Receipt_Payment] FOREIGN KEY ([PaymentId])
            REFERENCES [dbo].[Payment] ([Id])
    );

    -- DbContext Fluent API: HasIndex(r => r.ReceiptNumber).IsUnique()
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Receipt_ReceiptNumber]
        ON [dbo].[Receipt] ([ReceiptNumber] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 7. Visit  (Medical examination records)
--    Entity:  EyeClinic.Core.Entities.Visit
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'Visit')
BEGIN
    CREATE TABLE [dbo].[Visit] (
        [Id]              INT       IDENTITY(1,1) NOT NULL,
        [PatientId]       INT       NOT NULL,
        [AppointmentId]   INT       NULL,                          -- Optional FK
        [ChiefComplaint]  NVARCHAR(MAX) NULL,
        [Diagnosis]       NVARCHAR(MAX) NULL,
        [VA_OD]           NVARCHAR(MAX) NULL,                     -- Visual Acuity Right
        [VA_OS]           NVARCHAR(MAX) NULL,                     -- Visual Acuity Left
        [IOP_OD]          NVARCHAR(MAX) NULL,                     -- Intraocular Pressure Right
        [IOP_OS]          NVARCHAR(MAX) NULL,                     -- Intraocular Pressure Left
        [DoctorNotes]     NVARCHAR(MAX) NULL,
        [FollowUpDate]    DATETIME  NULL,
        [VisitDate]       DATETIME  NOT NULL DEFAULT GETDATE(),
        [CreatedByUserId] INT       NOT NULL,
        [CreatedAt]       DATETIME  NOT NULL DEFAULT GETDATE(),
        [UpdatedAt]       DATETIME  NULL,
        [IsArchived]      BIT       NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Visit] PRIMARY KEY CLUSTERED ([Id] ASC),
        -- FK to Patient (no cascade)
        CONSTRAINT [FK_Visit_Patient] FOREIGN KEY ([PatientId])
            REFERENCES [dbo].[Patient] ([Id]),
        -- FK to Appointment (optional, no cascade)
        CONSTRAINT [FK_Visit_Appointment] FOREIGN KEY ([AppointmentId])
            REFERENCES [dbo].[Appointment] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Visit_PatientId]
        ON [dbo].[Visit] ([PatientId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Visit_AppointmentId]
        ON [dbo].[Visit] ([AppointmentId] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 8. Prescription  (One prescription per visit)
--    Entity:  EyeClinic.Core.Entities.Prescription
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'Prescription')
BEGIN
    CREATE TABLE [dbo].[Prescription] (
        [Id]        INT      IDENTITY(1,1) NOT NULL,
        [VisitId]   INT      NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Prescription] PRIMARY KEY CLUSTERED ([Id] ASC),
        -- FK to Visit (no cascade per convention removal)
        CONSTRAINT [FK_Prescription_Visit] FOREIGN KEY ([VisitId])
            REFERENCES [dbo].[Visit] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Prescription_VisitId]
        ON [dbo].[Prescription] ([VisitId] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 9. PrescriptionItem  (Individual medicines in a prescription)
--    Entity:  EyeClinic.Core.Entities.PrescriptionItem
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'PrescriptionItem')
BEGIN
    CREATE TABLE [dbo].[PrescriptionItem] (
        [Id]               INT            IDENTITY(1,1) NOT NULL,
        [PrescriptionId]   INT            NOT NULL,
        [MedicineName]     NVARCHAR(MAX)  NULL,
        [DoseInstructions] NVARCHAR(MAX)  NULL,
        [OrderIndex]       INT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_PrescriptionItem] PRIMARY KEY CLUSTERED ([Id] ASC),
        -- FK to Prescription (cascade delete per Fluent API)
        CONSTRAINT [FK_PrescriptionItem_Prescription] FOREIGN KEY ([PrescriptionId])
            REFERENCES [dbo].[Prescription] ([Id])
            ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_PrescriptionItem_PrescriptionId]
        ON [dbo].[PrescriptionItem] ([PrescriptionId] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 10. PrescriptionTemplate  (Saved prescription templates)
--     Entity:  EyeClinic.Core.Entities.PrescriptionTemplate
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'PrescriptionTemplate')
BEGIN
    CREATE TABLE [dbo].[PrescriptionTemplate] (
        [Id]           INT            IDENTITY(1,1) NOT NULL,
        [TemplateName] NVARCHAR(MAX)  NULL,
        [ItemsJson]    NVARCHAR(MAX)  NULL,
        [CreatedAt]    DATETIME       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_PrescriptionTemplate] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- -----------------------------------------------------------------------
-- 11. MedicineCatalog  (Auto-learning medication dictionary)
--     Entity:  EyeClinic.Core.Entities.MedicineCatalog
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'MedicineCatalog')
BEGIN
    CREATE TABLE [dbo].[MedicineCatalog] (
        [Id]           INT            IDENTITY(1,1) NOT NULL,
        [MedicineName] NVARCHAR(MAX)  NULL,
        [DefaultDose]  NVARCHAR(MAX)  NULL,
        [UsageCount]   INT            NOT NULL DEFAULT 1,
        [LastUsedAt]   DATETIME       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_MedicineCatalog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- -----------------------------------------------------------------------
-- 12. DiagnosisCatalog  (Auto-learning diagnosis dictionary)
--     Entity:  EyeClinic.Core.Entities.DiagnosisCatalog
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'DiagnosisCatalog')
BEGIN
    CREATE TABLE [dbo].[DiagnosisCatalog] (
        [Id]            INT            IDENTITY(1,1) NOT NULL,
        [DiagnosisName] NVARCHAR(MAX)  NULL,
        [UsageCount]    INT            NOT NULL DEFAULT 1,
        [LastUsedAt]    DATETIME       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_DiagnosisCatalog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- -----------------------------------------------------------------------
-- 13. DailyClosing  (End-of-day cash reconciliation)
--     Entity:  EyeClinic.Core.Entities.DailyClosing
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'DailyClosing')
BEGIN
    CREATE TABLE [dbo].[DailyClosing] (
        [Id]               INT            IDENTITY(1,1) NOT NULL,
        [ClosingDate]      DATETIME       NOT NULL,
        [ExpectedCash]     DECIMAL(18,2)  NOT NULL,                -- HasPrecision(18,2)
        [ActualCash]       DECIMAL(18,2)  NOT NULL,                -- HasPrecision(18,2)
        [CashDifference]   DECIMAL(18,2)  NOT NULL,                -- HasPrecision(18,2)
        [VodafoneCashTotal] DECIMAL(18,2) NOT NULL,                -- HasPrecision(18,2)
        [InstaPayTotal]    DECIMAL(18,2)  NOT NULL,                -- HasPrecision(18,2)
        [Notes]            NVARCHAR(MAX)  NULL,
        [ClosedByUserId]   INT            NOT NULL,
        [ClosedAt]         DATETIME       NOT NULL DEFAULT GETDATE(),
        [IsReopened]       BIT            NOT NULL DEFAULT 0,
        [ReopenedByUserId] INT            NULL,
        [ReopenedAt]       DATETIME       NULL,
        CONSTRAINT [PK_DailyClosing] PRIMARY KEY CLUSTERED ([Id] ASC),
        -- FK to User (closed by, no cascade)
        CONSTRAINT [FK_DailyClosing_ClosedByUser] FOREIGN KEY ([ClosedByUserId])
            REFERENCES [dbo].[User] ([Id]),
        -- FK to User (reopened by, optional, no cascade)
        CONSTRAINT [FK_DailyClosing_ReopenedByUser] FOREIGN KEY ([ReopenedByUserId])
            REFERENCES [dbo].[User] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_DailyClosing_ClosingDate]
        ON [dbo].[DailyClosing] ([ClosingDate] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 14. AuditLog  (Immutable audit trail)
--     Entity:  EyeClinic.Core.Entities.AuditLog
--     Note:    Id is BIGINT (C# long), not INT
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'AuditLog')
BEGIN
    CREATE TABLE [dbo].[AuditLog] (
        [Id]             BIGINT         IDENTITY(1,1) NOT NULL,
        [UserId]         INT            NULL,
        [Username]       NVARCHAR(MAX)  NULL,
        [ActionType]     INT            NOT NULL,                  -- AuditActionType enum (1-19)
        [EntityName]     NVARCHAR(MAX)  NULL,
        [RecordId]       NVARCHAR(MAX)  NULL,
        [OldValuesJson]  NVARCHAR(MAX)  NULL,
        [NewValuesJson]  NVARCHAR(MAX)  NULL,
        [Description]    NVARCHAR(MAX)  NULL,
        [Timestamp]      DATETIME       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_AuditLog_Timestamp]
        ON [dbo].[AuditLog] ([Timestamp] DESC);
    CREATE NONCLUSTERED INDEX [IX_AuditLog_UserId]
        ON [dbo].[AuditLog] ([UserId] ASC);
END
GO

-- -----------------------------------------------------------------------
-- 15. ClinicSetting  (Key-value configuration store)
--     Entity:  EyeClinic.Core.Entities.ClinicSetting
--     PK:      SettingKey (string), MaxLength(128)
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'ClinicSetting')
BEGIN
    CREATE TABLE [dbo].[ClinicSetting] (
        [SettingKey]   NVARCHAR(128)  NOT NULL,
        [SettingValue] NVARCHAR(MAX)  NULL,
        [Description]  NVARCHAR(MAX)  NULL,
        [GroupName]    NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_ClinicSetting] PRIMARY KEY CLUSTERED ([SettingKey] ASC)
    );
END
GO

-- =======================================================================
-- Schema creation complete.
-- No Stored Procedures, Views, or Triggers are used by the application.
-- All data access is handled via Entity Framework 6 LINQ-to-SQL.
-- The only raw SQL query is in DbInitializer.cs:
--   "SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClinicSetting'"
-- =======================================================================

PRINT N'✔ EyeClinicDb schema created successfully.';
GO
