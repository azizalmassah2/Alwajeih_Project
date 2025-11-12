using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج المتأخرة اليومية - تخص الأسبوع الحالي فقط
    /// </summary>
    public class DailyArrear
    {
        public int ArrearID { get; set; }
        public int PlanID { get; set; }
        public int WeekNumber { get; set; } // الأسبوع الحالي
        public int DayNumber { get; set; } // رقم اليوم في الأسبوع (1-7)
        public DateTime ArrearDate { get; set; }
        public decimal AmountDue { get; set; }
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidDate { get; set; }
        public decimal PaidAmount { get; set; } = 0;
        public decimal RemainingAmount { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // خصائص إضافية للعرض
        public string? MemberName { get; set; }
        public int PlanNumber { get; set; }
        public int DaysOverdue { get; set; }
        public string? DayName { get; set; }
    }
}
