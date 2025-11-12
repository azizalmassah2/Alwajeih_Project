using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// جدول السابقات المتراكمة - سجل واحد لكل عضو
    /// يحتوي على إجمالي السابقات من جميع الأسابيع بدون تكرار
    /// </summary>
    public class AccumulatedArrears
    {
        public int AccumulatedArrearID { get; set; }
        public int PlanID { get; set; }
        
        /// <summary>
        /// آخر رقم أسبوع تم ترحيل السابقات منه
        /// </summary>
        public int LastWeekNumber { get; set; }
        
        /// <summary>
        /// المبلغ الإجمالي المتراكم من جميع الأسابيع
        /// </summary>
        public decimal TotalArrears { get; set; } = 0;
        
        /// <summary>
        /// المبلغ المدفوع من الإجمالي
        /// </summary>
        public decimal PaidAmount { get; set; } = 0;
        
        /// <summary>
        /// المبلغ المتبقي
        /// </summary>
        public decimal RemainingAmount { get; set; } = 0;
        
        /// <summary>
        /// هل تم السداد بالكامل
        /// </summary>
        public bool IsPaid { get; set; } = false;
        
        /// <summary>
        /// تاريخ الإنشاء
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// آخر تحديث
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        // خصائص إضافية للعرض
        public string? MemberName { get; set; }
        public int PlanNumber { get; set; }
    }
}
