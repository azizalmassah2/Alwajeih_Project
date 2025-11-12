using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// جدول تسجيل مدفوعات السابقات المتراكمة
    /// يستخدم لكشف حساب العضو ومعرفة متى وكم دفع من السابقات
    /// </summary>
    public class AccumulatedArrearPayment
    {
        public int PaymentID { get; set; }
        public int PlanID { get; set; }
        
        /// <summary>
        /// رقم الأسبوع الذي تم فيه الدفع
        /// </summary>
        public int WeekNumber { get; set; }
        
        /// <summary>
        /// رقم اليوم الذي تم فيه الدفع (1-7)
        /// </summary>
        public int DayNumber { get; set; }
        
        /// <summary>
        /// المبلغ المدفوع
        /// </summary>
        public decimal AmountPaid { get; set; }
        
        /// <summary>
        /// تاريخ الدفع
        /// </summary>
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// من قام بالتسجيل
        /// </summary>
        public int RecordedBy { get; set; }
        
        /// <summary>
        /// ملاحظات
        /// </summary>
        public string? Notes { get; set; }
        
        // خصائص إضافية للعرض
        public string? MemberName { get; set; }
        public int PlanNumber { get; set; }
    }
}
