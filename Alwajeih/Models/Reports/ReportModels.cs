using System;
using System.Collections.Generic;

namespace Alwajeih.Models.Reports
{
    /// <summary>
    /// نموذج تقرير مالي شامل
    /// </summary>
    public class FinancialReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetBalance { get; set; }
        public decimal VaultBalance { get; set; }
        public int TotalCollections { get; set; }
        public int TotalMembers { get; set; }
        public int ActivePlans { get; set; }
        public decimal TotalArrears { get; set; }
        public decimal TotalPreviousArrears { get; set; }
        public List<DailyFinancialSummary> DailySummaries { get; set; } = new();
    }

    /// <summary>
    /// ملخص مالي يومي
    /// </summary>
    public class DailyFinancialSummary
    {
        public DateTime Date { get; set; }
        public decimal DailyIncome { get; set; }
        public decimal DailyExpenses { get; set; }
        public decimal DailyNet { get; set; }
        public int CollectionsCount { get; set; }
    }

    /// <summary>
    /// تقرير تفصيلي للعضو
    /// </summary>
    public class MemberDetailReport
    {
        public int MemberID { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public MemberType MemberType { get; set; }
        public DateTime JoinDate { get; set; }
        public List<PlanSummary> Plans { get; set; } = new();
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
        public decimal TotalArrears { get; set; }
        public int TotalCollections { get; set; }
        public decimal AveragePayment { get; set; }
        public double CompletionPercentage { get; set; }
    }

    /// <summary>
    /// ملخص الحصة
    /// </summary>
    public class PlanSummary
    {
        public int PlanID { get; set; }
        public int PlanNumber { get; set; }
        public decimal DailyAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public double ProgressPercentage { get; set; }
        public PlanStatus Status { get; set; }
        public int DaysElapsed { get; set; }
        public int DaysRemaining { get; set; }
        public int TotalCollections { get; set; }
        public int MissedCollections { get; set; }
        public decimal TotalArrears { get; set; }
    }

    /// <summary>
    /// تقرير التحصيلات المفصل
    /// </summary>
    public class CollectionDetailReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalCollected { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AveragePerTransaction { get; set; }
        public decimal CashAmount { get; set; }
        public decimal ElectronicAmount { get; set; }
        public List<CollectionBySource> BySource { get; set; } = new();
        public List<CollectionByDay> ByDay { get; set; } = new();
        public List<CollectionByMember> TopMembers { get; set; } = new();
    }

    /// <summary>
    /// التحصيل حسب المصدر
    /// </summary>
    public class CollectionBySource
    {
        public PaymentSource Source { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// التحصيل حسب اليوم
    /// </summary>
    public class CollectionByDay
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// التحصيل حسب العضو
    /// </summary>
    public class CollectionByMember
    {
        public int MemberID { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int CollectionsCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// تقرير المتأخرات الشامل
    /// </summary>
    public class ArrearsComprehensiveReport
    {
        public DateTime ReportDate { get; set; }
        public decimal TotalArrearsAmount { get; set; }
        public int TotalArrearsCount { get; set; }
        public int AffectedMembers { get; set; }
        public decimal TotalPreviousArrears { get; set; }
        public int PreviousArrearsCount { get; set; }
        public List<MemberArrearsDetail> MemberDetails { get; set; } = new();
        public List<ArrearsByWeek> WeeklyBreakdown { get; set; } = new();
        public decimal AverageArrearPerMember { get; set; }
        public int MembersWithNoArrears { get; set; }
        public int MembersWithPartialPayment { get; set; }
    }

    /// <summary>
    /// تفاصيل متأخرات العضو
    /// </summary>
    public class MemberArrearsDetail
    {
        public int MemberID { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public int PlanNumber { get; set; }
        public decimal CurrentArrears { get; set; }
        public decimal PreviousArrears { get; set; }
        public decimal TotalArrears { get; set; }
        public int DaysOverdue { get; set; }
        public int ArrearsCount { get; set; }
        public DateTime? OldestArrearDate { get; set; }
        public DateTime? LatestArrearDate { get; set; }
    }

    /// <summary>
    /// المتأخرات حسب الأسبوع
    /// </summary>
    public class ArrearsByWeek
    {
        public int WeekNumber { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public int MembersCount { get; set; }
    }

    /// <summary>
    /// تقرير الخزنة المفصل
    /// </summary>
    public class VaultDetailedReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal ClosingBalance { get; set; }
        public int TotalTransactions { get; set; }
        public List<VaultTransactionSummary> TransactionsByCategory { get; set; } = new();
        public List<VaultTransactionDetail> Transactions { get; set; } = new();
    }

    /// <summary>
    /// ملخص معاملات الخزنة حسب الفئة
    /// </summary>
    public class VaultTransactionSummary
    {
        public VaultTransactionCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// تفاصيل معاملة الخزنة
    /// </summary>
    public class VaultTransactionDetail
    {
        public int TransactionID { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public VaultTransactionCategory Category { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? MemberName { get; set; }
    }

    /// <summary>
    /// تقرير الجرد الأسبوعي
    /// </summary>
    public class WeeklyReconciliationReport
    {
        public int WeekNumber { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Difference { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalDues { get; set; }
        public decimal TotalArrears { get; set; }
        public decimal ManagerWithdrawals { get; set; }
        public decimal AssociationDebts { get; set; }
        public decimal Missing { get; set; }
        public decimal Graduates { get; set; }
        public List<DailyReconciliationDetail> DailyDetails { get; set; } = new();
    }

    /// <summary>
    /// تفاصيل الجرد اليومي
    /// </summary>
    public class DailyReconciliationDetail
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public decimal Net { get; set; }
        public int CollectionsCount { get; set; }
    }

    /// <summary>
    /// تقرير الأداء والإحصائيات
    /// </summary>
    public class PerformanceReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int InactiveMembers { get; set; }
        public int TotalPlans { get; set; }
        public int ActivePlans { get; set; }
        public int CompletedPlans { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal AverageCollectionPerDay { get; set; }
        public decimal AverageCollectionPerMember { get; set; }
        public double AverageCompletionRate { get; set; }
        public int BestCollectionDay { get; set; }
        public string BestCollectionDayName { get; set; } = string.Empty;
        public decimal BestCollectionDayAmount { get; set; }
        public List<TopPerformer> TopPerformers { get; set; } = new();
        public List<WeeklyPerformance> WeeklyPerformance { get; set; } = new();
    }

    /// <summary>
    /// أفضل الأعضاء أداءً
    /// </summary>
    public class TopPerformer
    {
        public int MemberID { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public decimal TotalCollected { get; set; }
        public int CollectionsCount { get; set; }
        public double CompletionRate { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// الأداء الأسبوعي
    /// </summary>
    public class WeeklyPerformance
    {
        public int WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalCollected { get; set; }
        public int CollectionsCount { get; set; }
        public int ActiveMembers { get; set; }
        public double AveragePerMember { get; set; }
    }

    /// <summary>
    /// تقرير المدفوعات الخارجية
    /// </summary>
    public class ExternalPaymentReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalPayments { get; set; }
        public int MatchedPayments { get; set; }
        public int UnmatchedPayments { get; set; }
        public int PendingPayments { get; set; }
        public decimal MatchedAmount { get; set; }
        public decimal UnmatchedAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public List<ExternalPaymentDetail> Payments { get; set; } = new();
        public List<PaymentBySource> BySource { get; set; } = new();
    }

    /// <summary>
    /// تفاصيل الدفعة الخارجية
    /// </summary>
    public class ExternalPaymentDetail
    {
        public int ExternalPaymentID { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentSource PaymentSource { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public ExternalPaymentStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    /// <summary>
    /// الدفعات حسب المصدر
    /// </summary>
    public class PaymentBySource
    {
        public PaymentSource Source { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// تقرير المقارنة الزمنية
    /// </summary>
    public class ComparisonReport
    {
        public DateTime Period1Start { get; set; }
        public DateTime Period1End { get; set; }
        public DateTime Period2Start { get; set; }
        public DateTime Period2End { get; set; }
        public decimal Period1Income { get; set; }
        public decimal Period2Income { get; set; }
        public decimal IncomeChange { get; set; }
        public double IncomeChangePercentage { get; set; }
        public int Period1Collections { get; set; }
        public int Period2Collections { get; set; }
        public int CollectionsChange { get; set; }
        public double CollectionsChangePercentage { get; set; }
        public decimal Period1Arrears { get; set; }
        public decimal Period2Arrears { get; set; }
        public decimal ArrearsChange { get; set; }
        public double ArrearsChangePercentage { get; set; }
    }
}
