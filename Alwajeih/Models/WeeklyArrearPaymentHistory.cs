using System;

namespace Alwajeih.Models
{
    /// &lt;summary&gt;
    /// سجل مدفوعات السابقات الأسبوعية
    /// يستخدم لكشف حساب العضو - لتتبع ما دفعه كل عضو من السابقات في كل أسبوع
    /// &lt;/summary&gt;
    public class WeeklyArrearPaymentHistory
    {
        public int HistoryID { get; set; }
        public int PlanID { get; set; }
        public int WeekNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBeforePayment { get; set; }
        public decimal RemainingAfterPayment { get; set; }
        public string? Notes { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
