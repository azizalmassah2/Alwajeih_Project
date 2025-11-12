using System;
using System.Data.SQLite;
using System.IO;

namespace Alwajeih.Data
{
    /// <summary>
    /// سياق قاعدة البيانات - إدارة الاتصال بقاعدة البيانات SQLite
    /// </summary>
    public class DatabaseContext
    {
        // المسارات الثابتة - استخدام مسار ثابت واحد
        private static readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string DatabaseFolder = System.IO.Path.Combine(_baseDirectory, "Data");
        public static readonly string DatabasePath = System.IO.Path.Combine(DatabaseFolder, "Alwajeih.db");

        /// <summary>
        /// الحصول على نص الاتصال بقاعدة البيانات
        /// </summary>
        public static string GetConnectionString()
        {
            // إنشاء مجلد Data إذا لم يكن موجودًا
            if (!Directory.Exists(DatabaseFolder))
            {
                Directory.CreateDirectory(DatabaseFolder);
            }

            // تسجيل المسار للتشخيص
            // System.Diagnostics.Debug.WriteLine($"Database Path: {DatabasePath}");

            return $"Data Source={DatabasePath};Version=3;";
        }

        /// <summary>
        /// إنشاء اتصال جديد بقاعدة البيانات
        /// </summary>
        public static SQLiteConnection CreateConnection()
        {
            return new SQLiteConnection(GetConnectionString());
        }

        /// <summary>
        /// التحقق من وجود قاعدة البيانات
        /// </summary>
        public static bool DatabaseExists()
        {
            return File.Exists(DatabasePath);
        }

        /// <summary>
        /// الحصول على مسار قاعدة البيانات
        /// </summary>
        public static string GetDatabasePath()
        {
            return DatabasePath;
        }

        /// <summary>
        /// الحصول على مسار مجلد قاعدة البيانات
        /// </summary>
        public static string GetDatabaseFolder()
        {
            return DatabaseFolder;
        }
    }
}
