namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مساعد تشفير كلمات المرور
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// تشفير كلمة المرور
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// التحقق من كلمة المرور
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
