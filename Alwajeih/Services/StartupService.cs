using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة تشغيل التطبيق مع بدء Windows
    /// </summary>
    public static class StartupService
    {
        private const string APP_NAME = "Alwajeih";
        private static readonly string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// تفعيل بدء التشغيل التلقائي
        /// </summary>
        public static bool EnableStartup()
        {
            try
            {
                string appPath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue(APP_NAME, $"\"{appPath}\" -minimized");
                        System.Diagnostics.Debug.WriteLine("✅ تم تفعيل بدء التشغيل التلقائي");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تفعيل بدء التشغيل: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// إلغاء بدء التشغيل التلقائي
        /// </summary>
        public static bool DisableStartup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(APP_NAME, false);
                        System.Diagnostics.Debug.WriteLine("✅ تم إلغاء بدء التشغيل التلقائي");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إلغاء بدء التشغيل: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// التحقق من تفعيل بدء التشغيل التلقائي
        /// </summary>
        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, false))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(APP_NAME);
                        return value != null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق من بدء التشغيل: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// تبديل حالة بدء التشغيل
        /// </summary>
        public static bool ToggleStartup()
        {
            if (IsStartupEnabled())
            {
                return DisableStartup();
            }
            else
            {
                return EnableStartup();
            }
        }
    }
}
