using System;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    public class ExternalPaymentService
    {
        private readonly ExternalPaymentRepository _externalPaymentRepository;
        private readonly AuditRepository _auditRepository;

        public ExternalPaymentService()
        {
            _externalPaymentRepository = new ExternalPaymentRepository();
            _auditRepository = new AuditRepository();
        }

        public (bool Success, string Message, int PaymentID) RegisterPayment(string referenceNumber, decimal amount, DateTime paymentDate, PaymentSource source, string? notes, int createdBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(referenceNumber))
                    return (false, "رقم المرجع مطلوب", 0);

                if (amount <= 0)
                    return (false, "المبلغ يجب أن يكون أكبر من صفر", 0);

                var payment = new ExternalPayment
                {
                    ReferenceNumber = referenceNumber,
                    Amount = amount,
                    PaymentDate = paymentDate,
                    PaymentSource = source,
                    Status = ExternalPaymentStatus.Pending,
                    Notes = notes,
                    CreatedBy = createdBy
                };

                int paymentId = _externalPaymentRepository.Add(payment);

                _auditRepository.Add(new AuditLog
                {
                    UserID = createdBy,
                    Action = AuditAction.Create,
                    EntityType = EntityType.ExternalPayment,
                    EntityID = paymentId,
                    Details = $"تسجيل دفعة خارجية - المرجع: {referenceNumber} - المبلغ: {amount}"
                });

                return (true, "تم تسجيل الدفعة بنجاح", paymentId);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}", 0);
            }
        }

        public (bool Success, string Message) MatchWithCollection(int externalPaymentId, int collectionId, int userId)
        {
            try
            {
                _externalPaymentRepository.MatchWithCollection(externalPaymentId, collectionId);

                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.ExternalPayment,
                    EntityID = externalPaymentId,
                    Details = $"مطابقة دفعة خارجية مع تحصيل رقم {collectionId}"
                });

                return (true, "تمت المطابقة بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }
    }
}
