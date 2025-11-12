using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// نموذج العضو
    /// </summary>
    public class Member
    {
        public int MemberID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public MemberType MemberType { get; set; } = MemberType.Regular;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsArchived { get; set; } = false;
        public int CreatedBy { get; set; }
        
        // خاصية إضافية للعرض فقط (لا تخزن في قاعدة البيانات)
        public CollectionFrequency? CollectionFrequency { get; set; }
    }
}
