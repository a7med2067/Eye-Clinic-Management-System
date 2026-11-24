using System.Collections.Generic;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class SettingsService
    {
        public Dictionary<string, string> GetAllSettings()
        {
            using (var db = new EyeClinicDbContext())
            {
                return db.ClinicSettings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
            }
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            using (var db = new EyeClinicDbContext())
            {
                var s = db.ClinicSettings.Find(key);
                return s != null ? s.SettingValue : defaultValue;
            }
        }

        public Result SaveSettings(Dictionary<string, string> settings)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("تعديل إعدادات العيادة مقتصر على الطبيب/المدير فقط.");

            using (var db = new EyeClinicDbContext())
            {
                foreach (var kvp in settings)
                {
                    var s = db.ClinicSettings.Find(kvp.Key);
                    if (s != null)
                    {
                        s.SettingValue = kvp.Value;
                    }
                    else
                    {
                        db.ClinicSettings.Add(new ClinicSetting
                        {
                            SettingKey = kvp.Key,
                            SettingValue = kvp.Value,
                            GroupName = "Custom"
                        });
                    }
                }

                db.SaveChanges();
                return Result.Success();
            }
        }
    }
}
