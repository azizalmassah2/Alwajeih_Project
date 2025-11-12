using System;
using System.Collections.Generic;

namespace Alwajeih.Models
{
    /// <summary>
    /// ملخص الأسبوع - يحتوي على جميع البنود المالية للأسبوع
    /// </summary>
    public class WeekSummary
    {
        public int WeekNumber { get; set; } // رقم الأسبوع (1-26)
        
        // الواردات (التحصيلات اليومية)
        public decimal TotalIncome { get; set; }
        public int IncomeMembersCount { get; set; }
        
        // الرصيد السابق (من الجرد السابق)
        public decimal PreviousBalance { get; set; }
        public int PreviousWeekNumber { get; set; }
        
        // السابقات المتراكمة (من AccumulatedArrears)
        public decimal TotalPreviousArrears { get; set; }
        
        // سداد السابقات (من DailyCollections)
        public decimal PreviousArrearPayments { get; set; }
        
        // ماعليكم (المستحقات على الأعضاء)
        public decimal TotalDues { get; set; }
        public int DuesMembersCount { get; set; }
        
        // المتأخرات
        public decimal TotalArrears { get; set; }
        public int ArrearsMembersCount { get; set; }
        
        // سحوبات الأعضاء (من الخزنة)
        public decimal MemberWithdrawals { get; set; }
        
        // خرجيات المدير (مصروفات/سحوبات)
        public decimal ManagerWithdrawals { get; set; }
        
        // خلف الجمعية (ديون)
        public decimal AssociationDebts { get; set; }
        
        // الخرجيات (من واجهة الخرجيات)
        public decimal Expenses { get; set; }
        
        // المفقودات (منفصلة)
        public decimal Missing { get; set; }
        
        // إجمالي المعاملات الأخرى
        public decimal TotalOtherTransactions { get; set; }
        
        // خريجات (الأعضاء الذين أنهوا الخطة)
        public decimal Graduates { get; set; }
        public int GraduatesCount { get; set; }
        
        // الرصيد النهائي للأسبوع
        public decimal FinalBalance { get; set; }
        
        // التفاصيل اليومية
        public List<DailySummary> DailyDetails { get; set; } = new List<DailySummary>();
    }
    
    /// <summary>
    /// ملخص يومي - تفاصيل يوم معين في الأسبوع
    /// </summary>
    public class DailySummary
    {
        public int WeekNumber { get; set; }
        public int DayNumber { get; set; } // 1-7 (السبت-الجمعة)
        public string DayName { get; set; } // اسم اليوم بالعربي
        public DateTime Date { get; set; }
        
        // الواردات اليومية
        public decimal DailyIncome { get; set; }
        public int CollectionsCount { get; set; }
        
        // المستحقات
        public decimal DailyDues { get; set; }
        
        // المتأخرات
        public decimal DailyArrears { get; set; }
        
        // خرجيات المدير
        public decimal DailyWithdrawals { get; set; }
        
        // قائمة التحصيلات التفصيلية
        public List<DailyCollection> Collections { get; set; } = new List<DailyCollection>();
    }
    
    /// <summary>
    /// نوع البند في ملخص الأسبوع
    /// </summary>
    public enum WeekSummaryItemType
    {
        Income,              // واردات
        PreviousBalance,     // سابقات
        Dues,                // ماعليكم
        Arrears,             // متأخرات
        ManagerWithdrawals,  // خرجيات المدير
        AssociationDebts,    // خلف الجمعية
        Missing,             // مفقود
        Graduates            // خريجات
    }
}
