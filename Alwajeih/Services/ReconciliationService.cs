using System;
using System.Linq;
using Alwajeih.Utilities.Helpers;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    public class ReconciliationService
    {
        private readonly ReconciliationRepository _reconciliationRepository;
        private readonly CollectionRepository _collectionRepository;
        private readonly ExternalPaymentRepository _externalPaymentRepository;
        private readonly VaultRepository _vaultRepository;
        private readonly AuditRepository _auditRepository;
        private readonly ArrearService _arrearService;

        public ReconciliationService()
        {
            _reconciliationRepository = new ReconciliationRepository();
            _collectionRepository = new CollectionRepository();
            _externalPaymentRepository = new ExternalPaymentRepository();
            _vaultRepository = new VaultRepository();
            _auditRepository = new AuditRepository();
            _arrearService = new ArrearService();
        }

        /// <summary>
        /// حساب المبلغ المتوقع لأسبوع معين (بنظام 26 أسبوع)
        /// </summary>
        public decimal CalculateExpectedAmount(int weekNumber)
        {
            var (weekStart, weekEnd) = WeekHelper.GetWeekDateRange(weekNumber);
            return CalculateExpectedAmountByDate(weekStart, weekEnd);
        }

        /// <summary>
        /// حساب المبلغ المتوقع بالتواريخ
        /// المبلغ المتوقع = الرصيد السابق + التحصيل - المصروفات - السحوبات
        /// </summary>
        private decimal CalculateExpectedAmountByDate(DateTime weekStart, DateTime weekEnd)
        {
            var (weekNumber, _) = WeekHelper.GetWeekAndDayFromDate(weekStart);
            
            // 1️⃣ الرصيد السابق (من الجرد السابق)
            decimal previousBalance = 0;
            if (weekNumber > 1)
            {
                var (prevStart, prevEnd) = WeekHelper.GetWeekDateRange(weekNumber - 1);
                var previousReconciliations = _reconciliationRepository.GetByDateRange(prevStart, prevEnd);
                var lastRecon = previousReconciliations.OrderByDescending(r => r.ReconciliationDate).FirstOrDefault();
                previousBalance = lastRecon?.ActualAmount ?? 0;
            }
            
            // 2️⃣ التحصيلات (من DailyCollections)
            var dailyCollectionRepo = new DailyCollectionRepository();
            var collections = dailyCollectionRepo.GetCollectionsByWeek(weekNumber)
                .Where(c => !c.IsCancelled).ToList();
            
            // التحصيل اليومي
            decimal todayPayments = collections.Sum(c => c.AmountPaid);
            
            // سداد السابقات (من AccumulatedArrears - المبالغ المدفوعة في هذا الأسبوع)
            // نقرأ PaidAmount للأعضاء الذين LastWeekNumber == weekNumber
            var accumulatedArrearsRepo = new AccumulatedArrearsRepository();
            decimal previousArrearPayments = accumulatedArrearsRepo.GetAll()
                .Where(a => a.LastWeekNumber == weekNumber)
                .Sum(a => a.PaidAmount);
            
            // سداد متأخرات الأسبوع
            var arrearRepo = new ArrearRepository();
            var weekArrears = arrearRepo.GetArrearsByWeek(weekNumber);
            decimal arrearsPayments = weekArrears
                .Where(a => a.IsPaid && a.PaidDate.HasValue && 
                           a.PaidDate.Value.Date >= weekStart && a.PaidDate.Value.Date <= weekEnd)
                .Sum(a => a.PaidAmount);
            
            // ✅ دفعات أعضاء خلف الجمعية (نظام الأمانة)
            var behindAssociationRepo = new Data.Repositories.BehindAssociation.BehindAssociationRepository();
            decimal behindAssociationDeposits = behindAssociationRepo.GetWeekTotalDeposits(weekNumber);
            
            // الخرجيات والمفقودات
            var otherTransactionRepo = new OtherTransactionRepository();
            var otherTransactions = otherTransactionRepo.GetByWeek(weekNumber).ToList();
            decimal otherExpenses = otherTransactions.Sum(t => t.Amount);
            
            // إجمالي التحصيل (الصندوق) = التحصيل العادي + المتأخرات + السابقات + خلف الجمعية
            decimal totalIncome = todayPayments + arrearsPayments + previousArrearPayments + behindAssociationDeposits;
            
            // Debug: طباعة المكونات
            System.Diagnostics.Debug.WriteLine($"💰 [ReconciliationService] الصندوق - الأسبوع {weekNumber}:");
            System.Diagnostics.Debug.WriteLine($"  - التحصيل اليومي: {todayPayments:N2}");
            System.Diagnostics.Debug.WriteLine($"  - سداد متأخرات: {arrearsPayments:N2}");
            System.Diagnostics.Debug.WriteLine($"  - سداد سابقات: {previousArrearPayments:N2}");
            System.Diagnostics.Debug.WriteLine($"  - خلف الجمعية: {behindAssociationDeposits:N2}");
            System.Diagnostics.Debug.WriteLine($"  = الإجمالي: {totalIncome:N2}");
            
            // 3️⃣ المبلغ المتوقع = الصندوق الأسبوعي فقط
            // ✅ الجرد الأسبوعي مستقل عن الخزنة
            // ✅ يحسب فقط: الدخل - الخرجيات (من OtherTransactions)
            decimal expectedAmount = totalIncome - otherExpenses;
            
            return expectedAmount;
        }

        /// <summary>
        /// الحصول علمّى رقم الأسبوع الحالي
        /// </summary>
        public int GetCurrentWeekNumber()
        {
            var (weekNumber, _) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
            return weekNumber;
        }

        /// <summary>
        /// الحصول على رقم الأسبوع المسموح بجرده
        /// • إذا كان يوم الجمعة: الأسبوع الحالي
        /// • إذا لم يكن يوم الجمعة: الأسبوع السابق
        /// </summary>
        public int GetAllowedReconciliationWeek()
        {
            var (currentWeekNumber, currentDayNumber) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
            
            // إذا كان يوم الجمعة (7) → يمكن جرد الأسبوع الحالي
            if (currentDayNumber == 7)
                return currentWeekNumber;
            
            // في باقي الأيام → يمكن جرد الأسبوع السابق فقط
            return currentWeekNumber > 1 ? currentWeekNumber - 1 : 1;
        }

        /// <summary>
        /// التحقق من إمكانية جرد أسبوع معين
        /// • الأسبوع السابق: يمكن جرده في أي وقت
        /// • الأسبوع الحالي: يمكن جرده يوم الجمعة فقط
        /// </summary>
        public (bool CanReconcile, string Message) CanReconcileWeek(int weekNumber)
        {
            if (!WeekHelper.IsValidWeek(weekNumber))
                return (false, $"رقم الأسبوع غير صحيح");

            int currentWeek = GetCurrentWeekNumber();
            var (currentWeekNumber, currentDayNumber) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
            
            // لا يمكن جرد أسبوع مستقبلي
            if (weekNumber > currentWeek)
                return (false, $"لا يمكن جرد أسبوع مستقبلي");

            // إذا كان الأسبوع الحالي: يجب أن يكون يوم الجمعة
            if (weekNumber == currentWeek && currentDayNumber != 7)
            {
                string currentDayName = WeekHelper.GetArabicDayName(currentDayNumber);
                return (false, $"لا يمكن جرد الأسبوع الحالي إلا يوم الجمعة (اليوم الحالي: {currentDayName})");
            }

            // التحقق من عدم الجرد المسبق
            var (weekStart, _) = WeekHelper.GetWeekDateRange(weekNumber);
            var existingReconciliation = _reconciliationRepository.GetByWeek(weekStart);
            if (existingReconciliation != null)
                return (false, $"الأسبوع {weekNumber} مُجرد مسبقاً");

            return (true, $"يمكن جرد الأسبوع {weekNumber}");
        }

        /// <summary>
        /// إتمام الجرد الأسبوعي (بنظام 26 أسبوع)
        /// • الأسبوع السابق: يمكن جرده في أي وقت
        /// • الأسبوع الحالي: يمكن جرده يوم الجمعة فقط (اليوم 7)
        /// </summary>
        public (bool Success, string Message) SubmitReconciliation(int weekNumber, decimal actualAmount, string? notes, int performedBy)
        {
            if (!WeekHelper.IsValidWeek(weekNumber))
                return (false, $"رقم الأسبوع يجب أن يكون بين 1 و {WeekHelper.TotalWeeks}");

            // التحقق من الأسبوع المطلوب
            int currentWeek = GetCurrentWeekNumber();
            var (currentWeekNumber, currentDayNumber) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
            
            // لا يمكن جرد أسبوع مستقبلي
            if (weekNumber > currentWeek)
                return (false, $"⚠️ لا يمكن جرد أسبوع مستقبلي\n\nالأسبوع الحالي: {currentWeek}\nالأسبوع المحدد: {weekNumber}");

            // إذا كان الأسبوع الحالي: يجب أن يكون يوم الجمعة فقط
            if (weekNumber == currentWeek)
            {
                if (currentDayNumber != 7) // 7 = الجمعة
                {
                    string currentDayName = WeekHelper.GetArabicDayName(currentDayNumber);
                    return (false, $"⚠️ لا يمكن جرد الأسبوع الحالي إلا يوم الجمعة\n\n" +
                                   $"اليوم الحالي: {currentDayName} (اليوم {currentDayNumber})\n" +
                                   $"الأسبوع الحالي: {currentWeek}\n\n" +
                                   $"يرجى الانتظار حتى يوم الجمعة لجرد هذا الأسبوع");
                }
            }

            // التحقق من عدم جرد هذا الأسبوع مسبقاً
            var (weekStart, weekEnd) = WeekHelper.GetWeekDateRange(weekNumber);
            var existingReconciliation = _reconciliationRepository.GetByWeek(weekStart);
            if (existingReconciliation != null)
                return (false, $"⚠️ الأسبوع {weekNumber} مُجرد مسبقاً\n\n" +
                               $"تاريخ الجرد: {existingReconciliation.PerformedDate:yyyy-MM-dd HH:mm}\n" +
                               $"المبلغ الفعلي: {existingReconciliation.ActualAmount:N2} ريال");

            // ✅ التحقق من أن متأخرات هذا الأسبوع تم تحويلها مسبقاً
            var arrearsAlreadyConverted = _arrearService.CheckIfArrearsAlreadyConverted(weekNumber);

            return SubmitReconciliationByDate(weekStart, weekEnd, actualAmount, notes, performedBy, weekNumber, arrearsAlreadyConverted);
        }

        /// <summary>
        /// إتمام الجرد الأسبوعي بالتواريخ
        /// </summary>
        private (bool Success, string Message) SubmitReconciliationByDate(DateTime weekStart, DateTime weekEnd, decimal actualAmount, string? notes, int performedBy, int weekNumber, bool arrearsAlreadyConverted = false)
        {
            try
            {
                // التحقق من صحة رقم الأسبوع
                if (weekNumber <= 0)
                {
                    weekNumber = WeekHelper.GetWeekNumber(weekEnd);
                    System.Diagnostics.Debug.WriteLine($"⚠️ تم حساب رقم الأسبوع تلقائياً: {weekNumber}");
                }

                decimal expectedAmount = CalculateExpectedAmountByDate(weekStart, weekEnd);
                decimal difference = actualAmount - expectedAmount;

                // التحقق من الفرق الكبير
                if (Math.Abs(difference) > expectedAmount * 0.01m && string.IsNullOrWhiteSpace(notes))
                {
                    return (false, "يجب إدخال ملاحظات توضيحية عند وجود فرق كبير");
                }

                var reconciliation = new WeeklyReconciliation
                {
                    WeekNumber = weekNumber,
                    WeekStartDate = weekStart,
                    WeekEndDate = weekEnd,
                    ExpectedAmount = expectedAmount,
                    ActualAmount = actualAmount,
                    Difference = difference,
                    Notes = notes,
                    Status = ReconciliationStatus.Completed,
                    PerformedBy = performedBy
                };

                int reconciliationId = _reconciliationRepository.Add(reconciliation);

                // ترحيل المبلغ للخزنة (إيداع تلقائي)
                var vaultTransaction = new VaultTransaction
                {
                    TransactionType = TransactionType.Deposit,
                    Category = VaultTransactionCategory.WeeklyReconciliation,
                    Amount = actualAmount,
                    TransactionDate = weekEnd,
                    Description = $"ترحيل جرد أسبوعي - الأسبوع {weekNumber} ({weekStart:yyyy-MM-dd} - {weekEnd:yyyy-MM-dd})",
                    RelatedReconciliationID = reconciliationId,
                    PerformedBy = performedBy
                };
                _vaultRepository.Add(vaultTransaction);

                System.Diagnostics.Debug.WriteLine($"📊 بدء جرد الأسبوع {weekNumber}");
                System.Diagnostics.Debug.WriteLine($"   المتوقع: {expectedAmount:N2} ريال");
                System.Diagnostics.Debug.WriteLine($"   الفعلي: {actualAmount:N2} ريال");
                System.Diagnostics.Debug.WriteLine($"   الفرق: {difference:N2} ريال");
                System.Diagnostics.Debug.WriteLine($"   المتأخرات محولة مسبقاً: {arrearsAlreadyConverted}");

                string resultMessage = $"تم إتمام جرد الأسبوع {weekNumber} والترحيل للخزنة بنجاح";
                string auditDetails = $"إتمام الجرد الأسبوعي {weekNumber} - المتوقع: {expectedAmount:N2} - الفعلي: {actualAmount:N2}";

                // ✅ إذا لم يتم تحويل المتأخرات مسبقاً، نقوم بالتحويل الآن
                if (!arrearsAlreadyConverted)
                {
                    // 1. تحديث السابقات المتراكمة (تسجيل المدفوعات وتحديث LastWeekNumber)
                    var updateResult = _arrearService.UpdateAccumulatedArrearsOnReconciliation(weekNumber);
                    
                    // 2. تحويل المتأخرات إلى سابقات
                    var conversionResult = _arrearService.ConvertCurrentWeekArrearsToPrevious(weekNumber);
                    
                    resultMessage += $"\n{updateResult.Message}\n{conversionResult.Message}";
                    auditDetails += $" - {updateResult.Message} - {conversionResult.Message}";
                }
                else
                {
                    // ⚠️ المتأخرات محولة مسبقاً، نتخطى التحويل ونكتفي بالترحيل
                    resultMessage += "\n⚠️ تم تخطي تحويل المتأخرات (محولة مسبقاً) - تم ترحيل المبالغ فقط";
                    auditDetails += " - تخطي تحويل المتأخرات (محولة مسبقاً)";
                    
                    System.Diagnostics.Debug.WriteLine($"⚠️ الأسبوع {weekNumber}: تم تخطي تحويل المتأخرات - ترحيل المبالغ فقط");
                }
                
                _auditRepository.Add(new AuditLog
                {
                    UserID = performedBy,
                    Action = AuditAction.Create,
                    EntityType = EntityType.WeeklyReconciliation,
                    EntityID = reconciliationId,
                    Details = auditDetails
                });

                System.Diagnostics.Debug.WriteLine($"✅ اكتمل جرد الأسبوع {weekNumber} بنجاح");
                System.Diagnostics.Debug.WriteLine($"   تم ترحيل {actualAmount:N2} ريال للخزنة");
                System.Diagnostics.Debug.WriteLine($"   الأسبوع الحالي الآن: {weekNumber + 1}");

                return (true, resultMessage);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }
    }
}
