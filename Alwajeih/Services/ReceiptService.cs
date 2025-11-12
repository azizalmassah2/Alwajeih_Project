using System;
using System.Linq;
using Alwajeih.Utilities.Helpers;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    public class ReceiptService
    {
        private readonly ReceiptRepository _receiptRepository;
        private readonly CollectionRepository _collectionRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly ArrearRepository _arrearRepository;

        public ReceiptService()
        {
            _receiptRepository = new ReceiptRepository();
            _collectionRepository = new CollectionRepository();
            _planRepository = new SavingPlanRepository();
            _arrearRepository = new ArrearRepository();
        }

        public (bool Success, string Message, Receipt? Receipt) GenerateReceipt(int collectionId, int printedBy)
        {
            try
            {
                var collection = _collectionRepository.GetById(collectionId);
                if (collection == null)
                    return (false, "التحصيل غير موجود", null);

                var plan = _planRepository.GetById(collection.PlanID);
                if (plan == null)
                    return (false, "الحصة غير موجودة", null);

                // حساب الرصيد المتبقي
                var totalPaid = _collectionRepository.GetTotalPaidForPlan(collection.PlanID);
                var remainingBalance = plan.TotalAmount - totalPaid;

                // حساب السوابق
                var previousArrears = _arrearRepository.GetPreviousArrearsByPlanId(collection.PlanID);
                var totalArrears = previousArrears?.Where(p => !p.IsPaid).Sum(p => p.RemainingAmount) ?? 0;

                var receipt = new Receipt
                {
                    ReceiptNumber = collection.ReceiptNumber ?? ReceiptNumberGenerator.GenerateReceiptNumber(),
                    CollectionID = collectionId,
                    PrintedBy = printedBy,
                    MemberName = collection.MemberName,
                    PlanNumber = collection.PlanNumber,
                    AmountPaid = collection.AmountPaid,
                    DailyAmount = collection.DailyAmount,
                    RemainingBalance = remainingBalance,
                    PreviousArrears = totalArrears
                };

                _receiptRepository.Add(receipt);

                return (true, "تم إنشاء الإيصال بنجاح", receipt);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}", null);
            }
        }

        public Receipt? GetReceiptByNumber(string receiptNumber)
        {
            return _receiptRepository.GetByReceiptNumber(receiptNumber);
        }
    }
}
