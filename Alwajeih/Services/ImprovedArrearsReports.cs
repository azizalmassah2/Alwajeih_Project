using System;
using System.Data;
using System.Linq;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    /// <summary>
    /// تقارير محسّنة للمتأخرات والسوابق
    /// </summary>
    public partial class ReportService
    {
        /// <summary>
        /// تقرير السوابق المحسّن - ملخص لكل عضو
        /// </summary>
        public DataTable GenerateImprovedPreviousArrearsReport()
        {
            var dt = new DataTable();
            dt.Columns.Add("اسم العضو");
            dt.Columns.Add("عدد السوابق", typeof(int));
            dt.Columns.Add("إجمالي المبلغ", typeof(decimal));
            dt.Columns.Add("المبلغ المدفوع", typeof(decimal));
            dt.Columns.Add("المبلغ المتبقي", typeof(decimal));
            dt.Columns.Add("نسبة السداد");
            dt.Columns.Add("الحالة");

            var previousArrears = _arrearRepository.GetAllPreviousArrears().OrderBy(pa => pa.MemberName).ToList();
            var memberGroups = previousArrears.GroupBy(pa => pa.MemberName);

            foreach (var group in memberGroups)
            {
                var totalAmount = group.Sum(pa => pa.TotalArrears);
                var totalPaid = group.Sum(pa => pa.TotalArrears - pa.RemainingAmount);
                var totalRemaining = group.Sum(pa => pa.RemainingAmount);
                var paymentPercent = totalAmount > 0 ? (totalPaid / totalAmount) * 100 : 0;
                
                var status = totalRemaining == 0 ? "✅ مسدد" :
                    totalPaid > 0 ? "🔄 جزئي" : "❌ غير مسدد";

                dt.Rows.Add(
                    group.Key,
                    group.Count(),
                    totalAmount,
                    totalPaid,
                    totalRemaining,
                    $"{paymentPercent:F1}%",
                    status
                );
            }

            // الإجماليات
            if (memberGroups.Any())
            {
                var total = previousArrears.Sum(pa => pa.TotalArrears);
                var paid = previousArrears.Sum(pa => pa.TotalArrears - pa.RemainingAmount);
                var remaining = previousArrears.Sum(pa => pa.RemainingAmount);
                var percent = total > 0 ? (paid / total) * 100 : 0;

                dt.Rows.Add(
                    "📊 الإجماليات",
                    previousArrears.Count,
                    total,
                    paid,
                    remaining,
                    $"{percent:F1}%",
                    "📈"
                );
            }

            return dt;
        }
    }
}
