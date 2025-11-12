using System;
using System.Linq;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services
{
    /// <summary>
    /// 🔄 خدمة ترحيل البيانات التاريخية
    /// تستخدم لترحيل بيانات الأسابيع السابقة إلى الخزنة
    /// </summary>
    public class HistoricalDataMigrationService
    {
        private readonly ArrearService _arrearService;
        private readonly ReconciliationService _reconciliationService;
        private readonly ReconciliationRepository _reconciliationRepository;
        private readonly VaultRepository _vaultRepository;
        private readonly CollectionRepository _collectionRepository;
        private readonly AuditRepository _auditRepository;

        public HistoricalDataMigrationService()
        {
            _arrearService = new ArrearService();
            _reconciliationService = new ReconciliationService();
            _reconciliationRepository = new ReconciliationRepository();
            _vaultRepository = new VaultRepository();
            _collectionRepository = new CollectionRepository();
            _auditRepository = new AuditRepository();
        }

        /// <summary>
        /// ترحيل البيانات التاريخية الكاملة (المتأخرات + الجرد + الخزنة)
        /// </summary>
        /// <param name="startWeek">الأسبوع الأول (افتراضي: 1)</param>
        /// <param name="endWeek">الأسبوع الأخير (افتراضي: الأسبوع الحالي - 1)</param>
        /// <param name="userId">معرف المستخدم الذي يقوم بالترحيل</param>
        /// <param name="progressCallback">دالة لتحديث التقدم (النسبة المئوية، الرسالة)</param>
        /// <returns></returns>
        public (bool Success, string Message, MigrationResult Result) MigrateHistoricalData(
            int startWeek = 1,
            int? endWeek = null,
            int userId = 1,
            Action<int, string> progressCallback = null)
        {
            try
            {
                var result = new MigrationResult();
                
                // تحديد الأسبوع النهائي
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                int lastWeek = endWeek ?? (currentWeek > 1 ? currentWeek - 1 : 1);
                
                // التحقق من الصحة
                if (startWeek < 1 || startWeek > WeekHelper.TotalWeeks)
                    return (false, $"رقم الأسبوع الأول يجب أن يكون بين 1 و {WeekHelper.TotalWeeks}", result);
                
                if (lastWeek < startWeek || lastWeek > WeekHelper.TotalWeeks)
                    return (false, $"رقم الأسبوع الأخير يجب أن يكون بين {startWeek} و {WeekHelper.TotalWeeks}", result);

                progressCallback?.Invoke(0, "بدء عملية الترحيل...");

                // ═══════════════════════════════════════════════════════════
                // المرحلة 1️⃣: إنشاء المتأخرات للأسابيع الماضية
                // ═══════════════════════════════════════════════════════════
                progressCallback?.Invoke(10, "المرحلة 1: إنشاء المتأخرات للأيام الماضية...");
                
                var arrearsResult = _arrearService.ProcessHistoricalData((progress, message) =>
                {
                    // تحويل التقدم من 0-100 إلى 10-40
                    int adjustedProgress = 10 + (progress * 30 / 100);
                    progressCallback?.Invoke(adjustedProgress, $"المرحلة 1: {message}");
                });

                if (!arrearsResult.Success)
                    return (false, $"فشل في إنشاء المتأخرات: {arrearsResult.Message}", result);

                result.ArrearsCreated = arrearsResult.ArrearsCreated;
                result.PreviousArrearsCreated = arrearsResult.PreviousCreated;

                // ═══════════════════════════════════════════════════════════
                // المرحلة 2️⃣: جرد كل أسبوع وترحيله للخزنة
                // ═══════════════════════════════════════════════════════════
                progressCallback?.Invoke(40, "المرحلة 2: بدء جرد الأسابيع...");

                int totalWeeks = lastWeek - startWeek + 1;
                int processedWeeks = 0;

                for (int week = startWeek; week <= lastWeek; week++)
                {
                    // تحديث التقدم (40-90)
                    int progress = 40 + ((processedWeeks * 50) / totalWeeks);
                    progressCallback?.Invoke(progress, $"المرحلة 2: جرد الأسبوع {week} من {lastWeek}...");

                    // التحقق من وجود جرد مسبق
                    var (weekStart, weekEnd) = WeekHelper.GetWeekDateRange(week);
                    var existingReconciliation = _reconciliationRepository.GetByWeek(weekStart);

                    if (existingReconciliation != null)
                    {
                        result.WeeksSkipped++;
                        progressCallback?.Invoke(progress, $"⏭️ تخطي الأسبوع {week} (مُجرد مسبقاً)");
                        processedWeeks++;
                        continue;
                    }

                    // حساب المبلغ المتوقع
                    decimal expectedAmount = _reconciliationService.CalculateExpectedAmount(week);

                    // استخدام المبلغ المتوقع كمبلغ فعلي (أو يمكن تعديله حسب السجلات)
                    decimal actualAmount = expectedAmount;

                    // إتمام الجرد
                    var reconciliationResult = _reconciliationService.SubmitReconciliation(
                        week,
                        actualAmount,
                        $"جرد تاريخي تلقائي - الأسبوع {week}",
                        userId);

                    if (reconciliationResult.Success)
                    {
                        result.WeeksReconciled++;
                        result.TotalAmountTransferred += actualAmount;
                    }
                    else
                    {
                        result.Errors.Add($"الأسبوع {week}: {reconciliationResult.Message}");
                    }

                    processedWeeks++;
                }

                // ═══════════════════════════════════════════════════════════
                // المرحلة 3️⃣: التحقق النهائي
                // ═══════════════════════════════════════════════════════════
                progressCallback?.Invoke(90, "المرحلة 3: التحقق من النتائج...");

                // حساب رصيد الخزنة النهائي
                result.FinalVaultBalance = _vaultRepository.GetCurrentBalance();

                // حساب إجمالي السابقات المتراكمة
                var accumulatedRepo = new AccumulatedArrearsRepository();
                var allAccumulated = accumulatedRepo.GetAll();
                result.TotalAccumulatedArrears = allAccumulated.Sum(a => a.RemainingAmount);

                progressCallback?.Invoke(100, "✅ اكتملت عملية الترحيل بنجاح!");

                // ═══════════════════════════════════════════════════════════
                // تسجيل في Audit Log
                // ═══════════════════════════════════════════════════════════
                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Create,
                    EntityType = EntityType.WeeklyReconciliation,
                    EntityID = 0,
                    Details = $"ترحيل بيانات تاريخية: الأسابيع {startWeek}-{lastWeek}\n" +
                              $"• أسابيع مُجردة: {result.WeeksReconciled}\n" +
                              $"• مبلغ مُرحل: {result.TotalAmountTransferred:N2} ريال\n" +
                              $"• متأخرات مُنشأة: {result.ArrearsCreated}\n" +
                              $"• سابقات مُنشأة: {result.PreviousArrearsCreated}"
                });

                string successMessage = BuildSuccessMessage(result, startWeek, lastWeek);
                return (true, successMessage, result);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في الترحيل: {ex.Message}", new MigrationResult());
            }
        }

        /// <summary>
        /// التحقق من إمكانية الترحيل (فحص أولي)
        /// </summary>
        public (bool CanMigrate, string Message) CheckMigrationStatus()
        {
            try
            {
                // التحقق من وجود تحصيلات
                var collections = _collectionRepository.GetAll();
                if (!collections.Any())
                    return (false, "❌ لا توجد تحصيلات في قاعدة البيانات.\nيرجى إدخال التحصيلات أولاً.");

                // التحقق من وجود جرد مسبق
                var reconciliations = _reconciliationRepository.GetAll();
                int reconciledWeeks = reconciliations.Count();

                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                int maxWeeksToReconcile = currentWeek > 1 ? currentWeek - 1 : 0;

                if (reconciledWeeks >= maxWeeksToReconcile)
                    return (false, $"✅ جميع الأسابيع مُجردة مسبقاً ({reconciledWeeks} أسبوع).\nلا حاجة للترحيل.");

                // حساب الأسابيع المتبقية
                int remainingWeeks = maxWeeksToReconcile - reconciledWeeks;

                return (true, $"✅ يمكن ترحيل {remainingWeeks} أسبوع.\n" +
                             $"الأسابيع المُجردة: {reconciledWeeks}\n" +
                             $"الأسابيع المتبقية: {remainingWeeks}");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في الفحص: {ex.Message}");
            }
        }

        /// <summary>
        /// بناء رسالة النجاح
        /// </summary>
        private string BuildSuccessMessage(MigrationResult result, int startWeek, int lastWeek)
        {
            string message = $"✅ تم ترحيل البيانات التاريخية بنجاح!\n\n";
            message += $"📊 ملخص العملية:\n";
            message += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            message += $"🗓️  الأسابيع: من {startWeek} إلى {lastWeek}\n";
            message += $"✅ أسابيع مُجردة: {result.WeeksReconciled}\n";
            message += $"⏭️  أسابيع مُتخطاة: {result.WeeksSkipped}\n";
            message += $"💰 مبلغ مُرحل للخزنة: {result.TotalAmountTransferred:N2} ريال\n";
            message += $"⚠️  متأخرات مُنشأة: {result.ArrearsCreated}\n";
            message += $"📋 سابقات مُنشأة: {result.PreviousArrearsCreated}\n";
            message += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            message += $"🏦 رصيد الخزنة النهائي: {result.FinalVaultBalance:N2} ريال\n";
            message += $"📊 إجمالي السابقات المتراكمة: {result.TotalAccumulatedArrears:N2} ريال\n";

            if (result.Errors.Any())
            {
                message += $"\n⚠️ تحذيرات ({result.Errors.Count}):\n";
                foreach (var error in result.Errors.Take(5))
                {
                    message += $"  • {error}\n";
                }
            }

            return message;
        }
    }

    /// <summary>
    /// نتيجة عملية الترحيل
    /// </summary>
    public class MigrationResult
    {
        public int WeeksReconciled { get; set; }
        public int WeeksSkipped { get; set; }
        public decimal TotalAmountTransferred { get; set; }
        public int ArrearsCreated { get; set; }
        public int PreviousArrearsCreated { get; set; }
        public decimal FinalVaultBalance { get; set; }
        public decimal TotalAccumulatedArrears { get; set; }
        public System.Collections.Generic.List<string> Errors { get; set; } = new System.Collections.Generic.List<string>();
    }
}
