namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج لعنصر في ملخص اليوم
    /// </summary>
    public class DailySummaryItem
    {
        public int PlanID { get; set; }
        public string MemberName { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount => ExpectedAmount - PaidAmount;
        public bool IsPaid => PaidAmount >= ExpectedAmount;
        public string Status => IsPaid ? "مدفوع" : "غير مدفوع";
        public string PaymentType { get; set; }
        public string Notes { get; set; }
    }
}
