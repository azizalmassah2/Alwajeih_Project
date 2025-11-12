using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// سُلفة مُقدمة للعضو من رصيده المستقبلي
    /// </summary>
    public class AdvancePayment
    {
        public int AdvanceID { get; set; }
        public int PlanID { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Description { get; set; }
        public int ApprovedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public string? MemberName { get; set; }
        public int? PlanNumber { get; set; }
        public string? ApprovedByName { get; set; }
    }
}
