using System;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة السُلف المُقدمة - سحب مبلغ من رصيد العضو المستقبلي
    /// </summary>
    public class AdvancePaymentService
    {
        private readonly AdvancePaymentRepository _advanceRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly CollectionRepository _collectionRepository;
        private readonly VaultRepository _vaultRepository;
        private readonly AuditRepository _auditRepository;

        public AdvancePaymentService()
        {
            _advanceRepository = new AdvancePaymentRepository();
            _planRepository = new SavingPlanRepository();
            _collectionRepository = new CollectionRepository();
            _vaultRepository = new VaultRepository();
            _auditRepository = new AuditRepository();
        }

        /// <summary>
        /// صرف سُلفة للعضو من رصيده المستقبلي
        /// </summary>
        public (bool Success, string Message) ProcessAdvancePayment(
            int planId,
            decimal amount,
            int approvedBy,
            string? description = null
        )
        {
            try
            {
                // 1. التحقق من وجود السهم
                var plan = _planRepository.GetById(planId);
                if (plan == null)
                    return (false, "السهم غير موجود");

                if (plan.Status != PlanStatus.Active)
                    return (false, "السهم غير نشط");

                // 2. حساب الرصيد المتاح
                var totalExpected = plan.TotalAmount; // المبلغ الإجمالي المتوقع (5000 × 182 = 910,000)
                var totalPaid = _collectionRepository.GetTotalPaidForPlan(planId); // ما تم دفعه فعلياً
                var totalAdvances = _advanceRepository.GetTotalAdvanceForPlan(planId); // السُلف السابقة

                // الرصيد المتاح = الإجمالي المتوقع - ما تم دفعه - السُلف السابقة
                var availableBalance = totalExpected - totalPaid - totalAdvances;

                // 3. التحقق من كفاية الرصيد
                if (amount > availableBalance)
                {
                    return (
                        false,
                        $"المبلغ المطلوب ({amount:N2} ريال) أكبر من الرصيد المتاح ({availableBalance:N2} ريال)"
                    );
                }

                if (amount <= 0)
                    return (false, "المبلغ يجب أن يكون أكبر من صفر");

                // 4. تسجيل السُلفة
                var advance = new AdvancePayment
                {
                    PlanID = planId,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    Description = description ?? $"سُلفة من رصيد السهم رقم {plan.PlanNumber}",
                    ApprovedBy = approvedBy,
                    CreatedAt = DateTime.Now,
                };

                int advanceId = _advanceRepository.Add(advance);

                // 5. سحب المبلغ من الخزنة
                _vaultRepository.Add(
                    new VaultTransaction
                    {
                        TransactionType = TransactionType.Withdrawal,
                        Amount = amount,
                        TransactionDate = DateTime.Now,
                        Description = $"سُلفة للسهم رقم {plan.PlanNumber} - {description}",
                        PerformedBy = approvedBy,
                        PerformedAt = DateTime.Now,
                    }
                );

                // 6. تسجيل في Audit Log
                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = approvedBy,
                        Action = AuditAction.Create,
                        EntityType = EntityType.AdvancePayment,
                        EntityID = advanceId,
                        Details =
                            $"صرف سُلفة بمبلغ {amount:N2} ريال للسهم رقم {plan.PlanNumber} - الرصيد المتبقي: {availableBalance - amount:N2} ريال",
                    }
                );

                return (
                    true,
                    $"تم صرف السُلفة بنجاح\nالمبلغ: {amount:N2} ريال\nالرصيد المتبقي: {availableBalance - amount:N2} ريال"
                );
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// حساب الرصيد المتاح للسحب
        /// </summary>
        public (
            decimal TotalExpected,
            decimal TotalPaid,
            decimal TotalAdvances,
            decimal AvailableBalance
        ) GetAvailableBalance(int planId)
        {
            var plan = _planRepository.GetById(planId);
            if (plan == null)
                return (0, 0, 0, 0);

            var totalExpected = plan.TotalAmount;
            var totalPaid = _collectionRepository.GetTotalPaidForPlan(planId);
            var totalAdvances = _advanceRepository.GetTotalAdvanceForPlan(planId);
            var availableBalance = totalExpected - totalPaid - totalAdvances;

            return (totalExpected, totalPaid, totalAdvances, availableBalance);
        }
    }
}
