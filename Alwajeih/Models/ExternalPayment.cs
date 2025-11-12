using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج المدفوعات الخارجية (كريمي)
    /// </summary>
    public class ExternalPayment
    {
        public int ExternalPaymentID { get; set; }
        public int MemberID { get; set; }  // ⭐ ربط مباشر بالعضو
        public string ReferenceNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentSource PaymentSource { get; set; }
        public ExternalPaymentStatus Status { get; set; } = ExternalPaymentStatus.Pending;
        public int? MatchedWithCollectionID { get; set; }
        public int? MatchedPlanID { get; set; }  // ⭐ للربط بالحصة
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; }
        
        // خصائص إضافية للعرض
        public string? UserName { get; set; }
        public string? MemberName { get; set; }  // ⭐ اسم العضو
        public string? MatchedMemberName { get; set; }
    }
}
