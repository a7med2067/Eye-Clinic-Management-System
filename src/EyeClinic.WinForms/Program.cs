using System;
using System.Windows.Forms;
using EyeClinic.Data.Configuration;
using EyeClinic.Data.Context;
using EyeClinic.WinForms.Forms;

namespace EyeClinic.WinForms
{
    static class Program
    {
        public const string ApplicationVersion = "1.0.0";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Verify Database Connection
            bool connectionOk = false;
            while (!connectionOk)
            {
                string connStr = ConnectionConfig.GetConnectionString();
                var test = ConnectionConfig.TestConnection(connStr);
                if (test.success)
                {
                    connectionOk = true;
                }
                else
                {
                    var msg = $"تعذر الاتصال بقاعدة بيانات العيادة:\n{test.message}\n\nهل ترغب في فتح شاشة إعداد الاتصال؟";
                    var dr = MessageBox.Show(msg, "خطأ في الاتصال بقاعدة البيانات", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (dr == DialogResult.Yes)
                    {
                        using (var setup = new ConnectionSetupForm())
                        {
                            if (setup.ShowDialog() != DialogResult.OK)
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // 2. Initialize and Seed Database if needed
            try
            {
                using (var db = new EyeClinicDbContext())
                {
                    DbInitializer.Initialize(db);
                }
            }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                string details = (baseEx != null && !string.IsNullOrEmpty(baseEx.Message)) ? baseEx.Message : ex.Message;
                MessageBox.Show($"حدث خطأ أثناء تهيئة قاعدة البيانات:\n{details}", "خطأ تهيئة", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2.5 Verify Application and Database Version Compatibility (INS-4)
            try
            {
                using (var db = new EyeClinicDbContext())
                {
                    var verSetting = System.Linq.Queryable.FirstOrDefault(db.ClinicSettings, s => s.SettingKey == "Database_Version");
                    string dbVersion = verSetting != null ? verSetting.SettingValue : null;
                    if (!string.IsNullOrEmpty(dbVersion) && dbVersion != ApplicationVersion)
                    {
                        string msg = string.Format(
                            "عدم تطابق في الإصدار (INS-4):\n" +
                            "إصدار التطبيق الحالي: {0}\n" +
                            "إصدار قاعدة البيانات: {1}\n\n" +
                            "تم إيقاف التشغيل لحماية سلامة البيانات. يرجى تحديث التطبيق أو ترقية قاعدة البيانات.",
                            ApplicationVersion, dbVersion);
                        MessageBox.Show(msg, "خطأ عدم تطابق الإصدار", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر التحقق من توافق إصدار قاعدة البيانات:\n" + ex.Message, "خطأ فحص الإصدار", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 3. Application Authentication Loop
            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() == DialogResult.OK && login.AuthenticatedUser != null)
                    {
                        using (var main = new MainForm())
                        {
                            Application.Run(main);
                        }
                    }
                    else
                    {
                        // User closed login form or cancelled
                        break;
                    }
                }
            }
        }
    }
}
