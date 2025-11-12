using System;

namespace Alwajeih.Models.BehindAssociation
{
    /// <summary>
    /// نموذج معاملات أعضاء "خلف الجمعية"
    /// هؤلاء الأعضاء يدفعون ويسحبون متى أرادوا (نظام أمانة)
    /// لا يخضعون لنظام المتأخرات أو السابقات
    /// </summary>
    public class BehindAssociationTransaction
    {
        public int TransactionID { get; set; }
        
        /// <summary>
        /// معرف العضو (من جدول Members حيث MemberType = BehindAssociation)
        /// </summary>
        public int MemberID { get; set; }
        
        /// <summary>
        /// نوع المعاملة - دفع فقط (السحب يتم من الخزنة)
        /// </summary>
        public BehindAssociationTransactionType TransactionType { get; set; }
        
        /// <summary>
        /// المبلغ المدفوع
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// تاريخ المعاملة
        /// </summary>
        public DateTime TransactionDate { get; set; }
        
        /// <summary>
        /// رقم الأسبوع (لأغراض الجرد والإحصائيات فقط)
        /// </summary>
        public int WeekNumber { get; set; }
        
        /// <summary>
        /// رقم اليوم (لأغراض الإحصائيات فقط)
        /// </summary>
        public int DayNumber { get; set; }
        
        /// <summary>
        /// مصدر الدفع
        /// </summary>
        public PaymentSource PaymentSource { get; set; }
        
        /// <summary>
        /// الرقم المرجعي (للدفعات الإلكترونية)
        /// </summary>
        public string? ReferenceNumber { get; set; }
        
        /// <summary>
        /// ملاحظات
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// المستخدم الذي سجل المعاملة
        /// </summary>
        public int RecordedBy { get; set; }
        
        /// <summary>
        /// تاريخ التسجيل
        /// </summary>
        public DateTime RecordedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// هل المعاملة ملغاة
        /// </summary>
        public bool IsCancelled { get; set; } = false;
        
        /// <summary>
        /// سبب الإلغاء
        /// </summary>
        public string? CancellationReason { get; set; }
        
        // خصائص إضافية للعرض
        public string? MemberName { get; set; }
        public string TransactionTypeDisplay => TransactionType == BehindAssociationTransactionType.Deposit ? "دفع" : "سحب";
        public string PaymentSourceDisplay => PaymentSource.ToString();
    }
    
    /// <summary>
    /// نوع معاملة خلف الجمعية
    /// </summary>
    public enum BehindAssociationTransactionType
    {
        /// <summary>
        /// دفع/إيداع - يُسجل هنا
        /// </summary>
        Deposit,
        
        /// <summary>
        /// سحب - يُسجل في الخزنة فقط (للرجوع والإحصاءات)
        /// </summary>
        Withdrawal
    }
}
