namespace Alwajeih.Models
{
    /// <summary>
    /// عنصر رصيد الأسبوع
    /// </summary>
    public class WeeklyBalanceItem
    {
        public int WeekNumber { get; set; }
        public decimal Income { get; set; }
        public decimal Dues { get; set; }
        public decimal Balance => Income - Dues;
        public decimal CumulativeBalance { get; set; }
    }
}
