using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Alwajeih.Models;

namespace Alwajeih.Data.Repositories
{
    public class AdvancePaymentRepository
    {
        public int Add(AdvancePayment advance)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO AdvancePayments (PlanID, Amount, PaymentDate, Description, ApprovedBy, CreatedAt)
                VALUES (@PlanID, @Amount, @PaymentDate, @Description, @ApprovedBy, @CreatedAt);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, advance);
        }

        public List<AdvancePayment> GetByPlanId(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT ap.*, m.Name as MemberName, sp.PlanNumber, u.Username as ApprovedByName
                FROM AdvancePayments ap
                INNER JOIN SavingPlans sp ON ap.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                INNER JOIN Users u ON ap.ApprovedBy = u.UserID
                WHERE ap.PlanID = @PlanID
                ORDER BY ap.PaymentDate DESC";
            
            return connection.Query<AdvancePayment>(sql, new { PlanID = planId }).ToList();
        }

        public decimal GetTotalAdvanceForPlan(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT COALESCE(SUM(Amount), 0) FROM AdvancePayments WHERE PlanID = @PlanID";
            return connection.ExecuteScalar<decimal>(sql, new { PlanID = planId });
        }

        public List<AdvancePayment> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT ap.*, m.Name as MemberName, sp.PlanNumber, u.Username as ApprovedByName
                FROM AdvancePayments ap
                INNER JOIN SavingPlans sp ON ap.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                INNER JOIN Users u ON ap.ApprovedBy = u.UserID
                ORDER BY ap.PaymentDate DESC";
            
            return connection.Query<AdvancePayment>(sql).ToList();
        }
    }
}
