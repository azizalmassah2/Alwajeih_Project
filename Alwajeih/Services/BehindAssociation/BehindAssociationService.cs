using System;
using System.Collections.Generic;
using System.Linq;
using Alwajeih.Data.Repositories;
using Alwajeih.Data.Repositories.BehindAssociation;
using Alwajeih.Models;
using Alwajeih.Models.BehindAssociation;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services.BehindAssociation
{
    /// <summary>
    /// خدمة إدارة أعضاء "خلف الجمعية"
    /// هؤلاء الأعضاء لا يخضعون لنظام المتأخرات أو السابقات
    /// يدفعون ويسحبون متى أرادوا (نظام أمانة)
    /// </summary>
    public class BehindAssociationService
    {
        private readonly BehindAssociationRepository _repository;
        private readonly AuditRepository _auditRepository;
        private readonly SystemSettingsRepository _settingsRepository;
        
        public BehindAssociationService()
        {
            _repository = new BehindAssociationRepository();
            _auditRepository = new AuditRepository();
            _settingsRepository = new SystemSettingsRepository();
        }
        
        /// <summary>
        /// تسجيل دفعة لعضو خلف الجمعية
        /// </summary>
        public (bool Success, string Message, int TransactionId) RecordDeposit(
            int memberId,
            decimal amount,
            PaymentSource paymentSource,
            string? referenceNumber,
            string? notes,
            int userId)
        {
            try
            {
                if (amount <= 0)
                    return (false, "المبلغ يجب أن يكون أكبر من صفر", 0);
                
                // التحقق من أن العضو من نوع خلف الجمعية
                var memberRepo = new MemberRepository();
                var member = memberRepo.GetById(memberId);
                
                if (member == null)
                    return (false, "العضو غير موجود", 0);
                
                if (member.MemberType != MemberType.BehindAssociation)
                    return (false, "هذا العضو ليس من أعضاء خلف الجمعية", 0);
                
                // تحميل تاريخ البداية من الإعدادات
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    WeekHelper.StartDate = settings.StartDate;
                }
                
                // حساب رقم الأسبوع واليوم (لأغراض الإحصائيات والجرد فقط)
                var (weekNumber, dayNumber) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
                
                // إنشاء المعاملة
                var transaction = new BehindAssociationTransaction
                {
                    MemberID = memberId,
                    TransactionType = BehindAssociationTransactionType.Deposit,
                    Amount = amount,
                    TransactionDate = DateTime.Now,
                    WeekNumber = weekNumber,
                    DayNumber = dayNumber,
                    PaymentSource = paymentSource,
                    ReferenceNumber = referenceNumber,
                    Notes = notes,
                    RecordedBy = userId,
                    RecordedAt = DateTime.Now
                };
                
                int transactionId = _repository.AddTransaction(transaction);
                
                if (transactionId > 0)
                {
                    // تسجيل في Audit Log
                    _auditRepository.Add(new AuditLog
                    {
                        UserID = userId,
                        Action = AuditAction.Create,
                        EntityType = EntityType.Member, // يمكن إضافة نوع جديد لاحقاً
                        EntityID = transactionId,
                        Details = $"تسجيل دفعة لعضو خلف الجمعية: {member.Name} - المبلغ: {amount:N2} ريال - المصدر: {paymentSource}",
                        Reason = notes
                    });
                    
                    return (true, $"تم تسجيل الدفعة بنجاح\nالمبلغ: {amount:N2} ريال", transactionId);
                }
                
                return (false, "فشل حفظ المعاملة", 0);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}", 0);
            }
        }
        
        /// <summary>
        /// الحصول على ملخص حساب عضو
        /// </summary>
        public BehindAssociationSummary GetMemberSummary(int memberId)
        {
            return _repository.GetMemberSummary(memberId);
        }
        
        /// <summary>
        /// الحصول على جميع ملخصات الأعضاء
        /// </summary>
        public List<BehindAssociationSummary> GetAllMembersSummaries()
        {
            return _repository.GetAllMembersSummaries();
        }
        
        /// <summary>
        /// الحصول على معاملات عضو
        /// </summary>
        public List<BehindAssociationTransaction> GetMemberTransactions(int memberId)
        {
            return _repository.GetMemberTransactions(memberId);
        }
        
        /// <summary>
        /// الحصول على جميع المعاملات
        /// </summary>
        public List<BehindAssociationTransaction> GetAllTransactions()
        {
            return _repository.GetAllTransactions();
        }
        
        /// <summary>
        /// الحصول على معاملات أسبوع معين (لأغراض الجرد)
        /// </summary>
        public List<BehindAssociationTransaction> GetWeekTransactions(int weekNumber)
        {
            return _repository.GetWeekTransactions(weekNumber);
        }
        
        /// <summary>
        /// الحصول على معاملات يوم معين (لأغراض الملخص اليومي)
        /// </summary>
        public List<BehindAssociationTransaction> GetDayTransactions(int weekNumber, int dayNumber)
        {
            return _repository.GetDayTransactions(weekNumber, dayNumber);
        }
        
        /// <summary>
        /// حساب إجمالي دفعات أسبوع معين (لإضافتها للجرد الأسبوعي)
        /// </summary>
        public decimal GetWeekTotalDeposits(int weekNumber)
        {
            return _repository.GetWeekTotalDeposits(weekNumber);
        }
        
        /// <summary>
        /// حساب إجمالي دفعات يوم معين (لإضافتها للملخص اليومي)
        /// </summary>
        public decimal GetDayTotalDeposits(int weekNumber, int dayNumber)
        {
            return _repository.GetDayTotalDeposits(weekNumber, dayNumber);
        }
        
        /// <summary>
        /// حساب إجمالي رصيد جميع أعضاء خلف الجمعية (ديون الجمعية)
        /// </summary>
        public decimal GetTotalBalance()
        {
            return _repository.GetTotalBalance();
        }
        
        /// <summary>
        /// إلغاء معاملة
        /// </summary>
        public (bool Success, string Message) CancelTransaction(int transactionId, string reason, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                    return (false, "يجب إدخال سبب الإلغاء");
                
                bool cancelled = _repository.CancelTransaction(transactionId, reason, userId);
                
                if (cancelled)
                {
                    // تسجيل في Audit Log
                    _auditRepository.Add(new AuditLog
                    {
                        UserID = userId,
                        Action = AuditAction.Cancel,
                        EntityType = EntityType.Member,
                        EntityID = transactionId,
                        Details = $"إلغاء معاملة خلف الجمعية",
                        Reason = reason
                    });
                    
                    return (true, "تم إلغاء المعاملة بنجاح");
                }
                
                return (false, "فشل إلغاء المعاملة");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }
        
        /// <summary>
        /// تقرير شامل لأعضاء خلف الجمعية
        /// </summary>
        public (decimal TotalDeposits, decimal TotalWithdrawals, decimal TotalBalance, int MembersCount) GetOverallSummary()
        {
            var summaries = GetAllMembersSummaries();
            
            return (
                TotalDeposits: summaries.Sum(s => s.TotalDeposits),
                TotalWithdrawals: summaries.Sum(s => s.TotalWithdrawals),
                TotalBalance: summaries.Sum(s => s.CurrentBalance),
                MembersCount: summaries.Count
            );
        }
    }
}
