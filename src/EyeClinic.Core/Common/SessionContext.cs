using System;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;

namespace EyeClinic.Core.Common
{
    public static class SessionContext
    {
        public static User CurrentUser { get; private set; }
        public static DateTime? SessionStartTime { get; private set; }
        public static DateTime LastActivityTime { get; set; } = DateTime.Now;

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsDoctor => CurrentUser != null && CurrentUser.Role == UserRole.Doctor;
        public static bool IsSecretary => CurrentUser != null && CurrentUser.Role == UserRole.Secretary;

        public static int CurrentUserId => CurrentUser?.Id ?? 0;
        public static string CurrentUsername => CurrentUser?.Username ?? "System";

        public static void SetSession(User user)
        {
            CurrentUser = user;
            SessionStartTime = DateTime.Now;
            LastActivityTime = DateTime.Now;
        }

        public static void ClearSession()
        {
            CurrentUser = null;
            SessionStartTime = null;
        }
    }
}
