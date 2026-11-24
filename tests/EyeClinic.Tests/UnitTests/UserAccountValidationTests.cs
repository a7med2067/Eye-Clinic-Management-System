using System;
using EyeClinic.Business.Services;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class UserAccountValidationTests
    {
        private readonly AuthService _authService = new AuthService();

        public void Test_UpdateUserAccount_RequiresDoctorRole()
        {
            // Simulate Secretary login
            SessionContext.SetSession(new User
            {
                Id = 2,
                Username = "secretary",
                FullName = "موظفة الاستقبال",
                Role = UserRole.Secretary
            });

            var result = _authService.UpdateUserAccount(2, "newsec", "اسم جديد", "123456");
            Assert.IsFalse(result.IsSuccess, "Secretary must not be allowed to update user accounts.");
            Assert.IsTrue(result.ErrorMessage.Contains("الطبيب"), "Error message should mention Doctor restriction.");

            SessionContext.ClearSession();
        }

        public void Test_UpdateUserAccount_ValidatesUsernameAndFullName()
        {
            // Simulate Doctor login
            SessionContext.SetSession(new User
            {
                Id = 1,
                Username = "admin",
                FullName = "د. أحمد عادل",
                Role = UserRole.Doctor
            });

            // Empty username
            var res1 = _authService.UpdateUserAccount(2, "", "اسم جديد");
            Assert.IsFalse(res1.IsSuccess, "Should fail on empty username");

            // Short username (< 3 chars)
            var res2 = _authService.UpdateUserAccount(2, "ab", "اسم جديد");
            Assert.IsFalse(res2.IsSuccess, "Should fail on short username");

            // Empty full name
            var res3 = _authService.UpdateUserAccount(2, "secuser", "   ");
            Assert.IsFalse(res3.IsSuccess, "Should fail on whitespace full name");

            // Short password (< 6 chars)
            var res4 = _authService.UpdateUserAccount(2, "secuser", "اسم جديد", "123");
            Assert.IsFalse(res4.IsSuccess, "Should fail on short password < 6 characters");

            SessionContext.ClearSession();
        }

        public void Test_RoleArabicTranslation_FormattedCorrectly()
        {
            var doctor = new User { Id = 1, FullName = "د. استشاري", Role = UserRole.Doctor };
            SessionContext.SetSession(doctor);
            string roleArabicDoc = SessionContext.IsDoctor ? "طبيب" : "استقبال / سكرتارية";
            string headerTextDoc = string.Format("المستخدم: {0} ({1})", SessionContext.CurrentUser.FullName, roleArabicDoc);
            Assert.AreEqual("المستخدم: د. استشاري (طبيب)", headerTextDoc, "Doctor header text format mismatch");

            var secretary = new User { Id = 2, FullName = "سارة أحمد", Role = UserRole.Secretary };
            SessionContext.SetSession(secretary);
            string roleArabicSec = SessionContext.IsDoctor ? "طبيب" : "استقبال / سكرتارية";
            string headerTextSec = string.Format("المستخدم: {0} ({1})", SessionContext.CurrentUser.FullName, roleArabicSec);
            Assert.AreEqual("المستخدم: سارة أحمد (استقبال / سكرتارية)", headerTextSec, "Secretary header text format mismatch");

            SessionContext.ClearSession();
        }
    }
}
