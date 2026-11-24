using System;
using System.Data.Entity;
using System.Linq;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Security;

namespace EyeClinic.Data.Context
{
    public static class DbInitializer
    {
        public static void Initialize(EyeClinicDbContext context)
        {
            // Ensure database is created
            if (!context.Database.Exists())
            {
                context.Database.Create();
            }
            else
            {
                // Verify if essential tables exist (in case database was created empty manually or prior failed step)
                bool tablesExist = false;
                try
                {
                    tablesExist = context.Database.SqlQuery<int?>(
                        "SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClinicSetting'").Any();
                }
                catch
                {
                    tablesExist = false;
                }

                if (!tablesExist)
                {
                    var adapter = (System.Data.Entity.Infrastructure.IObjectContextAdapter)context;
                    string script = adapter.ObjectContext.CreateDatabaseScript();
                    context.Database.ExecuteSqlCommand(script);
                }
            }

            // Seed initial data
            SeedSettings(context);
            SeedUsers(context);
            context.SaveChanges();
        }

        private static void SeedSettings(EyeClinicDbContext context)
        {
            var defaultSettings = new[]
            {
                new ClinicSetting { SettingKey = "Clinic_Name", SettingValue = "عيادة العيون التخصصية", Description = "اسم العيادة المطبوع على الإيصالات والواجهة", GroupName = "General" },
                new ClinicSetting { SettingKey = "Clinic_DoctorName", SettingValue = "د. استشاري طب وجراحة العيون", Description = "اسم الطبيب", GroupName = "General" },
                new ClinicSetting { SettingKey = "Clinic_Phone", SettingValue = "01000000000", Description = "هاتف العيادة", GroupName = "General" },
                new ClinicSetting { SettingKey = "Clinic_Address", SettingValue = "جمهورية مصر العربية", Description = "عنوان العيادة", GroupName = "General" },
                new ClinicSetting { SettingKey = "Price_Exam", SettingValue = "300", Description = "سعر الكشف الافتراضي (جنيه)", GroupName = "Pricing" },
                new ClinicSetting { SettingKey = "Price_ReExam", SettingValue = "50", Description = "سعر الإعادة الافتراضي (جنيه)", GroupName = "Pricing" },
                new ClinicSetting { SettingKey = "Price_Urgent", SettingValue = "450", Description = "سعر الكشف المستعجل الافتراضي (جنيه)", GroupName = "Pricing" },
                new ClinicSetting { SettingKey = "ReExam_ValidDays", SettingValue = "14", Description = "فترة صلاحية الإعادة بالأيام من آخر كشف مدفوع", GroupName = "Rules" },
                new ClinicSetting { SettingKey = "Max_DailyPatients", SettingValue = "0", Description = "الحد الأقصى لمرضى اليوم (0 يعني لا يوجد حد)", GroupName = "Rules" },
                new ClinicSetting { SettingKey = "InactivityTimeout_DoctorMinutes", SettingValue = "10", Description = "مهلة الخمول بالدقائق لحساب الطبيب", GroupName = "Security" },
                new ClinicSetting { SettingKey = "InactivityTimeout_SecretaryMinutes", SettingValue = "30", Description = "مهلة الخمول بالدقائق لحساب السكرتارية", GroupName = "Security" },
                new ClinicSetting { SettingKey = "Print_ReceiptEnabled", SettingValue = "True", Description = "تفعيل طباعة الإيصال الحراري", GroupName = "Printing" },
                new ClinicSetting { SettingKey = "Print_Rx_Offset_X_MM", SettingValue = "0", Description = "إزاحة أفقية لمعايرة الروشتة (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Offset_Y_MM", SettingValue = "0", Description = "إزاحة رأسية لمعايرة الروشتة (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Name_X_MM", SettingValue = "35", Description = "إحداثي X لاسم المريض (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Name_Y_MM", SettingValue = "38", Description = "إحداثي Y لاسم المريض (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Age_X_MM", SettingValue = "110", Description = "إحداثي X للسن (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Age_Y_MM", SettingValue = "38", Description = "إحداثي Y للسن (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Diagnosis_X_MM", SettingValue = "42", Description = "إحداثي X للتشخيص (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Diagnosis_Y_MM", SettingValue = "46", Description = "إحداثي Y للتشخيص (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Date_X_MM", SettingValue = "112", Description = "إحداثي X للتاريخ (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Date_Y_MM", SettingValue = "46", Description = "إحداثي Y للتاريخ (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Box_X_MM", SettingValue = "17", Description = "إحداثي X لصندوق الأدوية (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Box_Y_MM", SettingValue = "69", Description = "إحداثي Y لصندوق الأدوية (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Box_Width_MM", SettingValue = "130", Description = "عرض صندوق الأدوية (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Print_Rx_Box_Height_MM", SettingValue = "130", Description = "ارتفاع صندوق الأدوية (مليمتر)", GroupName = "Calibration" },
                new ClinicSetting { SettingKey = "Database_Version", SettingValue = "1.0.0", Description = "إصدار قاعدة البيانات لمطابقة التطبيق", GroupName = "System" }
            };

            foreach (var setting in defaultSettings)
            {
                if (!context.ClinicSettings.Any(s => s.SettingKey == setting.SettingKey))
                {
                    context.ClinicSettings.Add(setting);
                }
            }
        }

        private static void SeedUsers(EyeClinicDbContext context)
        {
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                var adminAuth = PasswordHasher.HashPassword("admin123");
                context.Users.Add(new User
                {
                    Username = "admin",
                    FullName = "دكتور العيادة (المدير)",
                    Role = UserRole.Doctor,
                    PasswordHash = adminAuth.hash,
                    PasswordSalt = adminAuth.salt,
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = DateTime.Now
                });
            }

            if (!context.Users.Any(u => u.Username == "secretary"))
            {
                var secAuth = PasswordHasher.HashPassword("sec123");
                context.Users.Add(new User
                {
                    Username = "secretary",
                    FullName = "موظفة الاستقبال",
                    Role = UserRole.Secretary,
                    PasswordHash = secAuth.hash,
                    PasswordSalt = secAuth.salt,
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = DateTime.Now
                });
            }
        }
    }
}
