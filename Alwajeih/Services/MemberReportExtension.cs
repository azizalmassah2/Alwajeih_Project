using System;
using System.Data;
using System.Linq;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;

namespace Alwajeih.Services
{
    /// <summary>
    /// توسعة لخدمة التقارير - تقارير الأعضاء الشاملة
    /// </summary>
    public partial class ReportService
    {
        /// <summary>
        /// تقرير مالي شامل للعضو مع نطاق تواريخ - إصدار محسّن
        /// </summary>
        public DataTable GenerateComprehensiveMemberFinancialReport(int memberId, DateTime startDate, DateTime endDate)
        {
            // التحقق من صحة التواريخ
            var validation = ValidateDateRange(startDate, endDate);
            if (!validation.isValid)
                throw new Exception(validation.message);

            var member = _memberRepository.GetById(memberId);
            if (member == null)
                throw new Exception("العضو غير موجود");

            var dt = new DataTable();
            
            // ✅ إضافة معلومات العضو كخصائص للـ DataTable (تظهر في العنوان)
            dt.TableName = $"تقرير مالي شامل - {member.Name}";
            dt.ExtendedProperties["MemberName"] = member.Name;
            dt.ExtendedProperties["MemberPhone"] = member.Phone ?? "-";
            dt.ExtendedProperties["MemberType"] = member.MemberType == MemberType.Regular ? "أساسي" : "خلف الجمعية";
            dt.ExtendedProperties["JoinDate"] = member.CreatedDate.ToString("yyyy-MM-dd");
            dt.ExtendedProperties["ReportPeriod"] = $"{startDate:yyyy-MM-dd} إلى {endDate:yyyy-MM-dd}";
            
            // ✅ الأعمدة - فقط البيانات المهمة
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("النوع");
            dt.Columns.Add("المبلغ المستحق", typeof(decimal));
            dt.Columns.Add("المبلغ المدفوع", typeof(decimal));
            dt.Columns.Add("المتبقي", typeof(decimal));
            dt.Columns.Add("طريقة الدفع");
            dt.Columns.Add("الحالة");

            // جلب جميع الأسهم
            var plans = _planRepository.GetByMemberId(memberId).OrderBy(p => p.PlanNumber).ToList();
            decimal totalPaidInPeriod = 0;
            decimal totalArrearsInPeriod = 0;
            int totalCollectionsInPeriod = 0;

            foreach (var plan in plans)
            {
                var planCollections = _collectionRepository.GetByPlanId(plan.PlanID).Where(c => !c.IsCancelled).ToList();
                
                // التحصيلات خلال الفترة
                var collections = planCollections
                    .Where(c => c.CollectionDate >= startDate && c.CollectionDate <= endDate)
                    .OrderBy(c => c.CollectionDate)
                    .ToList();

                foreach (var collection in collections)
                {
                    var paymentSource = collection.PaymentSource switch
                    {
                        PaymentSource.Cash => "نقدي",
                        PaymentSource.Karimi => "كريمي",
                        PaymentSource.BankTransfer => "تحويل بنكي",
                        _ => "-"
                    };

                    dt.Rows.Add(
                        collection.CollectionDate.ToString("yyyy-MM-dd"),
                        "تحصيل يومي",
                        plan.DailyAmount,
                        collection.AmountPaid,
                        0m,
                        paymentSource,
                        "✅ مسجل"
                    );

                    totalPaidInPeriod += collection.AmountPaid;
                    totalCollectionsInPeriod++;
                }

                // المتأخرات خلال الفترة
                var arrears = _arrearRepository.GetByPlanId(plan.PlanID)
                    .Where(a => a.ArrearDate >= startDate && a.ArrearDate <= endDate)
                    .OrderBy(a => a.ArrearDate)
                    .ToList();

                foreach (var arrear in arrears)
                {
                    var status = arrear.IsPaid ? "✅ مسددة" : 
                        arrear.RemainingAmount < arrear.AmountDue ? "🔸 جزئي" : "⚠️ غير مسددة";

                    dt.Rows.Add(
                        arrear.ArrearDate.ToString("yyyy-MM-dd"),
                        "متأخرة",
                        arrear.AmountDue,
                        arrear.AmountDue - arrear.RemainingAmount,
                        arrear.RemainingAmount,
                        "-",
                        status
                    );

                    if (!arrear.IsPaid)
                        totalArrearsInPeriod += arrear.RemainingAmount;
                }

                // السوابق
                var previousArrears = _arrearRepository.GetPreviousArrearsByPlanId(plan.PlanID)
                    .Where(pa => pa.CreatedDate >= startDate && pa.CreatedDate <= endDate)
                    .ToList();

                foreach (var pa in previousArrears)
                {
                    var paStatus = pa.IsPaid ? "✅ مسددة" : "⚠️ غير مسددة";
                    dt.Rows.Add(
                        pa.CreatedDate.ToString("yyyy-MM-dd"),
                        $"سابقة أسبوع {pa.WeekNumber}",
                        pa.TotalArrears,
                        pa.PaidAmount,
                        pa.RemainingAmount,
                        "-",
                        paStatus
                    );
                }
            }

            // السحوبات (إذا كان العضو استلم مبالغ)
            var withdrawals = _vaultRepository.GetByDateRange(startDate, endDate)
                .Where(v => v.TransactionType == TransactionType.Withdrawal && 
                       !v.IsCancelled &&
                       (v.RelatedMemberID == memberId || 
                        (v.Description != null && v.Description.Contains(member.Name))))
                .ToList();

            decimal totalWithdrawals = 0;
            foreach (var withdrawal in withdrawals)
            {
                dt.Rows.Add(
                    withdrawal.TransactionDate.ToString("yyyy-MM-dd"),
                    "سحب (استلام)",
                    0m,
                    withdrawal.Amount,
                    0m,
                    "-",
                    "💸 مستلم"
                );
                totalWithdrawals += withdrawal.Amount;
            }

            // الإجماليات
            if (totalCollectionsInPeriod > 0 || totalArrearsInPeriod > 0 || totalWithdrawals > 0)
            {
                dt.Rows.Add(
                    "-",
                    "📊 الإجمالي",
                    0m,
                    totalPaidInPeriod,
                    totalArrearsInPeriod,
                    "-",
                    totalArrearsInPeriod > 0 ? "⚠️ يوجد متأخرات" : "✅ لا توجد متأخرات"
                );
            }

            return dt;
        }

        /// <summary>
        /// تقرير تفصيلي لجميع معاملات العضو في فترة محددة
        /// </summary>
        public DataTable GenerateMemberTransactionsReport(int memberId, DateTime startDate, DateTime endDate)
        {
            var validation = ValidateDateRange(startDate, endDate);
            if (!validation.isValid)
                throw new Exception(validation.message);

            var member = _memberRepository.GetById(memberId);
            if (member == null)
                throw new Exception("العضو غير موجود");

            var dt = new DataTable();
            
            // ✅ إضافة معلومات العضو كخصائص
            dt.TableName = $"تقرير معاملات - {member.Name}";
            dt.ExtendedProperties["MemberName"] = member.Name;
            dt.ExtendedProperties["MemberPhone"] = member.Phone ?? "-";
            dt.ExtendedProperties["ReportPeriod"] = $"{startDate:yyyy-MM-dd} إلى {endDate:yyyy-MM-dd}";
            
            // ✅ أعمدة مبسطة
            dt.Columns.Add("التاريخ");
            dt.Columns.Add("النوع");
            dt.Columns.Add("المبلغ", typeof(decimal));
            dt.Columns.Add("طريقة الدفع");
            dt.Columns.Add("رقم المرجع");
            dt.Columns.Add("الحالة");

            var plans = _planRepository.GetByMemberId(memberId);

            foreach (var plan in plans)
            {
                var collections = _collectionRepository.GetByPlanId(plan.PlanID)
                    .Where(c => c.CollectionDate >= startDate && c.CollectionDate <= endDate && !c.IsCancelled)
                    .OrderBy(c => c.CollectionDate);

                foreach (var c in collections)
                {
                    var paymentSource = c.PaymentSource switch
                    {
                        PaymentSource.Cash => "نقدي",
                        PaymentSource.Karimi => "كريمي",
                        PaymentSource.BankTransfer => "تحويل بنكي",
                        _ => "-"
                    };

                    dt.Rows.Add(
                        c.CollectionDate.ToString("yyyy-MM-dd HH:mm"),
                        "تحصيل يومي",
                        c.AmountPaid,
                        paymentSource,
                        c.ReferenceNumber ?? "-",
                        "✅ مسجل"
                    );
                }
            }

            return dt;
        }
    }
}
