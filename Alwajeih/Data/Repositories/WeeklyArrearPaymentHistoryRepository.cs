using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Alwajeih.Data;
using Alwajeih.Models;

namespace Alwajeih.Data.Repositories
{
    public class WeeklyArrearPaymentHistoryRepository
    {
        public void Add(WeeklyArrearPaymentHistory history)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO WeeklyArrearPaymentHistory 
                (PlanID, WeekNumber, PaymentDate, AmountPaid, RemainingBeforePayment, RemainingAfterPayment, Notes, RecordedAt)
                VALUES 
                (@PlanID, @WeekNumber, @PaymentDate, @AmountPaid, @RemainingBeforePayment, @RemainingAfterPayment, @Notes, @RecordedAt)";
            
            connection.Execute(sql, history);
        }

        public IEnumerable<WeeklyArrearPaymentHistory> GetByPlanId(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM WeeklyArrearPaymentHistory WHERE PlanID = @PlanID ORDER BY WeekNumber DESC";
            return connection.Query<WeeklyArrearPaymentHistory>(sql, new { PlanID = planId });
        }

        public IEnumerable<WeeklyArrearPaymentHistory> GetByWeek(int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM WeeklyArrearPaymentHistory WHERE WeekNumber = @WeekNumber ORDER BY PlanID";
            return connection.Query<WeeklyArrearPaymentHistory>(sql, new { WeekNumber = weekNumber });
        }

        public IEnumerable<WeeklyArrearPaymentHistory> GetByPlanAndWeek(int planId, int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM WeeklyArrearPaymentHistory WHERE PlanID = @PlanID AND WeekNumber = @WeekNumber";
            return connection.Query<WeeklyArrearPaymentHistory>(sql, new { PlanID = planId, WeekNumber = weekNumber });
        }

        public decimal GetTotalPaidByPlan(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT COALESCE(SUM(AmountPaid), 0) FROM WeeklyArrearPaymentHistory WHERE PlanID = @PlanID";
            return connection.ExecuteScalar<decimal>(sql, new { PlanID = planId });
        }
    }
}
