using System;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.Utilities;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة التحصيل اليومي
    /// </summary>
    public class CollectionService
    {
        private readonly CollectionRepository _collectionRepository;
        private readonly ArrearRepository _arrearRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly AuditRepository _auditRepository;
        private readonly ArrearService _arrearService;
        private readonly ExternalPaymentRepository _externalPaymentRepository;

        public CollectionService()
        {
            _collectionRepository = new CollectionRepository();
            _arrearRepository = new ArrearRepository();
            _planRepository = new SavingPlanRepository();
            _auditRepository = new AuditRepository();
            _arrearService = new ArrearService();
            _externalPaymentRepository = new ExternalPaymentRepository();
        }

        /// <summary>
        /// تسجيل تحصيل يومي
        /// </summary>
        public (bool Success, string Message, string? ReceiptNumber) RecordCollection(
            int planId,
            DateTime collectionDate,
            decimal amountPaid,
            PaymentType paymentType,
            PaymentSource paymentSource,
            string? referenceNumber,
            string? notes,
            int collectedBy
        )
        {
            try
            {
                var plan = _planRepository.GetById(planId);
                if (plan == null)
                    return (false, "الحصة غير موجودة", null);

                // توليد رقم الإيصال
                string receiptNumber = ReceiptNumberGenerator.GenerateReceiptNumber();

                var collection = new DailyCollection
                {
                    PlanID = planId,
                    CollectionDate = collectionDate,
                    AmountPaid = amountPaid,
                    PaymentType = paymentType,
                    PaymentSource = paymentSource,
                    ReferenceNumber = referenceNumber,
                    ReceiptNumber = receiptNumber,
                    Notes = notes,
                    CollectedBy = collectedBy,
                };

                int collectionId = _collectionRepository.Add(collection);

                // إذا كان المبلغ المدفوع أقل من المبلغ اليومي، إنشاء متأخرة يومية
                if (amountPaid < plan.DailyAmount)
                {
                    decimal arrearAmount = plan.DailyAmount - amountPaid;
                    _arrearService.CreateDailyArrear(planId, collectionDate, arrearAmount);
                }

                // تسجيل في Audit Log
                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = collectedBy,
                        Action = AuditAction.Create,
                        EntityType = EntityType.DailyCollection,
                        EntityID = collectionId,
                        Details =
                            $"تسجيل تحصيل بمبلغ {amountPaid} ريال - إيصال رقم {receiptNumber}",
                    }
                );

                return (true, "تم تسجيل التحصيل بنجاح", receiptNumber);
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}", null);
            }
        }

        /// <summary>
        /// إلغاء تحصيل
        /// </summary>
        public (bool Success, string Message) CancelCollection(
            int collectionId,
            string reason,
            int userId
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                    return (false, "يجب إدخال سبب الإلغاء");

                _collectionRepository.Cancel(collectionId, reason);

                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = userId,
                        Action = AuditAction.Cancel,
                        EntityType = EntityType.DailyCollection,
                        EntityID = collectionId,
                        Details = "إلغاء تحصيل",
                        Reason = reason,
                    }
                );

                return (true, "تم إلغاء التحصيل بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل تحصيل مع معلومات الأسبوع واليوم
        /// </summary>
        public (bool Success, string Message, string? ReceiptNumber) RecordCollectionWithWeek(
            DailyCollection collection
        )
        {
            try
            {
                var plan = _planRepository.GetById(collection.PlanID);
                if (plan == null)
                    return (false, "الحصة غير موجودة", null);

                // التحقق من صحة الأسبوع واليوم
                if (!WeekHelper.IsValidWeek(collection.WeekNumber))
                    return (
                        false,
                        $"رقم الأسبوع غير صحيح. يجب أن يكون بين 1 و {WeekHelper.TotalWeeks}",
                        null
                    );

                if (!WeekHelper.IsValidDay(collection.DayNumber))
                    return (
                        false,
                        $"رقم اليوم غير صحيح. يجب أن يكون بين 1 و {WeekHelper.DaysPerWeek}",
                        null
                    );

                // ⭐ التحقق من وجود سجل سابق (سداد سابقة) لنفس الحصة في نفس الأسبوع واليوم
                var collectionRepo = new DailyCollectionRepository();
                var existingCollection = collectionRepo.GetByPlanWeekDay(collection.PlanID, collection.WeekNumber, collection.DayNumber);
                
                int collectionId;
                
                if (existingCollection != null)
                {
                    // ❌ يوجد سجل سابق - لا يمكن التكرار
                    string dayName = WeekHelper.GetArabicDayName(collection.DayNumber);
                    return (
                        false,
                        $"⚠️ تم تسجيل سداد مسبقاً لهذه الحصة في {dayName} - الأسبوع {collection.WeekNumber}\n\n" +
                        "لا يمكن تسجيل سداد مكرر لنفس اليوم.",
                        null
                    );
                }
                else
                {
                    // ✅ لا يوجد سجل سابق - ننشئ سجل جديد
                    collection.ReceiptNumber = ReceiptNumberGenerator.GenerateReceiptNumber();
                    collection.DayName = WeekHelper.GetArabicDayName(collection.DayNumber);
                    collectionId = collectionRepo.Add(collection);
                }

                // ملاحظة: لا يتم إضافة المبلغ للخزنة مباشرة
                // سيتم الترحيل للخزنة في نهاية اليوم بعد التدقيق

                // إذا كان المبلغ المدفوع أقل من المبلغ اليومي، إنشاء متأخرة يومية
                if (collection.AmountPaid < plan.DailyAmount)
                {
                    decimal arrearAmount = plan.DailyAmount - collection.AmountPaid;
                    _arrearService.CreateDailyArrear(
                        collection.PlanID,
                        collection.CollectionDate,
                        arrearAmount
                    );
                }

                // إذا كان الدفع عبر كريمي، تسجيله في المدفوعات الخارجية
                if (collection.PaymentSource == PaymentSource.Karimi)
                {
                    var externalPayment = new ExternalPayment
                    {
                        MemberID = plan.MemberID,
                        PaymentDate = collection.CollectionDate,
                        Amount = collection.AmountPaid,
                        PaymentSource = PaymentSource.Karimi,
                        Notes = $"تحصيل من {plan.MemberName} - {collection.DayName} الأسبوع {collection.WeekNumber}",
                        ReferenceNumber = collection.ReceiptNumber,
                        Status = ExternalPaymentStatus.Pending,
                        MatchedPlanID = plan.PlanID,
                        CreatedBy = collection.CollectedBy
                    };
                    _externalPaymentRepository.Add(externalPayment);
                }

                // تسجيل في Audit Log
                _auditRepository.Add(
                    new AuditLog
                    {
                        UserID = collection.CollectedBy,
                        Action = AuditAction.Create,
                        EntityType = EntityType.DailyCollection,
                        EntityID = collectionId,
                        Details =
                            $"تسجيل تحصيل بمبلغ {collection.AmountPaid} ريال - {collection.DayName} الأسبوع {collection.WeekNumber} - إيصال رقم {collection.ReceiptNumber}",
                    }
                );

                return (true, "تم تسجيل التحصيل بنجاح", collection.ReceiptNumber);
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}", null);
            }
        }

        /// <summary>
        /// الحصول على إجمالي المبلغ المحصل لخطة معينة
        /// </summary>
        public decimal GetTotalCollectedForPlan(int planId)
        {
            try
            {
                return _collectionRepository.GetTotalPaidForPlan(planId);
            }
            catch
            {
                return 0;
            }
        }
    }
}
