using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// عنصر المتأخرات
    /// </summary>
    public class ArrearItem
    {
        public int PlanID { get; set; }
        public string MemberName { get; set; }
        public int WeekNumber { get; set; }
        public int DayNumber { get; set; }
        public string DayName { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ArrearAmount => ExpectedAmount - PaidAmount;
        public DateTime Date { get; set; }
    }
}
