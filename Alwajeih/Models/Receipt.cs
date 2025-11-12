using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج الإيصال
    /// </summary>
    public class Receipt
    {
        public int ReceiptID { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int CollectionID { get; set; }
        public DateTime PrintDate { get; set; } = DateTime.Now;
        public int PrintedBy { get; set; }
        
        // خصائص إضافية للعرض
        public string? MemberName { get; set; }
        public int PlanNumber { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal DailyAmount { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal PreviousArrears { get; set; }
        public string? UserName { get; set; }
    }
}
