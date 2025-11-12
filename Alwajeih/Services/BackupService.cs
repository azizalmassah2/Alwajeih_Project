using System;
using System.IO;
using System.Linq;
using Alwajeih.Data;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    /// <summary>
    /// 💾 خدمة النسخ الاحتياطي والاسترجاع
    /// توفر إمكانية إنشاء نسخ احتياطية من قاعدة البيانات واسترجاعها
    /// مع تنظيف تلقائي للنسخ القديمة
    /// </summary>
    public class BackupService
    {
        private readonly AuditRepository _auditRepository;
        private static readonly string BackupFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

        /// <summary>
        /// المُنشئ - يقوم بتهيئة الخدمة وإنشاء مجلد النسخ الاحتياطي
        /// </summary>
        public BackupService()
        {
            _auditRepository = new AuditRepository();
            
            // إنشاء مجلد النسخ الاحتياطي إن لم يكن موجوداً
            if (!Directory.Exists(BackupFolder))
            {
                Directory.CreateDirectory(BackupFolder);
            }
        }

        /// <summary>
        /// إنشاء نسخة احتياطية من قاعدة البيانات
        /// </summary>
        /// <param name="userId">معرّف المستخدم الذي يقوم بالعملية</param>
        /// <returns>نتيجة العملية مع مسار النسخة الاحتياطية</returns>
        public (bool Success, string Message, string? BackupPath) CreateBackup(int userId)
        {
            try
            {
                var dbPath = DatabaseContext.GetDatabasePath();
                
                if (!File.Exists(dbPath))
                    return (false, "قاعدة البيانات غير موجودة", null);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"Alwajeih_Backup_{timestamp}.db";
                var backupPath = System.IO.Path.Combine(BackupFolder, backupFileName);

                // نسخ قاعدة البيانات
                File.Copy(dbPath, backupPath, true);

                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Backup,
                    EntityType = EntityType.User,
                    Details = $"إنشاء نسخة احتياطية: {backupFileName}"
                });

                return (true, "تم إنشاء النسخة الاحتياطية بنجاح", backupPath);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في إنشاء النسخة الاحتياطية: {ex.Message}", null);
            }
        }

        /// <summary>
        /// استرجاع نسخة احتياطية من قاعدة البيانات
        /// ⚠️ تحذير: سيتم استبدال جميع البيانات الحالية
        /// </summary>
        /// <param name="backupPath">مسار ملف النسخة الاحتياطية</param>
        /// <param name="userId">معرّف المستخدم الذي يقوم بالعملية</param>
        /// <returns>نتيجة العملية</returns>
        public (bool Success, string Message) RestoreBackup(string backupPath, int userId)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return (false, "ملف النسخة الاحتياطية غير موجود");

                var dbPath = DatabaseContext.GetDatabasePath();

                // نسخ احتياطي للقاعدة الحالية قبل الاسترجاع
                var tempBackup = dbPath + ".temp_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, tempBackup, true);
                }

                try
                {
                    // استرجاع النسخة الاحتياطية
                    File.Copy(backupPath, dbPath, true);

                    _auditRepository.Add(new AuditLog
                    {
                        UserID = userId,
                        Action = AuditAction.Restore,
                        EntityType = EntityType.User,
                        Details = $"استرجاع نسخة احتياطية: {System.IO.Path.GetFileName(backupPath)}"
                    });

                    // حذف النسخة المؤقتة
                    if (File.Exists(tempBackup))
                        File.Delete(tempBackup);

                    return (true, "تم استرجاع النسخة الاحتياطية بنجاح");
                }
                catch
                {
                    // في حالة الفشل، استرجاع النسخة المؤقتة
                    if (File.Exists(tempBackup))
                    {
                        File.Copy(tempBackup, dbPath, true);
                        File.Delete(tempBackup);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في استرجاع النسخة الاحتياطية: {ex.Message}");
            }
        }

        /// <summary>
        /// الحصول على قائمة النسخ الاحتياطية المتوفرة
        /// </summary>
        /// <returns>مصفوفة بمسارات النسخ الاحتياطية مرتبة حسب التاريخ (الأحدث أولاً)</returns>
        public string[] GetAvailableBackups()
        {
            if (!Directory.Exists(BackupFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(BackupFolder, "*.db")
                            .OrderByDescending(f => File.GetCreationTime(f))
                            .ToArray();
        }

        /// <summary>
        /// تنظيف النسخ الاحتياطية القديمة
        /// </summary>
        /// <param name="retentionDays">عدد الأيام للاحتفاظ بالنسخ (افتراضياً 30 يوم)</param>
        public void CleanOldBackups(int retentionDays = 30)
        {
            if (!Directory.Exists(BackupFolder))
                return;

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var oldBackups = Directory.GetFiles(BackupFolder, "*.db")
                                     .Where(f => File.GetCreationTime(f) < cutoffDate);

            foreach (var oldBackup in oldBackups)
            {
                try
                {
                    File.Delete(oldBackup);
                }
                catch
                {
                    // تجاهل الأخطاء في حذف الملفات القديمة
                }
            }
        }
    }
}
