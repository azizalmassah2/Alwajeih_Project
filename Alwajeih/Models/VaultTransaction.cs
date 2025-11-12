using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج معاملة الخزنة
    /// </summary>
    public class VaultTransaction
    {
        public int TransactionID { get; set; }
        public TransactionType TransactionType { get; set; }
        public VaultTransactionCategory Category { get; set; }  // تصنيف فرعي دقيق للمعاملة
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        
        // الربط بالكيانات الأخرى
        public int? RelatedReconciliationID { get; set; }  // ربط بالجرد الأسبوعي
        public int? RelatedMemberID { get; set; }          // ربط بالعضو (عند سحب/إيداع لعضو)
        public int? RelatedPlanID { get; set; }            // ربط بالحصة (اختياري)
        
        public int PerformedBy { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.Now;
        public bool IsCancelled { get; set; } = false;
        public string? CancellationReason { get; set; }
        
        // خصائص إضافية للعرض
        public string? UserName { get; set; }
        public string? MemberName { get; set; }  // اسم العضو المرتبط
        
        /// <summary>
        /// نوع المعاملة بالعربي
        /// </summary>
        public string TransactionTypeArabic
        {
            get
            {
                return TransactionType switch
                {
                    TransactionType.Deposit => "إيداع",
                    TransactionType.Withdrawal => "سحب",
                    _ => TransactionType.ToString()
                };
            }
        }
    }
}
