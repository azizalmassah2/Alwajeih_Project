using System;
using System.Text.RegularExpressions;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مساعد التحقق من المدخلات
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// التحقق من صحة رقم الهاتف اليمني
        /// </summary>
        public static bool IsValidYemeniPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; // اختياري

            // نمط رقم الهاتف اليمني
            var pattern = @"^((\+?967)|0)?(7[0-9]{8})$";
            return Regex.IsMatch(phone, pattern);
        }

        /// <summary>
        /// التحقق من صحة المبلغ
        /// </summary>
        public static bool IsValidAmount(decimal amount)
        {
            return amount > 0 && amount <= 1000000; // حد أقصى مليون ريال
        }

        /// <summary>
        /// التحقق من صحة فترة زمنية
        /// </summary>
        public static bool IsValidDateRange(DateTime startDate, DateTime endDate)
        {
            return endDate >= startDate;
        }

        /// <summary>
        /// التحقق من صحة اسم المستخدم
        /// </summary>
        public static bool IsValidUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return username.Length >= 3 && username.Length <= 50;
        }

        /// <summary>
        /// التحقق من صحة كلمة المرور
        /// </summary>
        public static bool IsValidPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return password.Length >= 6;
        }

        /// <summary>
        /// التحقق من صحة النص (ليس فارغاً)
        /// </summary>
        public static bool IsNotEmpty(string? text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }
    }
}
