using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// الخرجيات والمفقودات والمصروفات الأخرى
    /// </summary>
    public class OtherTransaction
    {
        public int TransactionID { get; set; }
        
        /// <summary>
        /// نوع العملية: خرجية، مفقود، مصروف، أخرى
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;
        
        /// <summary>
        /// المبلغ
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// رقم السهم (اختياري)
        /// </summary>
        public int? PlanID { get; set; }
        
        /// <summary>
        /// اسم العضو (للعرض)
        /// </summary>
        public string? MemberName { get; set; }
        
        /// <summary>
        /// رقم الأسبوع
        /// </summary>
        public int WeekNumber { get; set; }
        
        /// <summary>
        /// رقم اليوم
        /// </summary>
        public int DayNumber { get; set; }
        
        /// <summary>
        /// تاريخ العملية
        /// </summary>
        public DateTime TransactionDate { get; set; }
        
        /// <summary>
        /// الملاحظات
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// المستخدم الذي سجل العملية
        /// </summary>
        public int CreatedBy { get; set; }
        
        /// <summary>
        /// تاريخ التسجيل
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// حالة الإلغاء
        /// </summary>
        public bool IsCancelled { get; set; }
        
        /// <summary>
        /// سبب الإلغاء
        /// </summary>
        public string? CancellationReason { get; set; }
    }
    
    /// <summary>
    /// أنواع العمليات الأخرى
    /// </summary>
    public enum OtherTransactionType
    {
        /// <summary>
        /// خرجية - عضو خرج من الجمعية
        /// </summary>
        Exit,
        
        /// <summary>
        /// مفقود - عضو مفقود
        /// </summary>
        Lost,
        
        /// <summary>
        /// مصروف - مصروف عام
        /// </summary>
        Expense,
        
        /// <summary>
        /// أخرى
        /// </summary>
        Other
    }
}
