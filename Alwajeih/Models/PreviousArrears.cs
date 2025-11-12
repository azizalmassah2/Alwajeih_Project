using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج السوابق - متأخرات الأسابيع السابقة (أسبوعية)
    /// </summary>
    public class PreviousArrears
    {
        public int PreviousArrearID { get; set; }
        public int PlanID { get; set; }
        public int WeekNumber { get; set; } // الأسبوع السابق
        public decimal TotalArrears { get; set; } = 0; // إجمالي متأخرات هذا الأسبوع
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidDate { get; set; }
        public decimal PaidAmount { get; set; } = 0;
        public decimal RemainingAmount { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        // خصائص إضافية للعرض
        public string? MemberName { get; set; }
        public int PlanNumber { get; set; }
        
        /// <summary>
        /// المبلغ المدفوع المحسوب
        /// </summary>
        public decimal ActualPaidAmount => TotalArrears - RemainingAmount;
        
        /// <summary>
        /// حالة السداد
        /// </summary>
        public string Status => IsPaid ? "✅ مسدد" : RemainingAmount < TotalArrears ? "🔄 جزئي" : "❌ غير مسدد";
        
        /// <summary>
        /// نطاق الأسابيع (للعرض فقط)
        /// </summary>
        public string WeeksRange { get; set; } = string.Empty;
    }
}
