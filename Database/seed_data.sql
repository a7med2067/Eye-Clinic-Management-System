-- =======================================================================
-- نظام إدارة عيادة العيون (Eye Clinic Management System)
-- seed_data.sql — Demo Data & Pre-configured Login Accounts
-- =======================================================================
-- Target:  SQL Server 2014 Express (Compatibility Level 120)
-- Prerequisite: Run database.sql first to create the schema.
-- =======================================================================
-- PASSWORD HASHING METHOD:
--   Algorithm: PBKDF2 (Rfc2898DeriveBytes)
--   Salt size: 24 bytes (random)
--   Hash size: 32 bytes
--   Iterations: 10,000
--   Storage: Base64-encoded strings in PasswordHash and PasswordSalt columns
--
--   Source: src/EyeClinic.Data/Security/PasswordHasher.cs
--   Login:  Username is compared exactly (case-sensitive in C# code),
--           password is verified via PasswordHasher.VerifyPassword()
-- =======================================================================

USE [EyeClinicDb];
GO

SET NOCOUNT ON;

-- ===========================
-- 1. DEMO LOGIN ACCOUNTS
-- ===========================
-- +===========+==============+================+======+=======================+
-- | Role      | Username     | Password       | Role | MustChangePassword    |
-- +===========+==============+================+======+=======================+
-- | Doctor    | doctor       | Doctor@123     | 1    | 0 (no forced change)  |
-- | Secretary | secretary    | Secretary@123  | 2    | 0 (no forced change)  |
-- +===========+==============+================+======+=======================+
--
-- Hashes below were generated with the exact same PBKDF2 parameters as the app:
--   Salt = 24 random bytes, Hash = PBKDF2(password, salt, 10000 iterations, 32 bytes)
--   Both stored as Base64 strings.

IF NOT EXISTS (SELECT 1 FROM [dbo].[User] WHERE [Username] = N'doctor')
BEGIN
    INSERT INTO [dbo].[User]
        ([Username], [FullName], [PasswordHash], [PasswordSalt], [Role], [IsActive], [MustChangePassword], [FailedLoginAttempts], [CreatedAt])
    VALUES
        (N'doctor',
         N'د. أحمد - طبيب العيون',
         N'V98W9ADLQ7gYC3HVl/FKUeDpGFUtYBri5MM0+WUQGxE=',
         N'zHMKO1NsfK3uRHSQc6q/bXUH05rAXImV',
         1,  -- UserRole.Doctor
         1,  -- IsActive
         0,  -- MustChangePassword = false (demo ready)
         0,  -- FailedLoginAttempts
         GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[User] WHERE [Username] = N'secretary')
BEGIN
    INSERT INTO [dbo].[User]
        ([Username], [FullName], [PasswordHash], [PasswordSalt], [Role], [IsActive], [MustChangePassword], [FailedLoginAttempts], [CreatedAt])
    VALUES
        (N'secretary',
         N'سارة محمد - موظفة الاستقبال',
         N'XeTOFLk19zCF6pOFviN9Y3vX6CPdNox4re5lnRw3URM=',
         N'zHSIOGXhdqLWjVF4wjTsJ0ntrg/qj4v6',
         2,  -- UserRole.Secretary
         1,  -- IsActive
         0,  -- MustChangePassword = false (demo ready)
         0,  -- FailedLoginAttempts
         GETDATE());
END
GO

-- ===========================
-- 2. CLINIC SETTINGS (Seed)
-- ===========================
-- These are the same defaults seeded by DbInitializer.SeedSettings()

IF NOT EXISTS (SELECT 1 FROM [dbo].[ClinicSetting] WHERE [SettingKey] = N'Clinic_Name')
BEGIN
    INSERT INTO [dbo].[ClinicSetting] ([SettingKey], [SettingValue], [Description], [GroupName]) VALUES
    (N'Clinic_Name',        N'عيادة العيون التخصصية',                            N'اسم العيادة المطبوع على الإيصالات والواجهة', N'General'),
    (N'Clinic_DoctorName',  N'د. استشاري طب وجراحة العيون',                      N'اسم الطبيب',                                 N'General'),
    (N'Clinic_Phone',       N'01000000000',                                       N'هاتف العيادة',                               N'General'),
    (N'Clinic_Address',     N'جمهورية مصر العربية',                              N'عنوان العيادة',                              N'General'),
    (N'Price_Exam',         N'300',                                               N'سعر الكشف الافتراضي (جنيه)',                 N'Pricing'),
    (N'Price_ReExam',       N'50',                                                N'سعر الإعادة الافتراضي (جنيه)',               N'Pricing'),
    (N'Price_Urgent',       N'450',                                               N'سعر الكشف المستعجل الافتراضي (جنيه)',        N'Pricing'),
    (N'ReExam_ValidDays',   N'14',                                                N'فترة صلاحية الإعادة بالأيام من آخر كشف مدفوع', N'Rules'),
    (N'Max_DailyPatients',  N'0',                                                 N'الحد الأقصى لمرضى اليوم (0 يعني لا يوجد حد)', N'Rules'),
    (N'InactivityTimeout_DoctorMinutes',    N'10',                                N'مهلة الخمول بالدقائق لحساب الطبيب',          N'Security'),
    (N'InactivityTimeout_SecretaryMinutes', N'30',                                N'مهلة الخمول بالدقائق لحساب السكرتارية',      N'Security'),
    (N'Print_ReceiptEnabled',              N'True',                               N'تفعيل طباعة الإيصال الحراري',                N'Printing'),
    (N'Print_Rx_Offset_X_MM',  N'0',   N'إزاحة أفقية لمعايرة الروشتة (مليمتر)',   N'Calibration'),
    (N'Print_Rx_Offset_Y_MM',  N'0',   N'إزاحة رأسية لمعايرة الروشتة (مليمتر)',   N'Calibration'),
    (N'Print_Rx_Name_X_MM',    N'35',  N'إحداثي X لاسم المريض (مليمتر)',           N'Calibration'),
    (N'Print_Rx_Name_Y_MM',    N'38',  N'إحداثي Y لاسم المريض (مليمتر)',           N'Calibration'),
    (N'Print_Rx_Age_X_MM',     N'110', N'إحداثي X للسن (مليمتر)',                 N'Calibration'),
    (N'Print_Rx_Age_Y_MM',     N'38',  N'إحداثي Y للسن (مليمتر)',                 N'Calibration'),
    (N'Print_Rx_Diagnosis_X_MM', N'42', N'إحداثي X للتشخيص (مليمتر)',             N'Calibration'),
    (N'Print_Rx_Diagnosis_Y_MM', N'46', N'إحداثي Y للتشخيص (مليمتر)',             N'Calibration'),
    (N'Print_Rx_Date_X_MM',    N'112', N'إحداثي X للتاريخ (مليمتر)',              N'Calibration'),
    (N'Print_Rx_Date_Y_MM',    N'46',  N'إحداثي Y للتاريخ (مليمتر)',              N'Calibration'),
    (N'Print_Rx_Box_X_MM',     N'17',  N'إحداثي X لصندوق الأدوية (مليمتر)',       N'Calibration'),
    (N'Print_Rx_Box_Y_MM',     N'69',  N'إحداثي Y لصندوق الأدوية (مليمتر)',       N'Calibration'),
    (N'Print_Rx_Box_Width_MM', N'130', N'عرض صندوق الأدوية (مليمتر)',             N'Calibration'),
    (N'Print_Rx_Box_Height_MM',N'130', N'ارتفاع صندوق الأدوية (مليمتر)',          N'Calibration'),
    (N'Database_Version',      N'1.0.0', N'إصدار قاعدة البيانات لمطابقة التطبيق', N'System');
END
GO

-- ===========================
-- 3. SAMPLE PATIENTS
-- ===========================
-- Realistic Egyptian names, mobile numbers, and clinical data

DECLARE @DoctorUserId INT;
SELECT @DoctorUserId = [Id] FROM [dbo].[User] WHERE [Username] = N'doctor';
IF @DoctorUserId IS NULL SET @DoctorUserId = 1;

DECLARE @SecretaryUserId INT;
SELECT @SecretaryUserId = [Id] FROM [dbo].[User] WHERE [Username] = N'secretary';
IF @SecretaryUserId IS NULL SET @SecretaryUserId = 2;

-- Patient 1
IF NOT EXISTS (SELECT 1 FROM [dbo].[Patient] WHERE [FileNumber] = N'1001')
BEGIN
    INSERT INTO [dbo].[Patient]
        ([FileNumber], [FullName], [Mobile], [BirthDate], [Gender], [Address], [NationalId], [GeneralNotes], [ChronicDiseases], [DrugAllergies], [IsArchived], [CreatedAt])
    VALUES
        (N'1001', N'محمد أحمد إبراهيم', N'01012345678', '1975-03-15', N'ذكر', N'طنطا', N'27503150100000', N'مريض منتظم', N'السكري - النوع الثاني', NULL, 0, GETDATE());
END

-- Patient 2
IF NOT EXISTS (SELECT 1 FROM [dbo].[Patient] WHERE [FileNumber] = N'1002')
BEGIN
    INSERT INTO [dbo].[Patient]
        ([FileNumber], [FullName], [Mobile], [BirthDate], [Gender], [Address], [GeneralNotes], [IsArchived], [CreatedAt])
    VALUES
        (N'1002', N'فاطمة حسن علي', N'01098765432', '1990-07-22', N'أنثى', N'المحلة الكبرى', N'تحتاج نظارة قراءة', 0, GETDATE());
END

-- Patient 3
IF NOT EXISTS (SELECT 1 FROM [dbo].[Patient] WHERE [FileNumber] = N'1003')
BEGIN
    INSERT INTO [dbo].[Patient]
        ([FileNumber], [FullName], [Mobile], [BirthDate], [Gender], [Address], [ChronicDiseases], [DrugAllergies], [IsArchived], [CreatedAt])
    VALUES
        (N'1003', N'عبد الرحمن محمود سالم', N'01155667788', '1960-11-05', N'ذكر', N'كفر الزيات', N'ضغط دم مرتفع', N'حساسية بنسلين', 0, GETDATE());
END

-- Patient 4
IF NOT EXISTS (SELECT 1 FROM [dbo].[Patient] WHERE [FileNumber] = N'1004')
BEGIN
    INSERT INTO [dbo].[Patient]
        ([FileNumber], [FullName], [Mobile], [BirthDate], [Gender], [Address], [IsArchived], [CreatedAt])
    VALUES
        (N'1004', N'نورهان سعيد عبد الله', N'01223344556', '2000-01-10', N'أنثى', N'زفتى', 0, GETDATE());
END

-- Patient 5
IF NOT EXISTS (SELECT 1 FROM [dbo].[Patient] WHERE [FileNumber] = N'1005')
BEGIN
    INSERT INTO [dbo].[Patient]
        ([FileNumber], [FullName], [Mobile], [BirthDate], [Gender], [Address], [ChronicDiseases], [PastSurgeries], [IsArchived], [CreatedAt])
    VALUES
        (N'1005', N'حسين محمد عبد الغني', N'01011223344', '1950-06-20', N'ذكر', N'سمنود', N'السكري، ضغط الدم', N'عملية مياه بيضاء بالعين اليمنى (2020)', 0, GETDATE());
END
GO

-- ===========================
-- 4. SAMPLE APPOINTMENTS (Today)
-- ===========================
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @SecUserId INT;
SELECT @SecUserId = [Id] FROM [dbo].[User] WHERE [Username] = N'secretary';
IF @SecUserId IS NULL SET @SecUserId = 2;

-- Appointment 1: Patient 1001, Waiting (paid)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Appointment] WHERE [PatientId] = 1 AND CAST([AppointmentDate] AS DATE) = @Today)
BEGIN
    INSERT INTO [dbo].[Appointment] ([PatientId], [AppointmentDate], [TurnNumber], [Status], [IsUrgent], [CreatedAt])
    VALUES (1, @Today, 1, 2, 0, GETDATE()); -- Status=2 (Waiting)
END

-- Appointment 2: Patient 1002, Waiting
IF NOT EXISTS (SELECT 1 FROM [dbo].[Appointment] WHERE [PatientId] = 2 AND CAST([AppointmentDate] AS DATE) = @Today)
BEGIN
    INSERT INTO [dbo].[Appointment] ([PatientId], [AppointmentDate], [TurnNumber], [Status], [IsUrgent], [CreatedAt])
    VALUES (2, @Today, 2, 2, 0, GETDATE()); -- Status=2 (Waiting)
END

-- Appointment 3: Patient 1003, Urgent Waiting
IF NOT EXISTS (SELECT 1 FROM [dbo].[Appointment] WHERE [PatientId] = 3 AND CAST([AppointmentDate] AS DATE) = @Today)
BEGIN
    INSERT INTO [dbo].[Appointment] ([PatientId], [AppointmentDate], [TurnNumber], [Status], [IsUrgent], [CreatedAt])
    VALUES (3, @Today, 3, 2, 1, GETDATE()); -- Status=2 (Waiting), Urgent
END

-- Appointment 4: Patient 1004, Booked Unpaid
IF NOT EXISTS (SELECT 1 FROM [dbo].[Appointment] WHERE [PatientId] = 4 AND CAST([AppointmentDate] AS DATE) = @Today)
BEGIN
    INSERT INTO [dbo].[Appointment] ([PatientId], [AppointmentDate], [TurnNumber], [Status], [IsUrgent], [CreatedAt])
    VALUES (4, @Today, 4, 1, 0, GETDATE()); -- Status=1 (BookedUnpaid)
END
GO

-- ===========================
-- 5. SAMPLE PAYMENTS & RECEIPTS
-- ===========================
DECLARE @SecId INT;
SELECT @SecId = [Id] FROM [dbo].[User] WHERE [Username] = N'secretary';
IF @SecId IS NULL SET @SecId = 2;

-- Payment for Appointment 1 (Exam, Cash, 300 EGP)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Payment] WHERE [AppointmentId] = 1)
BEGIN
    INSERT INTO [dbo].[Payment]
        ([AppointmentId], [PatientId], [ExamType], [StandardPrice], [ActualPrice], [Method], [IsRefunded], [CreatedByUserId], [CreatedAt])
    VALUES (1, 1, 1, 300.00, 300.00, 1, 0, @SecId, GETDATE()); -- ExamType=1 (Exam), Method=1 (Cash)

    INSERT INTO [dbo].[Receipt] ([ReceiptNumber], [PaymentId], [IssuedAt], [PrintCount])
    VALUES (10001, SCOPE_IDENTITY(), GETDATE(), 1);
END

-- Payment for Appointment 2 (Exam, VodafoneCash, 300 EGP)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Payment] WHERE [AppointmentId] = 2)
BEGIN
    INSERT INTO [dbo].[Payment]
        ([AppointmentId], [PatientId], [ExamType], [StandardPrice], [ActualPrice], [Method], [IsRefunded], [CreatedByUserId], [CreatedAt])
    VALUES (2, 2, 1, 300.00, 300.00, 2, 0, @SecId, GETDATE()); -- Method=2 (VodafoneCash)

    INSERT INTO [dbo].[Receipt] ([ReceiptNumber], [PaymentId], [IssuedAt], [PrintCount])
    VALUES (10002, SCOPE_IDENTITY(), GETDATE(), 1);
END

-- Payment for Appointment 3 (Urgent, Cash, 450 EGP)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Payment] WHERE [AppointmentId] = 3)
BEGIN
    INSERT INTO [dbo].[Payment]
        ([AppointmentId], [PatientId], [ExamType], [StandardPrice], [ActualPrice], [Method], [IsRefunded], [CreatedByUserId], [CreatedAt])
    VALUES (3, 3, 3, 450.00, 450.00, 1, 0, @SecId, GETDATE()); -- ExamType=3 (Urgent)

    INSERT INTO [dbo].[Receipt] ([ReceiptNumber], [PaymentId], [IssuedAt], [PrintCount])
    VALUES (10003, SCOPE_IDENTITY(), GETDATE(), 1);
END
GO

-- ===========================
-- 6. SAMPLE VISIT (Past completed visit for Patient 1005)
-- ===========================
DECLARE @DocId INT;
SELECT @DocId = [Id] FROM [dbo].[User] WHERE [Username] = N'doctor';
IF @DocId IS NULL SET @DocId = 1;

-- A past appointment for patient 5 (yesterday, completed)
DECLARE @Yesterday DATE = DATEADD(DAY, -1, CAST(GETDATE() AS DATE));

IF NOT EXISTS (SELECT 1 FROM [dbo].[Appointment] WHERE [PatientId] = 5)
BEGIN
    INSERT INTO [dbo].[Appointment] ([PatientId], [AppointmentDate], [TurnNumber], [Status], [IsUrgent], [CreatedAt])
    VALUES (5, @Yesterday, 1, 4, 0, DATEADD(DAY, -1, GETDATE())); -- Status=4 (Completed)

    DECLARE @PastApptId INT = SCOPE_IDENTITY();

    -- Payment
    INSERT INTO [dbo].[Payment]
        ([AppointmentId], [PatientId], [ExamType], [StandardPrice], [ActualPrice], [Method], [IsRefunded], [CreatedByUserId], [CreatedAt])
    VALUES (@PastApptId, 5, 1, 300.00, 300.00, 1, 0, @DocId, DATEADD(DAY, -1, GETDATE()));

    INSERT INTO [dbo].[Receipt] ([ReceiptNumber], [PaymentId], [IssuedAt], [PrintCount])
    VALUES (10000, SCOPE_IDENTITY(), DATEADD(DAY, -1, GETDATE()), 1);

    -- Visit record
    INSERT INTO [dbo].[Visit]
        ([PatientId], [AppointmentId], [ChiefComplaint], [Diagnosis],
         [VA_OD], [VA_OS], [IOP_OD], [IOP_OS],
         [DoctorNotes], [FollowUpDate], [VisitDate], [CreatedByUserId], [CreatedAt], [IsArchived])
    VALUES
        (5, @PastApptId,
         N'ضعف بالرؤية تدريجي بالعين اليسرى منذ 3 أشهر',
         N'مياه بيضاء شيخوخية (Senile Cataract)',
         N'6/12', N'6/36', N'14', N'16',
         N'مياه بيضاء ناضجة بالعين اليسرى - ينصح بالتدخل الجراحي. متابعة بعد أسبوعين.',
         DATEADD(DAY, 13, @Yesterday),
         @Yesterday, @DocId, DATEADD(DAY, -1, GETDATE()), 0);

    -- Prescription for the visit
    DECLARE @PastVisitId INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[Prescription] ([VisitId], [CreatedAt])
    VALUES (@PastVisitId, DATEADD(DAY, -1, GETDATE()));

    DECLARE @PastRxId INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[PrescriptionItem] ([PrescriptionId], [MedicineName], [DoseInstructions], [OrderIndex]) VALUES
    (@PastRxId, N'Tobradex Eye Drops',      N'نقطة 4 مرات يومياً لمدة أسبوع',   1),
    (@PastRxId, N'Hyfresh Eye Drops',        N'نقطة 4 مرات يومياً للترطيب',       2),
    (@PastRxId, N'Pred Forte 1% Eye Drops',  N'نقطة 4 مرات يومياً ثم تقليل الجرعة تدريجياً', 3);
END
GO

-- ===========================
-- 7. SAMPLE MEDICINE & DIAGNOSIS CATALOGS
-- ===========================
-- (These are auto-seeded by CatalogService on first app launch,
--  but we insert a few here so the database is complete standalone.)

IF NOT EXISTS (SELECT 1 FROM [dbo].[MedicineCatalog] WHERE [MedicineName] = N'Tobradex Eye Drops')
BEGIN
    INSERT INTO [dbo].[MedicineCatalog] ([MedicineName], [DefaultDose], [UsageCount], [LastUsedAt]) VALUES
    (N'Tobradex Eye Drops',      N'نقطة 4 مرات يومياً لمدة أسبوع',                     10, GETDATE()),
    (N'Vigamox Eye Drops',       N'نقطة 3 مرات يومياً لمدة أسبوع',                      9, GETDATE()),
    (N'Pred Forte 1% Eye Drops', N'نقطة 4 مرات يومياً ثم تقليل الجرعة تدريجياً',         9, GETDATE()),
    (N'Hyfresh Eye Drops',       N'نقطة 4 مرات يومياً للترطيب',                         10, GETDATE()),
    (N'Systane Ultra Eye Drops', N'نقطة عند اللزوم للترطيب',                              9, GETDATE()),
    (N'Xalatan 0.005% Eye Drops', N'نقطة واحدة مساءً قبل النوم',                         9, GETDATE()),
    (N'Patanol 0.1% Eye Drops',  N'نقطة مرتين يومياً للحساسية',                          8, GETDATE()),
    (N'Cosopt Eye Drops',         N'نقطة مرتين يومياً',                                   8, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[DiagnosisCatalog] WHERE [DiagnosisName] = N'جفاف العين المزمن (Dry Eye Syndrome)')
BEGIN
    INSERT INTO [dbo].[DiagnosisCatalog] ([DiagnosisName], [UsageCount], [LastUsedAt]) VALUES
    (N'جفاف العين المزمن (Dry Eye Syndrome)',                              10, GETDATE()),
    (N'التهاب الملتحمة البكتيري (Bacterial Conjunctivitis)',               10, GETDATE()),
    (N'التهاب الملتحمة التحسسي (Allergic Conjunctivitis)',                10, GETDATE()),
    (N'مياه بيضاء شيخوخية (Senile Cataract)',                               8, GETDATE()),
    (N'مياه زرقاء - زرق مفتوح الزاوية (Primary Open Angle Glaucoma)',       8, GETDATE()),
    (N'خطأ انكساري - قصر نظر (Myopia)',                                    10, GETDATE()),
    (N'اعتلال الشبكية السكري (Diabetic Retinopathy)',                       9, GETDATE()),
    (N'كيس دهني بالجفن (Chalazion)',                                        9, GETDATE());
END
GO

-- ===========================
-- 8. AUDIT LOG (seed login)
-- ===========================
INSERT INTO [dbo].[AuditLog]
    ([UserId], [Username], [ActionType], [EntityName], [RecordId], [Description], [Timestamp])
VALUES
    (NULL, N'System', 15, N'Database', N'Seed', N'تم تهيئة قاعدة البيانات وبيانات العرض التجريبي بنجاح', GETDATE());
GO

PRINT N'';
PRINT N'✔ Seed data inserted successfully.';
PRINT N'';
PRINT N'╔══════════════════════════════════════════════════════════════╗';
PRINT N'║          DEMO LOGIN CREDENTIALS                            ║';
PRINT N'╠══════════════════════════════════════════════════════════════╣';
PRINT N'║  Role      │ Username    │ Password                        ║';
PRINT N'╠══════════════════════════════════════════════════════════════╣';
PRINT N'║  Doctor    │ doctor      │ Doctor@123                      ║';
PRINT N'║  Secretary │ secretary   │ Secretary@123                   ║';
PRINT N'╚══════════════════════════════════════════════════════════════╝';
PRINT N'';
PRINT N'  Hashing: PBKDF2 (Rfc2898DeriveBytes, 10000 iterations, 24-byte salt, 32-byte hash)';
PRINT N'  Login field: Username (exact match, case-sensitive)';
GO
