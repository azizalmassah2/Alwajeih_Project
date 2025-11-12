using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;

namespace Alwajeih.Services
{
    public partial class ReportService
    {
        private readonly CollectionRepository _collectionRepository;
        private readonly ArrearRepository _arrearRepository;
        private readonly VaultRepository _vaultRepository;
        private readonly ReconciliationRepository _reconciliationRepository;
        private readonly MemberRepository _memberRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly ExternalPaymentRepository _externalPaymentRepository;
        private readonly SystemSettingsRepository _settingsRepository;

        public ReportService()
        {
            _collectionRepository = new CollectionRepository();
            _arrearRepository = new ArrearRepository();
            _vaultRepository = new VaultRepository();
            _reconciliationRepository = new ReconciliationRepository();
            _memberRepository = new MemberRepository();
            _planRepository = new SavingPlanRepository();
            _externalPaymentRepository = new ExternalPaymentRepository();
            _settingsRepository = new SystemSettingsRepository();
        }

        /// <summary>
        /// التحقق من صحة نطاق التواريخ
        /// </summary>
        private (bool isValid, string message) ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            var settings = _settingsRepository.GetCurrentSettings();
            if (settings == null)
                return (false, "لم يتم العثور على إعدادات النظام");

            if (startDate < settings.StartDate)
                return (false, $"تاريخ البداية لا يمكن أن يكون قبل بداية الجمعية ({settings.StartDate:yyyy-MM-dd})");

            if (endDate > settings.EndDate)
                return (false, $"تاريخ النهاية لا يمكن أن يكون بعد نهاية الجمعية ({settings.EndDate:yyyy-MM-dd})");

            if (startDate > endDate)
                return (false, "تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            return (true, string.Empty);
        }

        public DataTable GenerateDailyReport(DateTime date)
        {
            var collections = _collectionRepository.GetByDateRange(date, date).ToList();
            
            var dt = new DataTable();
            dt.Columns.Add("العضو");
            dt.Columns.Add("المبلغ المدفوع");
            dt.Columns.Add("نوع الدفع");
            dt.Columns.Add("الوقت");

            foreach (var c in collections)
            {
                dt.Rows.Add(c.MemberName, c.AmountPaid, c.PaymentType, c.CollectedAt);
            }

            return dt;
        }

        public DataTable GenerateWeeklyReport(DateTime weekStart, DateTime weekEnd)
        {
            var collections = _collectionRepository.GetByDateRange(weekStart, weekEnd).ToList();
            
            var dt = new DataTable();
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("عدد التحصيلات");
            dt.Columns.Add("إجمالي المبلغ");

            var grouped = collections.GroupBy(c => c.CollectionDate.Date);
            foreach (var group in grouped)
            {
                dt.Rows.Add(
                    group.Key.ToString("yyyy-MM-dd"),
                    group.Count(),
                    group.Sum(c => c.AmountPaid)
                );
            }

            return dt;
        }

        public DataTable GenerateMemberReport(int memberId)
        {
            var member = _memberRepository.GetById(memberId);
            var plans = _planRepository.GetByMemberId(memberId);
            
            var dt = new DataTable();
            dt.Columns.Add("المبلغ اليومي");
            dt.Columns.Add("تاريخ البداية");
            dt.Columns.Add("تاريخ النهاية");
            dt.Columns.Add("المبلغ الإجمالي");
            dt.Columns.Add("الحالة");

            foreach (var plan in plans)
            {
                dt.Rows.Add(
                    plan.DailyAmount,
                    plan.StartDate.ToString("yyyy-MM-dd"),
                    plan.EndDate.ToString("yyyy-MM-dd"),
                    plan.TotalAmount,
                    plan.Status
                );
            }

            return dt;
        }

        public DataTable GenerateVaultReport(DateTime startDate, DateTime endDate)
        {
            var transactions = _vaultRepository.GetByDateRange(startDate, endDate).ToList();
            
            var dt = new DataTable();
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("النوع");
            dt.Columns.Add("الفئة");
            dt.Columns.Add("المبلغ");
            dt.Columns.Add("الوصف");
            dt.Columns.Add("العضو");
            dt.Columns.Add("المستخدم");

            foreach (var t in transactions)
            {
                string category = t.Category switch
                {
                    VaultTransactionCategory.MemberWithdrawal => "سحب لعضو",
                    VaultTransactionCategory.BehindAssociationWithdrawal => "سحب خلف الجمعية",
                    VaultTransactionCategory.ManagerWithdrawals => "خرجيات",
                    VaultTransactionCategory.Missing => "مفقود",
                    VaultTransactionCategory.OperatingExpense => "مصروف",
                    _ => "أخرى"
                };
                
                dt.Rows.Add(
                    t.TransactionDate.ToString("yyyy-MM-dd"),
                    t.TransactionType == TransactionType.Deposit ? "إيداع" : 
                    t.TransactionType == TransactionType.Withdrawal ? "سحب" : "مصروف",
                    category,
                    t.Amount,
                    t.Description,
                    t.MemberName ?? "-",
                    t.UserName
                );
            }

            return dt;
        }

        public DataTable GenerateArrearsReport()
        {
            var arrears = _arrearRepository.GetAllUnpaid().ToList();
            
            var dt = new DataTable();
            dt.Columns.Add("العضو");
            dt.Columns.Add("تاريخ المتأخرة");
            dt.Columns.Add("المبلغ المستحق");
            dt.Columns.Add("المبلغ المتبقي");
            dt.Columns.Add("أيام التأخير");

            foreach (var arrear in arrears)
            {
                var daysOverdue = (DateTime.Now.Date - arrear.ArrearDate.Date).Days;
                dt.Rows.Add(
                    arrear.MemberName,
                    arrear.ArrearDate.ToString("yyyy-MM-dd"),
                    arrear.AmountDue,
                    arrear.RemainingAmount,
                    daysOverdue
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير مالي شامل - بشكل جدول منظم
        /// </summary>
        public DataTable GenerateComprehensiveFinancialReport(DateTime startDate, DateTime endDate)
        {
            var dt = new DataTable();
            
            // الأعمدة
            dt.Columns.Add("التصنيف");
            dt.Columns.Add("البند");
            dt.Columns.Add("المبلغ", typeof(decimal));
            dt.Columns.Add("عدد العمليات", typeof(int));
            dt.Columns.Add("النسبة المئوية");
            dt.Columns.Add("الملاحظات");

            var collections = _collectionRepository.GetByDateRange(startDate, endDate).Where(c => !c.IsCancelled).ToList();
            var vaultTransactions = _vaultRepository.GetByDateRange(startDate, endDate).Where(t => !t.IsCancelled).ToList();
            var externalPayments = _externalPaymentRepository.GetByDateRange(startDate, endDate);
            
            var totalIncome = collections.Sum(c => c.AmountPaid);
            
            // ✅ تصنيف السحوبات حسب الفئة
            var memberWithdrawals = vaultTransactions.Where(t => t.Category == VaultTransactionCategory.MemberWithdrawal).Sum(t => t.Amount);
            var behindAssociationWithdrawals = vaultTransactions.Where(t => t.Category == VaultTransactionCategory.BehindAssociationWithdrawal).Sum(t => t.Amount);
            var managerWithdrawals = vaultTransactions.Where(t => t.Category == VaultTransactionCategory.ManagerWithdrawals).Sum(t => t.Amount);
            var missingAmount = vaultTransactions.Where(t => t.Category == VaultTransactionCategory.Missing).Sum(t => t.Amount);
            var operatingExpenses = vaultTransactions.Where(t => t.Category == VaultTransactionCategory.OperatingExpense).Sum(t => t.Amount);
            var otherWithdrawals = vaultTransactions.Where(t => t.TransactionType == TransactionType.Withdrawal && 
                t.Category != VaultTransactionCategory.MemberWithdrawal &&
                t.Category != VaultTransactionCategory.BehindAssociationWithdrawal &&
                t.Category != VaultTransactionCategory.ManagerWithdrawals &&
                t.Category != VaultTransactionCategory.Missing).Sum(t => t.Amount);
            
            var totalWithdrawals = memberWithdrawals + behindAssociationWithdrawals + managerWithdrawals + missingAmount + otherWithdrawals;
            var totalExpenses = operatingExpenses + vaultTransactions.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount);
            var totalExternal = externalPayments.Sum(e => e.Amount);
            var netBalance = totalIncome - totalWithdrawals - totalExpenses;
            var currentVaultBalance = _vaultRepository.GetCurrentBalance();

            // قسم الواردات
            dt.Rows.Add("الواردات", "التحصيلات اليومية", totalIncome, collections.Count, 
                totalIncome > 0 ? "100%" : "0%", $"الفترة: {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
            
            if (totalExternal > 0)
                dt.Rows.Add("الواردات", "المدفوعات الخارجية", totalExternal, externalPayments.Count(), 
                    $"{(totalExternal / (totalIncome + totalExternal)) * 100:F1}%", "كريمي - تحويلات");

            // قسم المصروفات - مفصلة حسب الفئة
            if (memberWithdrawals > 0)
                dt.Rows.Add("المصروفات", "سحوبات الأعضاء", memberWithdrawals, 
                    vaultTransactions.Count(t => t.Category == VaultTransactionCategory.MemberWithdrawal),
                    totalIncome > 0 ? $"{(memberWithdrawals / totalIncome) * 100:F1}%" : "0%", "سحب للأعضاء العاديين");
            
            if (behindAssociationWithdrawals > 0)
                dt.Rows.Add("المصروفات", "سحوبات خلف الجمعية", behindAssociationWithdrawals, 
                    vaultTransactions.Count(t => t.Category == VaultTransactionCategory.BehindAssociationWithdrawal),
                    totalIncome > 0 ? $"{(behindAssociationWithdrawals / totalIncome) * 100:F1}%" : "0%", "سحب لأعضاء خلف الجمعية");
            
            if (managerWithdrawals > 0)
                dt.Rows.Add("المصروفات", "خرجيات المدير", managerWithdrawals, 
                    vaultTransactions.Count(t => t.Category == VaultTransactionCategory.ManagerWithdrawals),
                    totalIncome > 0 ? $"{(managerWithdrawals / totalIncome) * 100:F1}%" : "0%", "خرجيات إدارية");
            
            if (missingAmount > 0)
                dt.Rows.Add("المصروفات", "مفقودات", missingAmount, 
                    vaultTransactions.Count(t => t.Category == VaultTransactionCategory.Missing),
                    totalIncome > 0 ? $"{(missingAmount / totalIncome) * 100:F1}%" : "0%", "مبالغ مفقودة");
            
            if (operatingExpenses > 0)
                dt.Rows.Add("المصروفات", "مصروفات تشغيلية", operatingExpenses,
                    vaultTransactions.Count(t => t.Category == VaultTransactionCategory.OperatingExpense),
                    totalIncome > 0 ? $"{(operatingExpenses / totalIncome) * 100:F1}%" : "0%", "مصاريف التشغيل");
            
            if (otherWithdrawals > 0)
                dt.Rows.Add("المصروفات", "سحوبات أخرى", otherWithdrawals,
                    vaultTransactions.Count(t => t.TransactionType == TransactionType.Withdrawal && 
                        t.Category != VaultTransactionCategory.MemberWithdrawal &&
                        t.Category != VaultTransactionCategory.BehindAssociationWithdrawal &&
                        t.Category != VaultTransactionCategory.ManagerWithdrawals &&
                        t.Category != VaultTransactionCategory.Missing),
                    totalIncome > 0 ? $"{(otherWithdrawals / totalIncome) * 100:F1}%" : "0%", "سحوبات متنوعة");

            // قسم الصافي والأرصدة
            dt.Rows.Add("الإجماليات", "صافي الرصيد", netBalance, 0, 
                totalIncome > 0 ? $"{(netBalance / totalIncome) * 100:F1}%" : "0%", 
                netBalance >= 0 ? "موجب" : "سالب");
            
            dt.Rows.Add("الخزنة", "الرصيد الحالي", currentVaultBalance, 0, "-", 
                currentVaultBalance >= 0 ? "جيد" : "عجز");

            // قسم الإحصائيات
            var activeMembers = _memberRepository.GetAll().Count(m => !m.IsArchived);
            var activePlans = _planRepository.GetAll().Count(p => p.Status == PlanStatus.Active);
            var avgCollection = collections.Count > 0 ? totalIncome / collections.Count : 0;

            dt.Rows.Add("الإحصائيات", "عدد الأعضاء النشطين", activeMembers, 0, "-", "أعضاء غير مؤرشفين");
            dt.Rows.Add("الإحصائيات", "عدد الأسهم النشطة", activePlans, 0, "-", "أسهم قيد التشغيل");
            dt.Rows.Add("الإحصائيات", "متوسط التحصيل اليومي", avgCollection, 0, "-", "متوسط المبلغ لكل تحصيلة");

            return dt;
        }

        /// <summary>
        /// تقرير تفصيلي للعضو
        /// </summary>
        public DataTable GenerateDetailedMemberReport(int memberId)
        {
            var member = _memberRepository.GetById(memberId);
            if (member == null)
                throw new Exception("العضو غير موجود");

            var plans = _planRepository.GetByMemberId(memberId).OrderBy(p => p.PlanNumber).ToList();
            var dt = new DataTable();
            
            // ✅ إضافة معلومات العضو كخصائص
            dt.TableName = $"تقرير تفصيلي - {member.Name}";
            dt.ExtendedProperties["MemberName"] = member.Name;
            dt.ExtendedProperties["MemberPhone"] = member.Phone ?? "-";
            dt.ExtendedProperties["MemberType"] = member.MemberType == MemberType.Regular ? "أساسي" : "خلف الجمعية";
            dt.ExtendedProperties["JoinDate"] = member.CreatedDate.ToString("yyyy-MM-dd");
            dt.ExtendedProperties["TotalPlans"] = plans.Count.ToString();
            
            dt.Columns.Add("تاريخ البدء");
            dt.Columns.Add("تاريخ الانتهاء");
            dt.Columns.Add("المبلغ اليومي", typeof(decimal));
            dt.Columns.Add("المبلغ الإجمالي", typeof(decimal));
            dt.Columns.Add("المبلغ المدفوع", typeof(decimal));
            dt.Columns.Add("المتبقي", typeof(decimal));
            dt.Columns.Add("نسبة الإنجاز");
            dt.Columns.Add("المتأخرات", typeof(decimal));
            dt.Columns.Add("عدد التحصيلات", typeof(int));
            dt.Columns.Add("الحالة");

            foreach (var plan in plans)
            {
                var collections = _collectionRepository.GetByPlanId(plan.PlanID).Where(c => !c.IsCancelled).ToList();
                var totalPaid = collections.Sum(c => c.AmountPaid);
                var remaining = plan.TotalAmount - totalPaid;
                var progress = plan.TotalAmount > 0 ? (totalPaid / plan.TotalAmount) * 100 : 0;
                var arrears = _arrearRepository.GetByPlanId(plan.PlanID).Where(a => !a.IsPaid).Sum(a => a.RemainingAmount);
                var statusText = plan.Status == PlanStatus.Active ? "✅ نشطة" : 
                                (plan.Status == PlanStatus.Completed ? "🎉 مكتملة" : "📦 مؤرشفة");

                dt.Rows.Add(
                    plan.StartDate.ToString("yyyy-MM-dd"),
                    plan.EndDate.ToString("yyyy-MM-dd"),
                    plan.DailyAmount,
                    plan.TotalAmount,
                    totalPaid,
                    remaining,
                    $"{progress:F1}%",
                    arrears,
                    collections.Count,
                    statusText
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير شامل لجميع الأعضاء - بشكل جدول منظم
        /// </summary>
        public DataTable GenerateAllMembersReport()
        {
            var dt = new DataTable();
            
            // ✅ إضافة معلومات التقرير
            dt.TableName = "تقرير جميع الأعضاء";
            dt.ExtendedProperties["ReportDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            
            dt.Columns.Add("الاسم");
            dt.Columns.Add("نوع العضوية");
            dt.Columns.Add("الهاتف");
            dt.Columns.Add("عدد الأسهم", typeof(int));
            dt.Columns.Add("إجمالي المبالغ", typeof(decimal));
            dt.Columns.Add("المدفوع", typeof(decimal));
            dt.Columns.Add("المتبقي", typeof(decimal));
            dt.Columns.Add("نسبة الإنجاز");
            dt.Columns.Add("المتأخرات", typeof(decimal));
            dt.Columns.Add("تاريخ الانضمام");
            dt.Columns.Add("الحالة");

            var members = _memberRepository.GetAll().Where(m => !m.IsArchived).ToList();

            foreach (var member in members)
            {
                var plans = _planRepository.GetByMemberId(member.MemberID);
                var activePlans = plans.Count(p => p.Status == PlanStatus.Active);
                
                decimal totalPaid = 0;
                decimal totalRemaining = 0;
                decimal totalArrears = 0;
                decimal totalAmount = 0;

                foreach (var plan in plans)
                {
                    var paid = _collectionRepository.GetByPlanId(plan.PlanID).Where(c => !c.IsCancelled).Sum(c => c.AmountPaid);
                    totalPaid += paid;
                    totalAmount += plan.TotalAmount;
                    totalRemaining += plan.TotalAmount - paid;
                    totalArrears += _arrearRepository.GetByPlanId(plan.PlanID).Where(a => !a.IsPaid).Sum(a => a.RemainingAmount);
                }

                var memberTypeText = member.MemberType == MemberType.Regular ? "👤 أساسي" : "💰 خلف الجمعية";
                var completionRate = totalAmount > 0 ? (totalPaid / totalAmount) * 100 : 0;
                var status = totalArrears > 0 ? "⚠️ متأخر" : 
                    activePlans > 0 ? "✅ نشط" : 
                    plans.Any(p => p.Status == PlanStatus.Completed) ? "🎉 مكتمل" : "⚪ عادي";

                dt.Rows.Add(
                    member.Name,
                    memberTypeText,
                    member.Phone ?? "-",
                    plans.Count(),
                    totalAmount,
                    totalPaid,
                    totalRemaining,
                    $"{completionRate:F1}%",
                    totalArrears,
                    member.CreatedDate.ToString("yyyy-MM-dd"),
                    status
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير الأعضاء العاديين فقط
        /// </summary>
        public DataTable GenerateRegularMembersReport()
        {
            var dt = new DataTable();
            
            // ✅ إضافة معلومات التقرير
            dt.TableName = "تقرير الأعضاء العاديين";
            dt.ExtendedProperties["ReportDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            
            dt.Columns.Add("الاسم");
            dt.Columns.Add("الهاتف");
            dt.Columns.Add("عدد الأسهم", typeof(int));
            dt.Columns.Add("إجمالي المبالغ", typeof(decimal));
            dt.Columns.Add("المدفوع", typeof(decimal));
            dt.Columns.Add("المتبقي", typeof(decimal));
            dt.Columns.Add("نسبة الإنجاز");
            dt.Columns.Add("المتأخرات", typeof(decimal));
            dt.Columns.Add("تاريخ الانضمام");
            dt.Columns.Add("الحالة");

            var members = _memberRepository.GetAll()
                .Where(m => !m.IsArchived && m.MemberType == MemberType.Regular)
                .OrderBy(m => m.Name)
                .ToList();

            foreach (var member in members)
            {
                var plans = _planRepository.GetByMemberId(member.MemberID);
                var activePlans = plans.Count(p => p.Status == PlanStatus.Active);
                
                decimal totalPaid = 0;
                decimal totalRemaining = 0;
                decimal totalArrears = 0;
                decimal totalAmount = 0;

                foreach (var plan in plans)
                {
                    var paid = _collectionRepository.GetByPlanId(plan.PlanID).Where(c => !c.IsCancelled).Sum(c => c.AmountPaid);
                    totalPaid += paid;
                    totalAmount += plan.TotalAmount;
                    totalRemaining += plan.TotalAmount - paid;
                    totalArrears += _arrearRepository.GetByPlanId(plan.PlanID).Where(a => !a.IsPaid).Sum(a => a.RemainingAmount);
                }

                var completionRate = totalAmount > 0 ? (totalPaid / totalAmount) * 100 : 0;
                var status = totalArrears > 0 ? "⚠️ متأخر" : 
                    activePlans > 0 ? "✅ نشط" : 
                    plans.Any(p => p.Status == PlanStatus.Completed) ? "🎉 مكتمل" : "⚪ عادي";

                dt.Rows.Add(
                    member.Name,
                    member.Phone ?? "-",
                    plans.Count(),
                    totalAmount,
                    totalPaid,
                    totalRemaining,
                    $"{completionRate:F1}%",
                    totalArrears,
                    member.CreatedDate.ToString("yyyy-MM-dd"),
                    status
                );
            }
            
            // إضافة صف الإجماليات
            if (members.Any())
            {
                var allPlans = members.SelectMany(m => _planRepository.GetByMemberId(m.MemberID)).ToList();
                var grandTotalAmount = allPlans.Sum(p => p.TotalAmount);
                var grandTotalPaid = allPlans.Sum(p => 
                    _collectionRepository.GetByPlanId(p.PlanID).Where(c => !c.IsCancelled).Sum(c => c.AmountPaid));
                var grandTotalRemaining = grandTotalAmount - grandTotalPaid;
                var grandTotalArrears = allPlans.Sum(p => 
                    _arrearRepository.GetByPlanId(p.PlanID).Where(a => !a.IsPaid).Sum(a => a.RemainingAmount));
                var avgCompletion = grandTotalAmount > 0 ? (grandTotalPaid / grandTotalAmount) * 100 : 0;

                dt.Rows.Add(
                    $"📊 الإجمالي ({members.Count} عضو)",
                    "-",
                    allPlans.Count,
                    grandTotalAmount,
                    grandTotalPaid,
                    grandTotalRemaining,
                    $"{avgCompletion:F1}%",
                    grandTotalArrears,
                    "-",
                    ""
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير أعضاء خلف الجمعية - الملخص
        /// </summary>
        public DataTable GenerateBehindAssociationMembersOnlyReport()
        {
            var dt = new DataTable();
            
            // ✅ إضافة معلومات التقرير
            dt.TableName = "تقرير أعضاء خلف الجمعية";
            dt.ExtendedProperties["ReportDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            
            dt.Columns.Add("الاسم");
            dt.Columns.Add("الهاتف");
            dt.Columns.Add("إجمالي الإيداعات", typeof(decimal));
            dt.Columns.Add("إجمالي السحوبات", typeof(decimal));
            dt.Columns.Add("الرصيد الحالي", typeof(decimal));
            dt.Columns.Add("عدد المعاملات", typeof(int));
            dt.Columns.Add("آخر معاملة");
            dt.Columns.Add("تاريخ الانضمام");
            dt.Columns.Add("الحالة");

            var members = _memberRepository.GetAll()
                .Where(m => !m.IsArchived && m.MemberType == MemberType.BehindAssociation)
                .OrderBy(m => m.Name)
                .ToList();
            
            var behindAssociationRepo = new Data.Repositories.BehindAssociation.BehindAssociationRepository();

            foreach (var member in members)
            {
                var summary = behindAssociationRepo.GetMemberSummary(member.MemberID);
                var lastTransaction = behindAssociationRepo.GetMemberTransactions(member.MemberID)
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefault();
                
                var status = (summary?.CurrentBalance ?? 0) > 0 ? "✅ لديه رصيد" : 
                             (summary?.CurrentBalance ?? 0) < 0 ? "⚠️ رصيد سالب" : "⚪ رصيد صفر";

                dt.Rows.Add(
                    member.Name,
                    member.Phone ?? "-",
                    summary?.TotalDeposits ?? 0,
                    summary?.TotalWithdrawals ?? 0,
                    summary?.CurrentBalance ?? 0,
                    summary?.TransactionCount ?? 0,
                    lastTransaction?.TransactionDate.ToString("yyyy-MM-dd") ?? "-",
                    member.CreatedDate.ToString("yyyy-MM-dd"),
                    status
                );
            }
            
            // إضافة صف الإجماليات
            if (members.Any())
            {
                var allSummaries = members.Select(m => behindAssociationRepo.GetMemberSummary(m.MemberID))
                    .Where(s => s != null).ToList();
                
                dt.Rows.Add(
                    $"📊 الإجمالي ({members.Count} عضو)",
                    "-",
                    allSummaries.Sum(s => s.TotalDeposits),
                    allSummaries.Sum(s => s.TotalWithdrawals),
                    allSummaries.Sum(s => s.CurrentBalance),
                    allSummaries.Sum(s => s.TransactionCount),
                    "-",
                    "-",
                    $"{allSummaries.Count(s => s.CurrentBalance > 0)} لديهم رصيد"
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير التحصيلات المفصل
        /// </summary>
        public DataTable GenerateDetailedCollectionsReport(DateTime startDate, DateTime endDate)
        {
            var collections = _collectionRepository.GetByDateRange(startDate, endDate).Where(c => !c.IsCancelled).ToList();
            
            var dt = new DataTable();
            
            // ✅ إضافة معلومات التقرير
            dt.TableName = "تقرير التحصيلات المفصل";
            dt.ExtendedProperties["ReportPeriod"] = $"{startDate:yyyy-MM-dd} إلى {endDate:yyyy-MM-dd}";
            dt.ExtendedProperties["TotalCollections"] = collections.Count.ToString();
            dt.ExtendedProperties["TotalAmount"] = collections.Sum(c => c.AmountPaid).ToString("N2");
            
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("العضو");
            dt.Columns.Add("المبلغ", typeof(decimal));
            dt.Columns.Add("مصدر الدفع");
            dt.Columns.Add("رقم المرجع");
            dt.Columns.Add("رقم الإيصال");

            foreach (var c in collections.OrderBy(c => c.CollectionDate))
            {
                var paymentSource = c.PaymentSource switch
                {
                    PaymentSource.Cash => "💵 نقدي",
                    PaymentSource.Karimi => "💳 كريمي",
                    PaymentSource.BankTransfer => "🏦 تحويل بنكي",
                    _ => "أخرى"
                };

                dt.Rows.Add(
                    c.CollectionDate.ToString("yyyy-MM-dd HH:mm"),
                    c.MemberName,
                    c.AmountPaid,
                    paymentSource,
                    c.ReferenceNumber ?? "-",
                    c.ReceiptNumber ?? "-"
                );
            }
            
            // إضافة صف الإجمالي
            if (collections.Any())
            {
                dt.Rows.Add(
                    "-",
                    "📊 الإجمالي",
                    collections.Sum(c => c.AmountPaid),
                    "-",
                    "-",
                    "-"
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير المتأخرات الشامل - ملخص لكل عضو
        /// </summary>
        public DataTable GenerateComprehensiveArrearsReport()
        {
            var dt = new DataTable();
            
            // ✅ إضافة معلومات التقرير
            dt.TableName = "تقرير المتأخرات الشامل";
            dt.ExtendedProperties["ReportDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            
            dt.Columns.Add("اسم العضو");
            dt.Columns.Add("الهاتف");
            dt.Columns.Add("عدد المتأخرات", typeof(int));
            dt.Columns.Add("إجمالي المستحق", typeof(decimal));
            dt.Columns.Add("المدفوع", typeof(decimal));
            dt.Columns.Add("المتبقي", typeof(decimal));
            dt.Columns.Add("متوسط أيام التأخير", typeof(int));
            dt.Columns.Add("الحالة");

            var arrears = _arrearRepository.GetAllUnpaid().OrderBy(a => a.MemberName).ToList();
            
            // تجميع حسب العضو
            var memberGroups = arrears.GroupBy(a => a.MemberName);

            foreach (var group in memberGroups)
            {
                var totalDue = group.Sum(a => a.AmountDue);
                var totalPaid = group.Sum(a => a.AmountDue - a.RemainingAmount);
                var totalRemaining = group.Sum(a => a.RemainingAmount);
                var avgDaysOverdue = (int)group.Average(a => (DateTime.Now.Date - a.ArrearDate.Date).Days);
                var count = group.Count();
                
                // الحصول على هاتف العضو من PlanID
                var firstArrear = group.First();
                var plan = _planRepository.GetById(firstArrear.PlanID);
                var member = plan != null ? _memberRepository.GetById(plan.MemberID) : null;
                var phone = member?.Phone ?? "-";
                
                var status = totalRemaining == 0 ? "✅ مسدد بالكامل" :
                    totalPaid > 0 ? "🔸 مدفوع جزئياً" : "⚠️ غير مسدد";

                dt.Rows.Add(
                    group.Key,
                    phone,
                    count,
                    totalDue,
                    totalPaid,
                    totalRemaining,
                    avgDaysOverdue,
                    status
                );
            }

            // إضافة صف الإجماليات
            if (memberGroups.Any())
            {
                var grandTotalDue = arrears.Sum(a => a.AmountDue);
                var grandTotalPaid = arrears.Sum(a => a.AmountDue - a.RemainingAmount);
                var grandTotalRemaining = arrears.Sum(a => a.RemainingAmount);
                var grandAvgDays = (int)arrears.Average(a => (DateTime.Now.Date - a.ArrearDate.Date).Days);

                dt.Rows.Add(
                    "📊 الإجماليات",
                    "-",
                    arrears.Count,
                    grandTotalDue,
                    grandTotalPaid,
                    grandTotalRemaining,
                    grandAvgDays,
                    ""
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير السوابق - مُجمّع حسب العضو
        /// </summary>
        public DataTable GeneratePreviousArrearsReport()
        {
            var dt = new DataTable();
            
            // ✅ إضافة معلومات التقرير
            dt.TableName = "تقرير السوابق";
            dt.ExtendedProperties["ReportDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            
            dt.Columns.Add("اسم العضو");
            dt.Columns.Add("الهاتف");
            dt.Columns.Add("عدد الأسهم", typeof(int));
            dt.Columns.Add("إجمالي السوابق", typeof(decimal));
            dt.Columns.Add("المدفوع", typeof(decimal));
            dt.Columns.Add("المتبقي", typeof(decimal));
            dt.Columns.Add("نسبة السداد");
            dt.Columns.Add("الحالة");

            var previousArrears = _arrearRepository.GetAllPreviousArrears().OrderBy(pa => pa.MemberName).ToList();

            // تجميع حسب العضو
            var groupedByMember = previousArrears.GroupBy(pa => new { pa.MemberName, pa.PlanID })
                .Select(g => new
                {
                    MemberName = g.Key.MemberName,
                    PlanID = g.Key.PlanID,
                    PlanCount = g.Select(x => x.PlanID).Distinct().Count(),
                    TotalOriginal = g.Sum(x => x.TotalArrears),
                    TotalRemaining = g.Sum(x => x.RemainingAmount),
                    TotalPaid = g.Sum(x => x.PaidAmount) // ✅ استخدام PaidAmount الفعلي
                })
                .GroupBy(x => x.MemberName)
                .Select(g => new
                {
                    MemberName = g.Key,
                    PlanID = g.First().PlanID, // للحصول على MemberID
                    PlanCount = g.Sum(x => x.PlanCount),
                    TotalOriginal = g.Sum(x => x.TotalOriginal),
                    TotalRemaining = g.Sum(x => x.TotalRemaining),
                    TotalPaid = g.Sum(x => x.TotalPaid)
                })
                .OrderBy(x => x.MemberName)
                .ToList();

            foreach (var memberData in groupedByMember)
            {
                var percentage = memberData.TotalOriginal > 0 
                    ? (memberData.TotalPaid / memberData.TotalOriginal * 100) 
                    : 0;
                
                // الحصول على هاتف العضو من PlanID
                var plan = _planRepository.GetById(memberData.PlanID);
                var member = plan != null ? _memberRepository.GetById(plan.MemberID) : null;
                var phone = member?.Phone ?? "-";
                
                var status = memberData.TotalRemaining == 0 ? "✅ مسدد بالكامل" :
                    memberData.TotalPaid > 0 ? "🔸 مدفوع جزئياً" : "⚠️ غير مسدد";

                dt.Rows.Add(
                    memberData.MemberName,
                    phone,
                    memberData.PlanCount,
                    memberData.TotalOriginal,
                    memberData.TotalPaid,
                    memberData.TotalRemaining,
                    $"{percentage:N1}%",
                    status
                );
            }

            // إضافة صف الإجمالي
            if (groupedByMember.Any())
            {
                var totalOriginal = groupedByMember.Sum(x => x.TotalOriginal);
                var totalPaid = groupedByMember.Sum(x => x.TotalPaid);
                var totalRemaining = groupedByMember.Sum(x => x.TotalRemaining);
                var totalPercentage = totalOriginal > 0 ? (totalPaid / totalOriginal * 100) : 0;

                dt.Rows.Add(
                    "📊 الإجمالي",
                    "-",
                    groupedByMember.Sum(x => x.PlanCount),
                    totalOriginal,
                    totalPaid,
                    totalRemaining,
                    $"{totalPercentage:N1}%",
                    ""
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير الخزنة المفصل
        /// </summary>
        public DataTable GenerateDetailedVaultReport(DateTime startDate, DateTime endDate)
        {
            var transactions = _vaultRepository.GetByDateRange(startDate, endDate).Where(t => !t.IsCancelled).ToList();
            
            var dt = new DataTable();
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("النوع");
            dt.Columns.Add("الفئة");
            dt.Columns.Add("المبلغ");
            dt.Columns.Add("الوصف");
            dt.Columns.Add("العضو");
            dt.Columns.Add("المستخدم");

            foreach (var t in transactions.OrderBy(t => t.TransactionDate))
            {
                var typeText = t.TransactionType switch
                {
                    TransactionType.Deposit => "إيداع",
                    TransactionType.Withdrawal => "سحب",
                    TransactionType.Expense => "مصروف",
                    _ => "أخرى"
                };

                var categoryText = t.Category switch
                {
                    VaultTransactionCategory.WeeklyReconciliation => "ترحيل جرد أسبوعي",
                    VaultTransactionCategory.MemberWithdrawal => "سحب لعضو",
                    VaultTransactionCategory.BehindAssociationWithdrawal => "سحب خلف الجمعية",
                    VaultTransactionCategory.ManagerWithdrawals => "خرجيات المدير",
                    VaultTransactionCategory.AssociationDebt => "ديون الجمعية",
                    VaultTransactionCategory.Missing => "مفقود",
                    VaultTransactionCategory.MemberDeposit => "إيداع من عضو",
                    VaultTransactionCategory.OperatingExpense => "مصروف تشغيلي",
                    _ => "أخرى"
                };

                dt.Rows.Add(
                    t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                    typeText,
                    categoryText,
                    $"{t.Amount:N2}",
                    t.Description ?? "-",
                    t.MemberName ?? "-",
                    t.UserName ?? "-"
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير المدفوعات الخارجية
        /// </summary>
        public DataTable GenerateExternalPaymentsReport(DateTime startDate, DateTime endDate)
        {
            var payments = _externalPaymentRepository.GetByDateRange(startDate, endDate);
            
            var dt = new DataTable();
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("العضو");
            dt.Columns.Add("رقم المرجع");
            dt.Columns.Add("المبلغ");
            dt.Columns.Add("المصدر");
            dt.Columns.Add("الحالة");
            dt.Columns.Add("الملاحظات");

            foreach (var p in payments.OrderBy(p => p.PaymentDate))
            {
                var sourceText = p.PaymentSource switch
                {
                    PaymentSource.Karimi => "كريمي",
                    PaymentSource.BankTransfer => "تحويل بنكي",
                    _ => "أخرى"
                };

                var statusText = p.Status switch
                {
                    ExternalPaymentStatus.Pending => "معلق",
                    ExternalPaymentStatus.Matched => "مطابق",
                    ExternalPaymentStatus.Unmatched => "غير مطابق",
                    _ => "غير معروف"
                };

                dt.Rows.Add(
                    p.PaymentDate.ToString("yyyy-MM-dd"),
                    p.MemberName ?? "-",
                    p.ReferenceNumber,
                    $"{p.Amount:N2}",
                    sourceText,
                    statusText,
                    p.Notes ?? "-"
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير الجرد الأسبوعي - بشكل جدول منظم
        /// </summary>
        public DataTable GenerateWeeklyReconciliationDetailReport(int weekNumber)
        {
            var reconciliation = _reconciliationRepository.GetByWeekNumber(weekNumber);
            if (reconciliation == null)
                throw new Exception("الجرد غير موجود");

            var dt = new DataTable();
            dt.Columns.Add("التصنيف");
            dt.Columns.Add("البند");
            dt.Columns.Add("القيمة");
            dt.Columns.Add("التفاصيل");
            dt.Columns.Add("الحالة");

            // معلومات الأسبوع
            dt.Rows.Add("📅 الفترة", "رقم الأسبوع", weekNumber.ToString(), 
                $"{reconciliation.WeekStartDate:yyyy-MM-dd} - {reconciliation.WeekEndDate:yyyy-MM-dd}", "");

            // المبالغ
            var differenceStatus = reconciliation.Difference == 0 ? "✅ متطابق" : 
                (reconciliation.Difference > 0 ? "📈 زائد" : "📉 ناقص");
            
            dt.Rows.Add("💰 المبالغ", "المبلغ المتوقع", $"{reconciliation.ExpectedAmount:N2} ريال", 
                "المبلغ المحسوب من النظام", "");
            dt.Rows.Add("💰 المبالغ", "المبلغ الفعلي", $"{reconciliation.ActualAmount:N2} ريال", 
                "المبلغ الموجود في الخزنة", "");
            dt.Rows.Add("💰 المبالغ", "الفرق", $"{Math.Abs(reconciliation.Difference):N2} ريال", 
                reconciliation.Difference >= 0 ? "زيادة في الخزنة" : "نقص في الخزنة", differenceStatus);

            // معلومات الجرد
            var statusText = reconciliation.Status == ReconciliationStatus.Completed ? "✅ مكتمل" : "⏳ معلق";
            dt.Rows.Add("📊 معلومات الجرد", "تاريخ الجرد", reconciliation.ReconciliationDate.ToString("yyyy-MM-dd HH:mm"), 
                $"تم بواسطة: {reconciliation.UserName ?? "غير معروف"}", statusText);
            
            if (!string.IsNullOrEmpty(reconciliation.Notes))
                dt.Rows.Add("📝 ملاحظات", "الملاحظات", reconciliation.Notes, "", "");

            // تفاصيل التحصيلات في هذا الأسبوع
            var weekCollections = _collectionRepository.GetByDateRange(reconciliation.WeekStartDate, reconciliation.WeekEndDate)
                .Where(c => !c.IsCancelled).ToList();
            
            dt.Rows.Add("📈 الإحصائيات", "عدد التحصيلات", weekCollections.Count.ToString(), 
                $"إجمالي: {weekCollections.Sum(c => c.AmountPaid):N2} ريال", "");

            return dt;
        }

        /// <summary>
        /// تقرير الأداء الشهري - بشكل جدول منظم
        /// </summary>
        public DataTable GenerateMonthlyPerformanceReport(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            var collections = _collectionRepository.GetByDateRange(startDate, endDate).Where(c => !c.IsCancelled).ToList();
            
            var dt = new DataTable();
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("اليوم");
            dt.Columns.Add("عدد التحصيلات", typeof(int));
            dt.Columns.Add("المبلغ الإجمالي", typeof(decimal));
            dt.Columns.Add("متوسط التحصيل", typeof(decimal));
            dt.Columns.Add("نقدي", typeof(decimal));
            dt.Columns.Add("إلكتروني", typeof(decimal));
            dt.Columns.Add("الأداء");

            var totalMonthAmount = collections.Sum(c => c.AmountPaid);
            var dailyGroups = collections.GroupBy(c => c.CollectionDate.Date).OrderBy(g => g.Key);
            
            foreach (var group in dailyGroups)
            {
                var total = group.Sum(c => c.AmountPaid);
                var count = group.Count();
                var average = count > 0 ? total / count : 0;
                var cashAmount = group.Where(c => c.PaymentType == PaymentType.Cash).Sum(c => c.AmountPaid);
                var electronicAmount = group.Where(c => c.PaymentType == PaymentType.Electronic).Sum(c => c.AmountPaid);
                var dayName = group.Key.ToString("dddd", new System.Globalization.CultureInfo("ar-SA"));
                
                // تقييم الأداء
                var performance = total >= (totalMonthAmount / dailyGroups.Count()) ? "⭐ جيد" : 
                    total >= (totalMonthAmount / dailyGroups.Count() * 0.7m) ? "✅ متوسط" : "⚠️ ضعيف";

                dt.Rows.Add(
                    group.Key.ToString("yyyy-MM-dd"),
                    dayName,
                    count,
                    total,
                    average,
                    cashAmount,
                    electronicAmount,
                    performance
                );
            }

            // إضافة صف الإجماليات
            if (dailyGroups.Any())
            {
                var totalCount = collections.Count;
                var totalAmount = collections.Sum(c => c.AmountPaid);
                var totalCash = collections.Where(c => c.PaymentType == PaymentType.Cash).Sum(c => c.AmountPaid);
                var totalElectronic = collections.Where(c => c.PaymentType == PaymentType.Electronic).Sum(c => c.AmountPaid);
                var avgPerDay = totalAmount / dailyGroups.Count();

                dt.Rows.Add(
                    "الإجماليات",
                    $"{dailyGroups.Count()} يوم",
                    totalCount,
                    totalAmount,
                    avgPerDay,
                    totalCash,
                    totalElectronic,
                    "📊"
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير أفضل الأعضاء أداءً - بشكل جدول منظم
        /// </summary>
        public DataTable GenerateTopPerformersReport(DateTime startDate, DateTime endDate, int topCount = 10)
        {
            var collections = _collectionRepository.GetByDateRange(startDate, endDate).Where(c => !c.IsCancelled).ToList();
            var totalAmount = collections.Sum(c => c.AmountPaid);
            
            var memberPerformance = collections.GroupBy(c => new { c.MemberName })
                .Select(g => new
                {
                    MemberName = g.Key.MemberName,
                    TotalCollected = g.Sum(c => c.AmountPaid),
                    CollectionsCount = g.Count(),
                    AverageCollection = g.Average(c => c.AmountPaid),
                    LastCollection = g.Max(c => c.CollectionDate)
                })
                .OrderByDescending(x => x.TotalCollected)
                .Take(topCount);

            var dt = new DataTable();
            dt.Columns.Add("الترتيب");
            dt.Columns.Add("اسم العضو");
            dt.Columns.Add("المبلغ الإجمالي", typeof(decimal));
            dt.Columns.Add("عدد التحصيلات", typeof(int));
            dt.Columns.Add("متوسط التحصيل", typeof(decimal));
            dt.Columns.Add("النسبة من الإجمالي");
            dt.Columns.Add("آخر تحصيل");
            dt.Columns.Add("التقييم");

            int rank = 1;
            foreach (var item in memberPerformance)
            {
                var rankIcon = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : $"{rank}.";
                var percentage = totalAmount > 0 ? (item.TotalCollected / totalAmount) * 100 : 0;
                var rating = item.CollectionsCount >= 20 ? "⭐⭐⭐ ممتاز" : 
                    item.CollectionsCount >= 10 ? "⭐⭐ جيد جداً" : 
                    item.CollectionsCount >= 5 ? "⭐ جيد" : "✅ متوسط";

                dt.Rows.Add(
                    rankIcon,
                    item.MemberName,
                    item.TotalCollected,
                    item.CollectionsCount,
                    item.AverageCollection,
                    $"{percentage:F1}%",
                    item.LastCollection.ToString("yyyy-MM-dd"),
                    rating
                );
                rank++;
            }

            return dt;
        }

        /// <summary>
        /// تقرير شامل لأعضاء خلف الجمعية
        /// </summary>
        public DataTable GenerateBehindAssociationReport()
        {
            var behindAssociationRepo = new Data.Repositories.BehindAssociation.BehindAssociationRepository();
            var summaries = behindAssociationRepo.GetAllMembersSummaries();
            
            var dt = new DataTable();
            dt.Columns.Add("اسم العضو");
            dt.Columns.Add("الهاتف");
            dt.Columns.Add("إجمالي الإيداعات", typeof(decimal));
            dt.Columns.Add("إجمالي السحوبات", typeof(decimal));
            dt.Columns.Add("الرصيد الحالي", typeof(decimal));
            dt.Columns.Add("عدد المعاملات", typeof(int));
            dt.Columns.Add("آخر إيداع");
            dt.Columns.Add("مبلغ آخر إيداع", typeof(decimal));
            dt.Columns.Add("الحالة");

            foreach (var summary in summaries.OrderByDescending(s => s.CurrentBalance))
            {
                var status = summary.CurrentBalance > 0 ? "✅ متاح" : 
                             summary.CurrentBalance < 0 ? "⚠️ سالب" : "⚪ صفر";
                
                dt.Rows.Add(
                    summary.MemberName,
                    summary.Phone ?? "-",
                    summary.TotalDeposits,
                    summary.TotalWithdrawals,
                    summary.CurrentBalance,
                    summary.TransactionCount,
                    summary.LastDepositDate?.ToString("yyyy-MM-dd") ?? "-",
                    summary.LastDepositAmount,
                    status
                );
            }

            // إضافة صف الإجماليات
            if (summaries.Any())
            {
                dt.Rows.Add(
                    "📊 الإجمالي",
                    "-",
                    summaries.Sum(s => s.TotalDeposits),
                    summaries.Sum(s => s.TotalWithdrawals),
                    summaries.Sum(s => s.CurrentBalance),
                    summaries.Sum(s => s.TransactionCount),
                    "-",
                    0,
                    summaries.Count(s => s.CurrentBalance > 0) + " عضو لديهم رصيد"
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير تفصيلي لعضو خلف الجمعية
        /// </summary>
        public DataTable GenerateBehindAssociationMemberReport(int memberId)
        {
            var behindAssociationRepo = new Data.Repositories.BehindAssociation.BehindAssociationRepository();
            var transactions = behindAssociationRepo.GetMemberTransactions(memberId);
            var member = _memberRepository.GetById(memberId);
            var summary = behindAssociationRepo.GetMemberSummary(memberId);
            
            var dt = new DataTable();
            
            // ✅ إضافة معلومات العضو كخصائص
            dt.TableName = $"تقرير خلف الجمعية - {member?.Name ?? "غير معروف"}";
            dt.ExtendedProperties["MemberName"] = member?.Name ?? "غير معروف";
            dt.ExtendedProperties["MemberPhone"] = member?.Phone ?? "-";
            dt.ExtendedProperties["TotalDeposits"] = summary?.TotalDeposits.ToString("N2") ?? "0";
            dt.ExtendedProperties["TotalWithdrawals"] = summary?.TotalWithdrawals.ToString("N2") ?? "0";
            dt.ExtendedProperties["CurrentBalance"] = summary?.CurrentBalance.ToString("N2") ?? "0";
            
            // ✅ أعمدة مبسطة
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("النوع");
            dt.Columns.Add("المبلغ", typeof(decimal));
            dt.Columns.Add("الرصيد المتراكم", typeof(decimal));
            dt.Columns.Add("الأسبوع", typeof(int));
            dt.Columns.Add("اليوم", typeof(int));
            dt.Columns.Add("الملاحظات");

            decimal runningBalance = 0;
            foreach (var t in transactions.OrderBy(t => t.TransactionDate))
            {
                var type = t.TransactionType == Models.BehindAssociation.BehindAssociationTransactionType.Deposit 
                    ? "💰 إيداع" : "💸 سحب";
                
                if (t.TransactionType == Models.BehindAssociation.BehindAssociationTransactionType.Deposit)
                    runningBalance += t.Amount;
                else
                    runningBalance -= t.Amount;
                
                dt.Rows.Add(
                    t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                    type,
                    t.Amount,
                    runningBalance,
                    t.WeekNumber,
                    t.DayNumber,
                    t.Notes ?? "-"
                );
            }

            return dt;
        }
    }
}
