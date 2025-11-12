using System;

namespace Alwajeih.Models
{
    /// <summary>
    /// إعدادات النظام
    /// </summary>
    public class SystemSettings
    {
        public int SettingID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
    }
}
