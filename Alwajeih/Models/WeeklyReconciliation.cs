using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج الجرد الأسبوعي
    /// </summary>
    public class WeeklyReconciliation
    {
        public int ReconciliationID { get; set; }
        public int WeekNumber { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Difference { get; set; }
        public string? Notes { get; set; }
        public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Pending;
        public int PerformedBy { get; set; }
        public DateTime PerformedDate { get; set; } = DateTime.Now;
        public DateTime ReconciliationDate { get; set; } = DateTime.Now;
        
        // خصائص إضافية للعرض
        public string? UserName { get; set; }
    }
}
