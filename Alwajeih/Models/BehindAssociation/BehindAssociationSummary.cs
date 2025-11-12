using System;

namespace Alwajeih.Models.BehindAssociation
{
    /// <summary>
    /// ملخص حساب عضو "خلف الجمعية"
    /// يعرض الرصيد الحالي والمعاملات
    /// </summary>
    public class BehindAssociationSummary
    {
        public int MemberID { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        
        /// <summary>
        /// إجمالي المبالغ المدفوعة (الإيداعات)
        /// </summary>
        public decimal TotalDeposits { get; set; }
        
        /// <summary>
        /// إجمالي المبالغ المسحوبة (من الخزنة)
        /// </summary>
        public decimal TotalWithdrawals { get; set; }
        
        /// <summary>
        /// الرصيد الحالي = المدفوعات - المسحوبات
        /// </summary>
        public decimal CurrentBalance => TotalDeposits - TotalWithdrawals;
        
        /// <summary>
        /// عدد المعاملات (الدفعات فقط)
        /// </summary>
        public int TransactionCount { get; set; }
        
        /// <summary>
        /// آخر دفعة
        /// </summary>
        public DateTime? LastDepositDate { get; set; }
        
        /// <summary>
        /// مبلغ آخر دفعة
        /// </summary>
        public decimal? LastDepositAmount { get; set; }
        
        /// <summary>
        /// آخر سحب
        /// </summary>
        public DateTime? LastWithdrawalDate { get; set; }
        
        /// <summary>
        /// مبلغ آخر سحب
        /// </summary>
        public decimal? LastWithdrawalAmount { get; set; }
        
        /// <summary>
        /// تاريخ أول معاملة
        /// </summary>
        public DateTime? FirstTransactionDate { get; set; }
        
        /// <summary>
        /// عرض الرصيد بشكل منسق
        /// </summary>
        public string BalanceDisplay => $"{CurrentBalance:N2} ريال";
        
        /// <summary>
        /// حالة الرصيد (موجب/سالب/صفر)
        /// </summary>
        public string BalanceStatus
        {
            get
            {
                if (CurrentBalance > 0) return "موجب";
                if (CurrentBalance < 0) return "سالب";
                return "صفر";
            }
        }
        
        /// <summary>
        /// لون الرصيد للعرض
        /// </summary>
        public string BalanceColor
        {
            get
            {
                if (CurrentBalance > 0) return "#10B981"; // أخضر
                if (CurrentBalance < 0) return "#EF4444"; // أحمر
                return "#64748B"; // رمادي
            }
        }
    }
}
