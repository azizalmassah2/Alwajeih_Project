using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج سجل التدقيق
    /// </summary>
    public class AuditLog
    {
        public int AuditID { get; set; }
        public int UserID { get; set; }
        public AuditAction Action { get; set; }
        public EntityType EntityType { get; set; }
        public int? EntityID { get; set; }
        public string? Details { get; set; }
        public string? Reason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? IPAddress { get; set; }
        
        // خصائص إضافية للعرض
        public string? UserName { get; set; }
        
        // خصائص مترجمة للعرض
        public string ActionArabic => Action switch
        {
            AuditAction.Create => "إضافة",
            AuditAction.Update => "تعديل",
            AuditAction.Delete => "حذف",
            AuditAction.Cancel => "إلغاء",
            AuditAction.Login => "تسجيل دخول",
            AuditAction.Logout => "تسجيل خروج",
            _ => Action.ToString()
        };
        
        public string EntityTypeArabic => EntityType switch
        {
            EntityType.Member => "عضو",
            EntityType.SavingPlan => "حصة ادخار",
            EntityType.DailyCollection => "تحصيل يومي",
            EntityType.ExternalPayment => "مدفوعات خارجية",
            EntityType.VaultTransaction => "معاملة خزنة",
            EntityType.WeeklyReconciliation => "جرد أسبوعي",
            EntityType.User => "مستخدم",
            _ => EntityType.ToString()
        };
    }
}
