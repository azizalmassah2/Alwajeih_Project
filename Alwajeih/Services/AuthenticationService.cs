using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة المصادقة والصلاحيات
    /// </summary>
    public class AuthenticationService
    {
        private static AuthenticationService? _instance;
        public static AuthenticationService Instance => _instance ??= new AuthenticationService();

        private readonly UserRepository _userRepository;
        private readonly AuditRepository _auditRepository;
        public User? CurrentUser { get; private set; }

        public AuthenticationService()
        {
            _userRepository = new UserRepository();
            _auditRepository = new AuditRepository();
        }

        /// <summary>
        /// تسجيل الدخول
        /// </summary>
        public bool Login(string username, string password)
        {
            var user = _userRepository.GetByUsername(username);

            if (user == null || !user.IsActive)
                return false;

            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
                return false;

            CurrentUser = user;
            _userRepository.UpdateLastLogin(user.UserID);

            // تسجيل في Audit Log
            _auditRepository.Add(
                new AuditLog
                {
                    UserID = user.UserID,
                    Action = AuditAction.Login,
                    EntityType = EntityType.User,
                    EntityID = user.UserID,
                    Details = $"تسجيل دخول المستخدم {username}",
                }
            );

            return true;
        }

        /// <summary>
        /// تسجيل الخروج
        /// </summary>
        public void Logout()
        {
            if (CurrentUser != null)
            {
                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = CurrentUser.UserID,
                        Action = AuditAction.Logout,
                        EntityType = EntityType.User,
                        EntityID = CurrentUser.UserID,
                        Details = $"تسجيل خروج المستخدم {CurrentUser.Username}",
                    }
                );
            }

            CurrentUser = null;
        }

        /// <summary>
        /// التحقق من الصلاحية
        /// </summary>
        public bool HasPermission(string permission)
        {
            if (CurrentUser == null)
                return false;

            // المدير لديه جميع الصلاحيات
            if (CurrentUser.Role == UserRole.Manager)
                return true;

            // أمين الصندوق لديه معظم الصلاحيات ما عدا إدارة المستخدمين والنسخ الاحتياطي
            if (CurrentUser.Role == UserRole.Cashier)
            {
                return permission != "ManageUsers" && permission != "RestoreBackup";
            }

            // المشاهد ليس لديه صلاحيات تعديل
            return permission == "ViewOnly";
        }

        /// <summary>
        /// هل المستخدم مدير؟
        /// </summary>
        public bool IsManager()
        {
            return CurrentUser != null && CurrentUser.Role == UserRole.Manager;
        }

        /// <summary>
        /// هل المستخدم أمين صندوق؟
        /// </summary>
        public bool IsCashier()
        {
            return CurrentUser != null && CurrentUser.Role == UserRole.Cashier;
        }
    }
}
