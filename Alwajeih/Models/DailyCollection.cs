using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج التحصيل اليومي
    /// </summary>
    public class DailyCollection
    {
        public int CollectionID { get; set; }
        public int PlanID { get; set; }
        public DateTime CollectionDate { get; set; }
        public int WeekNumber { get; set; } // 1-26
        public int DayNumber { get; set; } // 1-7 (السبت=1, الأحد=2, ... الجمعة=7)
        public decimal AmountPaid { get; set; } // المبلغ المدفوع للتحصيل العادي
        public PaymentType PaymentType { get; set; }
        public PaymentSource PaymentSource { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Notes { get; set; }
        public int CollectedBy { get; set; }
        public DateTime CollectedAt { get; set; } = DateTime.Now;
        public bool IsCancelled { get; set; } = false;
        public string? CancellationReason { get; set; }
        
        // خصائص إضافية للعرض
        public string? MemberName { get; set; }
        public int PlanNumber { get; set; }
        public decimal DailyAmount { get; set; }
        public string? DayName { get; set; } // اسم اليوم بالعربي
        public string WeekDayDisplay => $"{DayName} - الأسبوع {WeekNumber}"; // عرض مركب
        public string? PaymentTypeDescription { get; set; } // وصف نوع الدفع (للمتأخرات والمستحقات)
    }
}
