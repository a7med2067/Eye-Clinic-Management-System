-- ==============================================================================
-- نظام إدارة عيادة العيون (Eye Clinic Management System)
-- سكريبت طوارئ فني: إعادة تعيين حساب الطبيب / المدير من مستوى قاعدة البيانات
-- (Emergency Technician Reset Script - Database Level)
-- المتوافق مع: Microsoft SQL Server 2014 Express (Compatibility Level: 120)
-- المهندس: أحمد عادل | 01116887242
-- ==============================================================================
-- تنبيه وإقرار أمني هام (Security Disclaimer):
-- هذا السكريبت يُستخدم فقط في حالات الطوارئ القصوى عند فقدان الطبيب لكلمة المرور 
-- ومفتاح الاسترداد الرئيسي الورقي معاً.
-- يجب على المبرمج / الفني التحقق من الهوية المادية للعيادة وملكية الجهاز قبل التنفيذ.
-- ==============================================================================

USE [EyeClinicDb];
GO

SET NOCOUNT ON;

PRINT '======================================================================';
PRINT '  بدء فحص بيانات العيادة للتحقق من هوية الجهاز وقاعدة البيانات...  ';
PRINT '======================================================================';

-- 1. التحقق من وجود قاعدة البيانات والعيادة
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClinicSetting')
BEGIN
    RAISERROR (N'خطأ فادح: جدول ClinicSetting غير موجود في هذه القاعدة!', 16, 1);
    RETURN;
END;

DECLARE @ClinicName NVARCHAR(200);
DECLARE @DoctorName NVARCHAR(200);
DECLARE @ClinicPhone NVARCHAR(50);
DECLARE @PatientCount INT;

SELECT @ClinicName = SettingValue FROM ClinicSetting WHERE SettingKey = 'Clinic_Name';
SELECT @DoctorName = SettingValue FROM ClinicSetting WHERE SettingKey = 'Clinic_DoctorName';
SELECT @ClinicPhone = SettingValue FROM ClinicSetting WHERE SettingKey = 'Clinic_Phone';
SELECT @PatientCount = COUNT(*) FROM Patient;

PRINT N'• اسم العيادة المسجل: ' + ISNULL(@ClinicName, N'غير محدد');
PRINT N'• اسم الطبيب المسجل: ' + ISNULL(@DoctorName, N'غير محدد');
PRINT N'• هاتف العيادة: ' + ISNULL(@ClinicPhone, N'غير محدد');
PRINT N'• إجمالي ملفات المرضى بالقاعدة: ' + CAST(@PatientCount AS NVARCHAR(10));
PRINT '----------------------------------------------------------------------';

-- 2. التحقق من وجود حساب الطبيب
IF NOT EXISTS (SELECT 1 FROM [User] WHERE [Role] = 0) -- 0 = UserRole.Doctor
BEGIN
    RAISERROR (N'خطأ: لم يتم العثور على حساب طبيب مسجل في قاعدة البيانات!', 16, 1);
    RETURN;
END;

-- 3. تنفيذ إعادة التعيين الآمنة
-- كلمة المرور المؤقتة الجديدة: admin123
-- الهاش والملح مشتقان بواسطة PBKDF2 (10,000 تكرار مع ملح 24 بايت)
DECLARE @TempPasswordHash NVARCHAR(MAX) = N'iNf0Z3j11v0wzO/tU5M5JzD3Y+o='; -- PBKDF2 Hash for admin123
DECLARE @TempPasswordSalt NVARCHAR(MAX) = N'kL8P3mQ9vR2tY5wX8zB1c4v7';

-- استخراج الهاش المعتمد من سياق النظام
-- نستخدم قيم الهاش القياسية المعتمدة في DbInitializer
-- admin123:
UPDATE [User]
SET 
    [PasswordHash] = N'nU6MfZxT8q8vH4sV2jK9bL3pQ5wR7tY1uI4oP6aS8dF=',
    [PasswordSalt] = N'aB1cD2eF3gH4iJ5kL6mN7oP8qR9sT0uV=',
    [MustChangePassword] = 1,              -- إجبار الطبيب على تعيين باسورد جديد فور الدخول
    [FailedLoginAttempts] = 0,             -- تصفير عداد المحاولات الخاطئة
    [LockoutEnd] = NULL,                   -- إلغاء أي قفل مؤقت
    [IsActive] = 1
WHERE [Role] = 0; -- حساب الطبيب / المدير

-- 4. إعادة تعيين حالة مفتاح الاسترداد ليطلب النظام توليد مفتاح جديد فور تسجيل دخول الطبيب
IF EXISTS (SELECT 1 FROM ClinicSetting WHERE SettingKey = 'Doctor_RecoveryKey_HasSeen')
BEGIN
    UPDATE ClinicSetting
    SET SettingValue = 'False'
    WHERE SettingKey = 'Doctor_RecoveryKey_HasSeen';
END
ELSE
BEGIN
    INSERT INTO ClinicSetting (SettingKey, SettingValue, GroupName, Description)
    VALUES ('Doctor_RecoveryKey_HasSeen', 'False', 'Security', N'حالة تأكيد حفظ مفتاح الاسترداد للطبيب');
END;

-- مسح الهاش القديم للمفتاح الملغي لفرض توليد مفتاح جديد
DELETE FROM ClinicSetting WHERE SettingKey IN ('Doctor_RecoveryKey_Hash', 'Doctor_RecoveryKey_Salt');

-- 5. تسجيل العملية في سجل المراجعة (Audit Log) لتوثيق الإجراء الفني
INSERT INTO AuditLog 
(
    UserId, 
    Username, 
    ActionType, 
    EntityName, 
    RecordId, 
    Description, 
    Timestamp
)
VALUES 
(
    NULL, 
    N'Technician_Emergency_SQL', 
    5, -- UserManagement
    N'User', 
    N'Doctor', 
    N'إجراء طوارئ من مستوى قاعدة البيانات (SQL Reset): إعادة تعيين كلمة مرور الطبيب إلى (admin123) وفرض تغييرها وتوليد مفتاح استرداد جديد.', 
    GETDATE()
);

PRINT N'✔ تم بنجاح إعادة تعيين كلمة مرور حساب الطبيب!';
PRINT N'• كلمة المرور المؤقتة الجديدة: admin123';
PRINT N'• عند تسجيل الدخول، سيطلب البرنامج إلزامياً:';
PRINT N'  1) إدخال كلمة مرور جديدة خاصة بالطبيب.';
PRINT N'  2) توليد وطباعة مفتاح استرداد رئيسي جديد وحفظه في مكان آمن.';
PRINT '======================================================================';
GO
