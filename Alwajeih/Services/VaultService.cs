using System;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة الخزنة
    /// </summary>
    public class VaultService
    {
        private readonly VaultRepository _vaultRepository;
        private readonly AuditRepository _auditRepository;

        public VaultService()
        {
            _vaultRepository = new VaultRepository();
            _auditRepository = new AuditRepository();
        }

        /// <summary>
        /// الحصول على الرصيد الحالي
        /// </summary>
        public decimal GetCurrentBalance()
        {
            return _vaultRepository.GetCurrentBalance();
        }

        /// <summary>
        /// إضافة معاملة للخزنة
        /// </summary>
        public (bool Success, string Message) AddTransaction(
            TransactionType transactionType,
            decimal amount,
            DateTime transactionDate,
            string? description,
            int? relatedMemberId,
            int performedBy,
            VaultTransactionCategory category = VaultTransactionCategory.Other,
            int? relatedReconciliationId = null)
        {
            try
            {
                if (amount <= 0)
                    return (false, "المبلغ يجب أن يكون أكبر من صفر");

                var transaction = new VaultTransaction
                {
                    TransactionType = transactionType,
                    Category = category,
                    Amount = amount,
                    TransactionDate = transactionDate,
                    Description = description,
                    RelatedMemberID = relatedMemberId,
                    RelatedReconciliationID = relatedReconciliationId,
                    PerformedBy = performedBy
                };

                int transactionId = _vaultRepository.Add(transaction);

                string memberInfo = relatedMemberId.HasValue ? $" للعضو (ID: {relatedMemberId})" : "";
                _auditRepository.Add(new AuditLog
                {
                    UserID = performedBy,
                    Action = AuditAction.Create,
                    EntityType = EntityType.VaultTransaction,
                    EntityID = transactionId,
                    Details = $"إضافة معاملة {transactionType} بمبلغ {amount} ريال{memberInfo}"
                });

                return (true, "تم إضافة المعاملة بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}");
            }
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

                _vaultRepository.Cancel(transactionId, reason);

                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Cancel,
                    EntityType = EntityType.VaultTransaction,
                    EntityID = transactionId,
                    Details = "إلغاء معاملة الخزنة",
                    Reason = reason
                });

                return (true, "تم إلغاء المعاملة بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }
    }
}
