-- =======================================================================
-- نظام إدارة عيادة العيون (Eye Clinic Management System)
-- ملف إنشاء وتهيئة قاعدة البيانات - SQL Server 2014 Express (Level 120)
-- متوافق تماماً مع: Windows 7 SP1 / Windows 10 / .NET Framework 4.8 / EF 6.4.4
-- =======================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EyeClinicDb')
BEGIN
    CREATE DATABASE [EyeClinicDb]
    COLLATE Arabic_CI_AS;
END
GO

ALTER DATABASE [EyeClinicDb] SET COMPATIBILITY_LEVEL = 120;
GO

USE [EyeClinicDb];
GO

-- 1. جدول المستخدمين (Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'User')
BEGIN
    CREATE TABLE [dbo].[User] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Username] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [PasswordHash] NVARCHAR(256) NOT NULL,
        [PasswordSalt] NVARCHAR(128) NOT NULL,
        [Role] INT NOT NULL, -- 1: Doctor, 2: Secretary
        [IsActive] BIT NOT NULL DEFAULT 1,
        [MustChangePassword] BIT NOT NULL DEFAULT 1,
        [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
        [LockoutEndUtc] DATETIME NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_User_Username] ON [dbo].[User] ([Username] ASC);
END
GO

-- 2. جدول المرضى (Patients)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Patient')
BEGIN
    CREATE TABLE [dbo].[Patient] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FileNumber] NVARCHAR(20) NOT NULL,
        [FullName] NVARCHAR(150) NOT NULL,
        [Mobile] NVARCHAR(20) NOT NULL,
        [BirthDate] DATE NULL,
        [Gender] NVARCHAR(10) NULL,
        [Address] NVARCHAR(250) NULL,
        [NationalId] NVARCHAR(20) NULL,
        [GeneralNotes] NVARCHAR(MAX) NULL,
        -- حقول طبية خاصة بالطبيب فقط
        [ChronicDiseases] NVARCHAR(MAX) NULL,
        [DrugAllergies] NVARCHAR(MAX) NULL,
        [PastSurgeries] NVARCHAR(MAX) NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT [PK_Patient] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_Patient_FileNumber] ON [dbo].[Patient] ([FileNumber] ASC);
    CREATE NONCLUSTERED INDEX [IX_Patient_FullName] ON [dbo].[Patient] ([FullName] ASC);
    CREATE NONCLUSTERED INDEX [IX_Patient_Mobile] ON [dbo].[Patient] ([Mobile] ASC);
END
GO

-- 3. جدول الحجوزات وقائمة الانتظار (Appointments)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Appointment')
BEGIN
    CREATE TABLE [dbo].[Appointment] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PatientId] INT NOT NULL,
        [TurnNumber] INT NOT NULL,
        [AppointmentDate] DATE NOT NULL,
        [Status] INT NOT NULL DEFAULT 1, -- 1: Booked, 2: Waiting, 3: InExam, 4: Completed, 5: NoShow, 6: Cancelled
        [ExamType] INT NOT NULL DEFAULT 1, -- 1: Exam, 2: ReExam, 3: Urgent
        [IsPaid] BIT NOT NULL DEFAULT 0,
        [ArrivalTime] DATETIME NULL,
        [ExamStartTime] DATETIME NULL,
        [ExamEndTime] DATETIME NULL,
        [CreatedByUserId] INT NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT [PK_Appointment] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Appointment_Patient] FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patient] ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Appointment_Date_Status] ON [dbo].[Appointment] ([AppointmentDate] ASC, [Status] ASC);
END
GO

-- 4. جدول المدفوعات (Payments)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Payment')
BEGIN
    CREATE TABLE [dbo].[Payment] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AppointmentId] INT NOT NULL,
        [PatientId] INT NOT NULL,
        [ExamType] INT NOT NULL,
        [StandardPrice] DECIMAL(18,2) NOT NULL,
        [ActualPrice] DECIMAL(18,2) NOT NULL,
        [Method] INT NOT NULL, -- 1: Cash, 2: VodafoneCash, 3: InstaPay
        [ReferenceNumber] NVARCHAR(50) NULL,
        [IsOverridden] BIT NOT NULL DEFAULT 0,
        [OverrideReason] NVARCHAR(250) NULL,
        [IsRefunded] BIT NOT NULL DEFAULT 0,
        [RefundReason] NVARCHAR(250) NULL,
        [RefundedAt] DATETIME NULL,
        [RefundedByUserId] INT NULL,
        [PaymentDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ReceivedByUserId] INT NOT NULL,
        CONSTRAINT [PK_Payment] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Payment_Appointment] FOREIGN KEY ([AppointmentId]) REFERENCES [dbo].[Appointment] ([Id]),
        CONSTRAINT [FK_Payment_Patient] FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patient] ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Payment_Date] ON [dbo].[Payment] ([PaymentDate] ASC);
END
GO

-- 5. جدول الإيصالات غير القابلة للثغرات (Receipts)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Receipt')
BEGIN
    CREATE TABLE [dbo].[Receipt] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ReceiptNumber] NVARCHAR(50) NOT NULL,
        [PaymentId] INT NOT NULL,
        [SequenceNumber] INT NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [IsVoid] BIT NOT NULL DEFAULT 0,
        [VoidReason] NVARCHAR(250) NULL,
        CONSTRAINT [PK_Receipt] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Receipt_Payment] FOREIGN KEY ([PaymentId]) REFERENCES [dbo].[Payment] ([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Receipt_SequenceNumber] ON [dbo].[Receipt] ([SequenceNumber] ASC);
END
GO

-- 6. جدول الكشوفات والزيارات الطبية (Visits)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Visit')
BEGIN
    CREATE TABLE [dbo].[Visit] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PatientId] INT NOT NULL,
        [AppointmentId] INT NOT NULL,
        [VisitDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ChiefComplaint] NVARCHAR(MAX) NULL,
        [Diagnosis] NVARCHAR(MAX) NULL,
        [VisualAcuityRight] NVARCHAR(50) NULL,
        [VisualAcuityLeft] NVARCHAR(50) NULL,
        [IntraocularPressureRight] NVARCHAR(50) NULL,
        [IntraocularPressureLeft] NVARCHAR(50) NULL,
        [DoctorNotes] NVARCHAR(MAX) NULL,
        [FollowUpDate] DATE NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT [PK_Visit] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Visit_Patient] FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patient] ([Id]),
        CONSTRAINT [FK_Visit_Appointment] FOREIGN KEY ([AppointmentId]) REFERENCES [dbo].[Appointment] ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Visit_PatientId] ON [dbo].[Visit] ([PatientId] ASC);
END
GO

-- 7. جدول الروشتات (Prescriptions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Prescription')
BEGIN
    CREATE TABLE [dbo].[Prescription] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [VisitId] INT NOT NULL,
        [PrintCount] INT NOT NULL DEFAULT 0,
        [LastPrintedAt] DATETIME NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Prescription] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Prescription_Visit] FOREIGN KEY ([VisitId]) REFERENCES [dbo].[Visit] ([Id])
    );
END
GO

-- 8. بنود الروشتة (PrescriptionItems)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'PrescriptionItem')
BEGIN
    CREATE TABLE [dbo].[PrescriptionItem] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PrescriptionId] INT NOT NULL,
        [MedicineName] NVARCHAR(150) NOT NULL,
        [DoseInstructions] NVARCHAR(250) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_PrescriptionItem] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PrescriptionItem_Prescription] FOREIGN KEY ([PrescriptionId]) REFERENCES [dbo].[Prescription] ([Id])
    );
END
GO

-- 9. قواميس الأدوية والتشخيصات والروشتات الجاهزة
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'MedicineCatalog')
BEGIN
    CREATE TABLE [dbo].[MedicineCatalog] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [DefaultDose] NVARCHAR(250) NULL,
        [UsageCount] INT NOT NULL DEFAULT 1,
        [LastUsedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_MedicineCatalog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Medicine_Name] ON [dbo].[MedicineCatalog] ([Name] ASC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'DiagnosisCatalog')
BEGIN
    CREATE TABLE [dbo].[DiagnosisCatalog] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [UsageCount] INT NOT NULL DEFAULT 1,
        [LastUsedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_DiagnosisCatalog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Diagnosis_Name] ON [dbo].[DiagnosisCatalog] ([Name] ASC);
END
GO

-- 10. جدول الإقفال اليومي (DailyClosing)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'DailyClosing')
BEGIN
    CREATE TABLE [dbo].[DailyClosing] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ClosingDate] DATE NOT NULL,
        [ExpectedCash] DECIMAL(18,2) NOT NULL,
        [ActualCash] DECIMAL(18,2) NOT NULL,
        [CashDifference] DECIMAL(18,2) NOT NULL,
        [VodafoneCashTotal] DECIMAL(18,2) NOT NULL,
        [InstaPayTotal] DECIMAL(18,2) NOT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [ClosedByUserId] INT NOT NULL,
        [ClosedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [IsReopened] BIT NOT NULL DEFAULT 0,
        [ReopenedByUserId] INT NULL,
        [ReopenedAt] DATETIME NULL,
        [ReopenReason] NVARCHAR(250) NULL,
        CONSTRAINT [PK_DailyClosing] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DailyClosing_Date] ON [dbo].[DailyClosing] ([ClosingDate] ASC);
END
GO

-- 11. جدول سجل المراجعة والتتبع غير القابل للتعديل (AuditLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'AuditLog')
BEGIN
    CREATE TABLE [dbo].[AuditLog] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NULL,
        [Username] NVARCHAR(50) NOT NULL,
        [ActionType] INT NOT NULL,
        [EntityName] NVARCHAR(50) NULL,
        [RecordId] INT NULL,
        [OldValues] NVARCHAR(MAX) NULL,
        [NewValues] NVARCHAR(MAX) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Timestamp] ON [dbo].[AuditLog] ([Timestamp] DESC);
END
GO

-- 12. جدول إعدادات العيادة (ClinicSetting)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'ClinicSetting')
BEGIN
    CREATE TABLE [dbo].[ClinicSetting] (
        [SettingKey] NVARCHAR(100) NOT NULL,
        [SettingValue] NVARCHAR(MAX) NULL,
        [Description] NVARCHAR(250) NULL,
        [GroupName] NVARCHAR(50) NULL,
        CONSTRAINT [PK_ClinicSetting] PRIMARY KEY CLUSTERED ([SettingKey] ASC)
    );
END
GO
