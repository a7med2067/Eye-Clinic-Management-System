using System;
using System.Linq;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Data.Context;
using EyeClinic.Data.Security;

namespace EyeClinic.Business.Services
{
    public class AuthService
    {
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public Result<User> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<User>.Failure("يرجى إدخال اسم المستخدم وكلمة المرور.");

            using (var db = new EyeClinicDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == username.Trim());
                if (user == null || !user.IsActive)
                    return Result<User>.Failure("اسم المستخدم أو كلمة المرور غير صحيحة.");

                // Check lockout
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
                {
                    var remaining = Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
                    return Result<User>.Failure($"الحساب مقفل مؤقتاً لكثرة المحاولات الخاطئة. يرجى الانتظار {remaining} دقيقة.");
                }

                // Verify password
                bool isValid = PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
                if (!isValid)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= MaxFailedAttempts)
                    {
                        user.LockoutEnd = DateTime.Now.AddMinutes(LockoutMinutes);
                        db.SaveChanges();
                        return Result<User>.Failure($"تم قفل الحساب لمدة {LockoutMinutes} دقيقة بعد {MaxFailedAttempts} محاولات خاطئة متتالية.");
                    }
                    db.SaveChanges();
                    int attemptsLeft = MaxFailedAttempts - user.FailedLoginAttempts;
                    return Result<User>.Failure($"اسم المستخدم أو كلمة المرور غير صحيحة. متبقي {attemptsLeft} محاولات قبل قفل الحساب.");
                }

                // Successful login
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.LastLoginAt = DateTime.Now;

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Username = user.Username,
                    ActionType = AuditActionType.Login,
                    EntityName = "User",
                    RecordId = user.Id.ToString(),
                    Description = $"تسجيل دخول ناجح للمستخدم: {user.FullName}",
                    Timestamp = DateTime.Now
                });

                db.SaveChanges();

                SessionContext.SetSession(user);
                return Result<User>.Success(user);
            }
        }

        public void Logout()
        {
            if (SessionContext.IsLoggedIn)
            {
                using (var db = new EyeClinicDbContext())
                {
                    db.AuditLogs.Add(new AuditLog
                    {
                        UserId = SessionContext.CurrentUserId,
                        Username = SessionContext.CurrentUsername,
                        ActionType = AuditActionType.Logout,
                        EntityName = "User",
                        RecordId = SessionContext.CurrentUserId.ToString(),
                        Description = $"تسجيل خروج للمستخدم: {SessionContext.CurrentUser.FullName}",
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            SessionContext.ClearSession();
        }

        public Result ChangePassword(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return Result.Failure("يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.");

            using (var db = new EyeClinicDbContext())
            {
                var user = db.Users.Find(userId);
                if (user == null)
                    return Result.Failure("المستخدم غير موجود.");

                if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                    return Result.Failure("كلمة المرور الحالية غير صحيحة.");

                var auth = PasswordHasher.HashPassword(newPassword);
                user.PasswordHash = auth.hash;
                user.PasswordSalt = auth.salt;
                user.MustChangePassword = false;

                db.SaveChanges();
                return Result.Success();
            }
        }

        public Result ResetPassword(int targetUserId, string newPassword)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("هذه العملية مقتصرة على حساب الطبيب/المدير فقط.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return Result.Failure("يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.");

            using (var db = new EyeClinicDbContext())
            {
                var user = db.Users.Find(targetUserId);
                if (user == null)
                    return Result.Failure("المستخدم غير موجود.");

                var auth = PasswordHasher.HashPassword(newPassword);
                user.PasswordHash = auth.hash;
                user.PasswordSalt = auth.salt;
                user.MustChangePassword = true;
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;

                db.SaveChanges();
                return Result.Success();
            }
        }

        public Result UpdateUserAccount(int targetUserId, string newUsername, string newFullName, string newPassword = null)
        {
            if (!SessionContext.IsDoctor)
                return Result.Failure("هذه العملية مقتصرة على حساب الطبيب/المدير فقط.");

            if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Trim().Length < 3)
                return Result.Failure("يجب ألا يقل اسم المستخدم عن 3 أحرف أو أرقام.");

            if (string.IsNullOrWhiteSpace(newFullName))
                return Result.Failure("يرجى إدخال الاسم المعروض.");

            if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length < 6)
                return Result.Failure("يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.");

            string cleanUsername = newUsername.Trim();
            string cleanFullName = newFullName.Trim();

            using (var db = new EyeClinicDbContext())
            {
                var user = db.Users.Find(targetUserId);
                if (user == null)
                    return Result.Failure("المستخدم غير موجود.");

                // Check username uniqueness if changed
                bool exists = db.Users.Any(u => u.Id != targetUserId && u.Username == cleanUsername);
                if (exists)
                    return Result.Failure($"اسم المستخدم '{cleanUsername}' محجوز بالفعل لمستخدم آخر. يرجى اختيار اسم مستخدم مختلف.");

                user.Username = cleanUsername;
                user.FullName = cleanFullName;

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (newPassword.Length < 6)
                        return Result.Failure("يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.");

                    var auth = PasswordHasher.HashPassword(newPassword);
                    user.PasswordHash = auth.hash;
                    user.PasswordSalt = auth.salt;
                    user.MustChangePassword = false;
                    user.FailedLoginAttempts = 0;
                    user.LockoutEnd = null;
                }

                db.SaveChanges();

                if (SessionContext.CurrentUserId == targetUserId && SessionContext.CurrentUser != null)
                {
                    SessionContext.CurrentUser.Username = user.Username;
                    SessionContext.CurrentUser.FullName = user.FullName;
                }

                return Result.Success();
            }
        }

        #region Master Recovery Key (Emergency Offline Password Reset)

        public static string GenerateRecoveryKey()
        {
            // 24 characters from an unambiguous alphabet (excluding 0, 1, O, I to avoid handwriting confusion)
            const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            var sb = new System.Text.StringBuilder();
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                byte[] buffer = new byte[24];
                rng.GetBytes(buffer);
                for (int i = 0; i < 24; i++)
                {
                    if (i > 0 && i % 4 == 0)
                        sb.Append('-');
                    sb.Append(chars[buffer[i] % chars.Length]);
                }
            }
            return sb.ToString(); // Format: XXXX-XXXX-XXXX-XXXX-XXXX-XXXX
        }

        public static string NormalizeRecoveryKey(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey)) return string.Empty;
            return rawKey.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
        }

        public bool NeedsRecoveryKeyPrompt()
        {
            using (var db = new EyeClinicDbContext())
            {
                var s = db.ClinicSettings.Find("Doctor_RecoveryKey_HasSeen");
                return s == null || s.SettingValue != "True";
            }
        }

        public Result SaveMasterRecoveryKey(string rawKey)
        {
            string normalized = NormalizeRecoveryKey(rawKey);
            if (string.IsNullOrEmpty(normalized) || normalized.Length < 16)
                return Result.Failure("مفتاح الاسترداد غير صالح.");

            var auth = PasswordHasher.HashPassword(normalized);

            using (var db = new EyeClinicDbContext())
            {
                SetSetting(db, "Doctor_RecoveryKey_Hash", auth.hash, "Security", "هاش مشفر لمفتاح الاسترداد الرئيسي للطوارئ");
                SetSetting(db, "Doctor_RecoveryKey_Salt", auth.salt, "Security", "ملح مفتاح الاسترداد");
                SetSetting(db, "Doctor_RecoveryKey_HasSeen", "True", "Security", "حالة تأكيد الطبيب لحفظ مفتاح الاسترداد");
                SetSetting(db, "Doctor_RecoveryKey_CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Security", "تاريخ توليد مفتاح الاسترداد");

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = SessionContext.CurrentUserId > 0 ? (int?)SessionContext.CurrentUserId : null,
                    Username = SessionContext.CurrentUsername ?? "Doctor",
                    ActionType = AuditActionType.UserManagement,
                    EntityName = "Security",
                    RecordId = "RecoveryKey",
                    Description = "توليد مفتاح الاسترداد الرئيسي وتأكيد حفظه خارج الجهاز بأمان",
                    Timestamp = DateTime.Now
                });

                db.SaveChanges();
                return Result.Success();
            }
        }

        public Result RecoverDoctorPassword(string enteredRecoveryKey, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(enteredRecoveryKey))
                return Result.Failure("يرجى إدخال مفتاح الاسترداد الرئيسي.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return Result.Failure("يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.");

            string normalized = NormalizeRecoveryKey(enteredRecoveryKey);

            using (var db = new EyeClinicDbContext())
            {
                var hashSetting = db.ClinicSettings.Find("Doctor_RecoveryKey_Hash");
                var saltSetting = db.ClinicSettings.Find("Doctor_RecoveryKey_Salt");

                if (hashSetting == null || saltSetting == null || string.IsNullOrWhiteSpace(hashSetting.SettingValue))
                    return Result.Failure("لم يتم تهيئة مفتاح استرداد في النظام بعد. يرجى مراجعة الدعم الفني.");

                bool isValid = PasswordHasher.VerifyPassword(normalized, hashSetting.SettingValue, saltSetting.SettingValue);
                if (!isValid)
                {
                    db.AuditLogs.Add(new AuditLog
                    {
                        ActionType = AuditActionType.Login,
                        EntityName = "Security",
                        RecordId = "RecoveryKey",
                        Description = "محاولة فاشلة لاسترداد حساب الطبيب باستخدام مفتاح طوارئ غير صحيح",
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                    return Result.Failure("مفتاح الاسترداد غير صحيح. تأكد من كتابة الرموز بدقة كما هي مدونة في وثيقة الطوارئ.");
                }

                var doctor = db.Users.FirstOrDefault(u => u.Role == UserRole.Doctor);
                if (doctor == null)
                    return Result.Failure("حساب الطبيب غير موجود في قاعدة البيانات.");

                var auth = PasswordHasher.HashPassword(newPassword);
                doctor.PasswordHash = auth.hash;
                doctor.PasswordSalt = auth.salt;
                doctor.FailedLoginAttempts = 0;
                doctor.LockoutEnd = null;
                doctor.MustChangePassword = false;

                // Mark that a new replacement key should be generated and shown
                var hasSeen = db.ClinicSettings.Find("Doctor_RecoveryKey_HasSeen");
                if (hasSeen != null) hasSeen.SettingValue = "False";

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = doctor.Id,
                    Username = doctor.Username,
                    ActionType = AuditActionType.UserManagement,
                    EntityName = "User",
                    RecordId = doctor.Id.ToString(),
                    Description = "استرداد طارئ ناجح لحساب الطبيب وتعيين كلمة مرور جديدة بواسطة مفتاح الاسترداد (Master Recovery Key)",
                    Timestamp = DateTime.Now
                });

                db.SaveChanges();
                return Result.Success();
            }
        }

        private static void SetSetting(EyeClinicDbContext db, string key, string value, string group, string desc)
        {
            var s = db.ClinicSettings.Find(key);
            if (s != null)
            {
                s.SettingValue = value;
            }
            else
            {
                db.ClinicSettings.Add(new ClinicSetting
                {
                    SettingKey = key,
                    SettingValue = value,
                    GroupName = group,
                    Description = desc
                });
            }
        }

        #endregion
    }
}
