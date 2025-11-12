using System;
using System.Linq;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة إدارة الحصص
    /// </summary>
    public class SavingPlanService
    {
        private readonly SavingPlanRepository _planRepository;
        private readonly ArrearRepository _arrearRepository;
        private readonly CollectionRepository _collectionRepository;
        private readonly AuditRepository _auditRepository;
        private readonly VaultRepository _vaultRepository;

        public SavingPlanService()
        {
            _planRepository = new SavingPlanRepository();
            _arrearRepository = new ArrearRepository();
            _collectionRepository = new CollectionRepository();
            _auditRepository = new AuditRepository();
            _vaultRepository = new VaultRepository();
        }

        /// <summary>
        /// إنشاء حصة جديدة
        /// </summary>
        public (bool Success, string Message, int PlanID) CreatePlan(
            int memberId,
            int planNumber,
            decimal dailyAmount,
            DateTime startDate,
            int createdBy,
            CollectionFrequency collectionFrequency = CollectionFrequency.Daily
        )
        {
            try
            {
                // التحقق من عدد الحصص النشطة للعضو
                var activeCount = _planRepository.GetActivePlanCountForMember(memberId);
                if (activeCount >= 2)
                {
                    return (false, "لا يمكن فتح أكثر من حصتين نشطتين للعضو الواحد", 0);
                }

                // التحقق من صحة المبلغ
                if (!ValidationHelper.IsValidAmount(dailyAmount))
                {
                    return (false, "المبلغ اليومي غير صحيح", 0);
                }

                // حساب تاريخ النهاية والإجمالي
                var endDate = DateHelper.GetEndDate(startDate);
                var totalAmount = dailyAmount * 182;

                var plan = new SavingPlan
                {
                    MemberID = memberId,
                    PlanNumber = planNumber,
                    DailyAmount = dailyAmount,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalAmount = totalAmount,
                    Status = PlanStatus.Active,
                    CollectionFrequency = collectionFrequency,
                    CreatedBy = createdBy,
                };

                int planId = _planRepository.Add(plan);

                // إنشاء سجل سوابق فارغ
                _arrearRepository.AddPreviousArrears(
                    new PreviousArrears { PlanID = planId, TotalArrears = 0 }
                );

                // تسجيل في Audit Log
                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = createdBy,
                        Action = AuditAction.Create,
                        EntityType = EntityType.SavingPlan,
                        EntityID = planId,
                        Details = $"إنشاء حصة جديدة رقم {planNumber} بمبلغ يومي {dailyAmount} ريال",
                    }
                );

                return (true, "تم إنشاء الحصة بنجاح", planId);
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// حساب نسبة التقدم للحصة
        /// </summary>
        public double CalculateProgress(SavingPlan plan)
        {
            return DateHelper.CalculateProgress(plan.StartDate, DateTime.Now);
        }

        /// <summary>
        /// حساب الرصيد المتبقي
        /// </summary>
        public decimal CalculateRemainingBalance(int planId)
        {
            var plan = _planRepository.GetById(planId);
            if (plan == null)
                return 0;

            var totalPaid = _collectionRepository.GetTotalPaidForPlan(planId);
            return plan.TotalAmount - totalPaid;
        }

        /// <summary>
        /// إكمال الحصة (تغيير الحالة إلى مكتملة)
        /// </summary>
        public (bool Success, string Message) CompletePlan(int planId, int userId)
        {
            try
            {
                var plan = _planRepository.GetById(planId);
                if (plan == null)
                    return (false, "الحصة غير موجودة");

                // التحقق من عدم وجود متأخرات
                var unpaidArrears = _arrearRepository.GetUnpaidArrears(planId);
                if (unpaidArrears.Any())
                {
                    return (false, "لا يمكن إكمال الحصة لوجود متأخرات غير مسددة");
                }

                plan.Status = PlanStatus.Completed;
                _planRepository.Update(plan);

                // ✅ حساب المبلغ الإجمالي المحصّل وسحبه من الخزنة لتسليمه للعضو
                var totalCollected = _collectionRepository.GetTotalPaidForPlan(planId);

                _vaultRepository.Add(new VaultTransaction
                {
                    TransactionType = TransactionType.Withdrawal,
                    Amount = totalCollected,
                    TransactionDate = DateTime.Now,
                    Description = $"إتمام السهم رقم {plan.PlanNumber} - تسليم المبلغ للعضو",
                    PerformedBy = userId,
                    PerformedAt = DateTime.Now
                });

                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = userId,
                        Action = AuditAction.Update,
                        EntityType = EntityType.SavingPlan,
                        EntityID = planId,
                        Details = $"إكمال الحصة - تم سحب {totalCollected:N2} ريال من الخزنة",
                    }
                );

                return (true, $"تم إكمال الحصة بنجاح وسحب {totalCollected:N2} ريال من الخزنة");
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// أرشفة الحصة
        /// </summary>
        public (bool Success, string Message) ArchivePlan(int planId, int userId)
        {
            try
            {
                var plan = _planRepository.GetById(planId);
                if (plan == null)
                    return (false, "الحصة غير موجودة");

                plan.Status = PlanStatus.Archived;
                _planRepository.Update(plan);

                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = userId,
                        Action = AuditAction.Archive,
                        EntityType = EntityType.SavingPlan,
                        EntityID = planId,
                        Details = "أرشفة الحصة",
                    }
                );

                return (true, "تم أرشفة الحصة بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// الحصول على الخطط النشطة لعضو معين
        /// </summary>
        public System.Collections.Generic.List<SavingPlan> GetActivePlansForMember(int memberId)
        {
            return _planRepository.GetByMemberId(memberId)
                .Where(p => p.Status == PlanStatus.Active)
                .ToList();
        }
    }
}
