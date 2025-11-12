using System;
using System.Collections.Generic;
using System.Linq;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;
using Alwajeih.Utilities;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة ملخص الأسبوع - الحصول على البيانات المالية لكل أسبوع
    /// </summary>
    public class WeekSummaryService
    {
        private readonly DailyCollectionRepository _collectionRepo;
        private readonly SavingPlanRepository _planRepo;
        private readonly VaultRepository _vaultRepo;
        private readonly ArrearRepository _arrearRepo;
        private readonly AccumulatedArrearsRepository _accumulatedArrearsRepo;
        private readonly OtherTransactionRepository _otherTransactionRepo;
        
        public WeekSummaryService()
        {
            _collectionRepo = new DailyCollectionRepository();
            _planRepo = new SavingPlanRepository();
            _vaultRepo = new VaultRepository();
            _arrearRepo = new ArrearRepository();
            _accumulatedArrearsRepo = new AccumulatedArrearsRepository();
            _otherTransactionRepo = new OtherTransactionRepository();
        }
        
        /// <summary>
        /// الحصول على ملخص كامل لأسبوع معين
        /// </summary>
        public WeekSummary GetWeekSummary(int weekNumber)
        {
            if (!WeekHelper.IsValidWeek(weekNumber))
                throw new ArgumentException($"رقم الأسبوع يجب أن يكون بين 1 و {WeekHelper.TotalWeeks}");
            
            var summary = new WeekSummary
            {
                WeekNumber = weekNumber,
                PreviousWeekNumber = weekNumber - 1
            };
            
            // 1. حساب الصندوق (المبالغ في يد المستخدم - التحصيلات الفعلية)
            var collections = _collectionRepo.GetCollectionsByWeek(weekNumber)
                .Where(c => !c.IsCancelled).ToList();
            
            // التحصيل اليومي (AmountPaid)
            decimal todayPayments = collections.Sum(c => c.AmountPaid);
            
            // سداد السابقات (من AccumulatedArrears - المبالغ المدفوعة في هذا الأسبوع)
            // نقرأ PaidAmount للأعضاء الذين LastWeekNumber == weekNumber
            var accumulatedArrearsRepo = new AccumulatedArrearsRepository();
            summary.PreviousArrearPayments = accumulatedArrearsRepo.GetAll()
                .Where(a => a.LastWeekNumber == weekNumber)
                .Sum(a => a.PaidAmount);
            
            // سداد متأخرات الأسبوع (من DailyArrears التي تم سدادها في هذا الأسبوع)
            var weekArrears = _arrearRepo.GetArrearsByWeek(weekNumber);
            var (startDate, endDate) = WeekHelper.GetWeekDateRange(weekNumber);
            decimal arrearsPayments = weekArrears
                .Where(a => a.IsPaid && a.PaidDate.HasValue && 
                           a.PaidDate.Value.Date >= startDate && a.PaidDate.Value.Date <= endDate)
                .Sum(a => a.PaidAmount);
            
            // ✅ دفعات أعضاء خلف الجمعية (نظام الأمانة)
            var behindAssociationRepo = new Data.Repositories.BehindAssociation.BehindAssociationRepository();
            decimal behindAssociationDeposits = behindAssociationRepo.GetWeekTotalDeposits(weekNumber);
            
            // الخرجيات والمفقودات (من OtherTransactions)
            var otherTransactions = _otherTransactionRepo.GetByWeek(weekNumber).ToList();
            
            // إجمالي التحصيل (ما في يد المستخدم قبل طرح الخرجيات) = التحصيل العادي + المتأخرات + خلف الجمعية
            summary.TotalIncome = todayPayments + arrearsPayments + summary.PreviousArrearPayments + behindAssociationDeposits;
            summary.IncomeMembersCount = collections.Select(c => c.PlanID).Distinct().Count();
            
            // 2. حساب المستحقات (ماعليكم - المبالغ المطلوبة للأسبوع الحالي)
            var duePlans = _planRepo.GetDueForWeek(weekNumber);
            summary.TotalDues = duePlans.Sum(p => p.DailyAmount * WeekHelper.DaysPerWeek);
            summary.DuesMembersCount = duePlans.Count;
            
            // 3. حساب المتأخرات الجديدة (من جدول DailyArrears - غير المسددة)
            summary.TotalArrears = weekArrears.Where(a => !a.IsPaid).Sum(a => a.RemainingAmount);
            summary.ArrearsMembersCount = weekArrears.Where(a => !a.IsPaid).Select(a => a.PlanID).Distinct().Count();
            
            // 4. حساب السابقات المتراكمة (من جدول AccumulatedArrears)
            var accumulatedArrears = _accumulatedArrearsRepo.GetAll()
                .Where(a => !a.IsPaid && a.LastWeekNumber < weekNumber);
            summary.TotalPreviousArrears = accumulatedArrears.Sum(a => a.RemainingAmount);
            
            // الرصيد السابق من الجرد السابق
            if (weekNumber > 1)
            {
                var reconciliationRepo = new ReconciliationRepository();
                var (prevStart, prevEnd) = WeekHelper.GetWeekDateRange(weekNumber - 1);
                var previousReconciliations = reconciliationRepo.GetByDateRange(prevStart, prevEnd);
                var lastRecon = previousReconciliations.OrderByDescending(r => r.ReconciliationDate).FirstOrDefault();
                summary.PreviousBalance = lastRecon?.ActualAmount ?? 0;
            }
            
            // 5. حساب السحوبات (سحوبات الأعضاء من الخزنة)
            // استخدام نفس startDate و endDate المعرفة سابقاً
            var vaultTransactions = _vaultRepo.GetByDateRange(startDate, endDate)
                .Where(t => !t.IsCancelled).ToList();
            
            // سحوبات الأعضاء
            summary.MemberWithdrawals = vaultTransactions
                .Where(t => t.Category == VaultTransactionCategory.MemberWithdrawal)
                .Sum(t => t.Amount);
            
            // خرجيات المدير (منفصلة)
            summary.ManagerWithdrawals = vaultTransactions
                .Where(t => t.Category == VaultTransactionCategory.ManagerWithdrawals)
                .Sum(t => t.Amount);
            
            // 6. حساب خلف الجمعية
            summary.AssociationDebts = vaultTransactions
                .Where(t => t.Category == VaultTransactionCategory.AssociationDebt)
                .Sum(t => t.Amount);
            
            // 7. حساب الخرجيات والمفقودات (استخدام المتغير المعرف سابقاً)
            // otherTransactions تم تعريفه في السطر 66
            
            // الخرجيات (مصروفات)
            summary.Expenses = otherTransactions
                .Where(t => t.TransactionType == "خرجية" || t.TransactionType == "مصروف")
                .Sum(t => t.Amount);
            
            // المفقودات (منفصلة)
            summary.Missing = otherTransactions
                .Where(t => t.TransactionType == "مفقود")
                .Sum(t => t.Amount);
            
            // إجمالي المعاملات الأخرى
            summary.TotalOtherTransactions = otherTransactions.Sum(t => t.Amount);
            
            // 8. حساب الخريجات (الأعضاء الذين أنهوا الخطة)
            // TODO: إضافة عند توفر هذه البيانات
            summary.Graduates = 0;
            summary.GraduatesCount = 0;
            
            // 9. حساب الرصيد النهائي (الصندوق المتوقع)
            // الرصيد = الرصيد السابق + الصندوق + سداد السابقات - السحوبات - الخرجيات - المفقودات
            summary.FinalBalance = summary.PreviousBalance + 
                                  summary.TotalIncome + 
                                  summary.PreviousArrearPayments - 
                                  summary.MemberWithdrawals - 
                                  summary.ManagerWithdrawals - 
                                  summary.Expenses - 
                                  summary.Missing - 
                                  summary.AssociationDebts;
            
            // ملاحظة: المستحقات والمتأخرات لا تدخل في حساب الرصيد (هي للمتابعة فقط)
            
            return summary;
        }
        
        /// <summary>
        /// الحصول على التفاصيل اليومية لأسبوع معين
        /// </summary>
        public List<DailySummary> GetDailyDetails(int weekNumber, WeekSummaryItemType itemType)
        {
            var dailyDetails = new List<DailySummary>();
            
            for (int day = 1; day <= WeekHelper.DaysPerWeek; day++)
            {
                var dailySummary = new DailySummary
                {
                    WeekNumber = weekNumber,
                    DayNumber = day,
                    DayName = WeekHelper.GetArabicDayName(day),
                    Date = DateTime.Now // TODO: حساب التاريخ الفعلي
                };
                
                // الحصول على التحصيلات لهذا اليوم
                var collections = _collectionRepo.GetCollectionsByWeekAndDay(weekNumber, day);
                
                switch (itemType)
                {
                    case WeekSummaryItemType.Income:
                        // الواردات: عرض التحصيلات الفعلية (عادي فقط - السابقات في جدول منفصل)
                        dailySummary.Collections = collections;
                        dailySummary.CollectionsCount = collections.Count;
                        dailySummary.DailyIncome = collections.Sum(c => c.AmountPaid);
                        break;
                        
                    case WeekSummaryItemType.Dues:
                        // المستحقات: عرض الخطط المستحقة لهذا اليوم
                        var duePlans = _planRepo.GetDueForWeekDay(weekNumber, day);
                        
                        // تحويل الخطط إلى تحصيلات وهمية للعرض
                        var dueCollections = duePlans.Select(p => new DailyCollection
                        {
                            PlanID = p.PlanID,
                            MemberName = p.MemberName,
                            AmountPaid = p.DailyAmount,
                            WeekNumber = weekNumber,
                            DayNumber = day,
                            CollectedAt = DateTime.Now,
                            PaymentType = PaymentType.Cash, // قيمة افتراضية
                            PaymentTypeDescription = "مستحق"
                        }).ToList();
                        
                        dailySummary.Collections = dueCollections;
                        dailySummary.CollectionsCount = dueCollections.Count;
                        dailySummary.DailyDues = duePlans.Sum(p => p.DailyAmount);
                        break;
                        
                    case WeekSummaryItemType.Arrears:
                        // المتأخرات: عرض الأعضاء الذين لم يدفعوا أو دفعوا أقل من المستحق
                        var arrearPlans = _planRepo.GetDueForWeekDay(weekNumber, day);
                        var arrearCollections = new List<DailyCollection>();
                        
                        foreach (var plan in arrearPlans)
                        {
                            // البحث عن التحصيل لهذا العضو في هذا اليوم
                            var memberCollection = collections.FirstOrDefault(c => c.PlanID == plan.PlanID);
                            decimal collected = memberCollection?.AmountPaid ?? 0;
                            decimal expected = plan.DailyAmount;
                            decimal arrear = expected - collected;
                            
                            // إذا كان هناك متأخرات، أضفه للقائمة
                            if (arrear > 0)
                            {
                                arrearCollections.Add(new DailyCollection
                                {
                                    PlanID = plan.PlanID,
                                    MemberName = plan.MemberName,
                                    AmountPaid = arrear, // المبلغ المتأخر
                                    WeekNumber = weekNumber,
                                    DayNumber = day,
                                    CollectedAt = DateTime.Now,
                                    PaymentType = PaymentType.Cash, // قيمة افتراضية
                                    PaymentTypeDescription = $"متأخر (المستحق: {expected:N2}، المدفوع: {collected:N2})"
                                });
                            }
                        }
                        
                        dailySummary.Collections = arrearCollections;
                        dailySummary.CollectionsCount = arrearCollections.Count;
                        dailySummary.DailyArrears = arrearCollections.Sum(c => c.AmountPaid);
                        break;
                        
                    case WeekSummaryItemType.ManagerWithdrawals:
                        // خرجيات المدير: جلب من جدول VaultTransactions
                        var (startDate, endDate) = WeekHelper.GetWeekDateRange(weekNumber);
                        var dayDate = startDate.AddDays(day - 1);
                        
                        var withdrawals = _vaultRepo.GetByDateRange(dayDate, dayDate)
                            .Where(t => t.Category == VaultTransactionCategory.ManagerWithdrawals)
                            .ToList();
                        
                        // تحويل إلى تحصيلات للعرض
                        var withdrawalCollections = withdrawals.Select(w => new DailyCollection
                        {
                            MemberName = w.Description ?? "خرجية المدير",
                            AmountPaid = w.Amount,
                            WeekNumber = weekNumber,
                            DayNumber = day,
                            CollectedAt = w.TransactionDate,
                            PaymentType = PaymentType.Cash, // قيمة افتراضية
                            PaymentTypeDescription = "خرجية"
                        }).ToList();
                        
                        dailySummary.Collections = withdrawalCollections;
                        dailySummary.CollectionsCount = withdrawalCollections.Count;
                        dailySummary.DailyWithdrawals = withdrawals.Sum(w => w.Amount);
                        break;
                        
                    default:
                        dailySummary.Collections = new List<DailyCollection>();
                        dailySummary.CollectionsCount = 0;
                        break;
                }
                
                dailyDetails.Add(dailySummary);
            }
            
            return dailyDetails;
        }
    }
}
