using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج الحصة (دورة الادخار)
    /// </summary>
    public class SavingPlan
    {
        public int PlanID { get; set; }
        public int MemberID { get; set; }
        public int PlanNumber { get; set; } // 1 أو 2
        public decimal DailyAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public PlanStatus Status { get; set; } = PlanStatus.Active;
        public CollectionFrequency CollectionFrequency { get; set; } = CollectionFrequency.Daily;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; }
        
        // خصائص إضافية للعرض فقط (لا تخزن في قاعدة البيانات)
        public string? MemberName { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public double ProgressPercentage { get; set; }
        
        // نوع التحصيل للعرض
        public string CollectionFrequencyDisplay => CollectionFrequency == CollectionFrequency.Daily ? "يومي" : "أسبوعي";
        
        // خاصية للتحقق من حالة الخطة
        public bool IsActive => Status == PlanStatus.Active;
        
        // أيام التحصيل (افتراضياً كل الأيام)
        public System.Collections.Generic.List<int> CollectionDays { get; set; } = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6, 7 };
        
        // فترة السماح (بالأيام) - للأعضاء الأسبوعيين
        public int GraceDays { get; set; } = 0;
        
        // يوم الدفع المفضل للأعضاء الأسبوعيين (1=أحد, 2=اثنين, ... 7=سبت)
        public int? PreferredPaymentDay { get; set; }
    }
}
