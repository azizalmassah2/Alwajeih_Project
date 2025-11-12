using System;
using System.Collections.Generic;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class VaultRepository
    {
        public IEnumerable<VaultTransaction> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT 
                    vt.TransactionID,
                    vt.TransactionType,
                    COALESCE(vt.Category, 'Other') as Category,
                    vt.Amount,
                    vt.TransactionDate,
                    vt.Description,
                    vt.RelatedReconciliationID,
                    vt.RelatedMemberID,
                    vt.RelatedPlanID,
                    vt.PerformedBy,
                    vt.PerformedAt,
                    vt.IsCancelled,
                    vt.CancellationReason,
                    u.Username as UserName,
                    m.Name as MemberName
                FROM VaultTransactions vt
                INNER JOIN Users u ON vt.PerformedBy = u.UserID
                LEFT JOIN Members m ON vt.RelatedMemberID = m.MemberID
                WHERE vt.IsCancelled = 0
                ORDER BY vt.TransactionDate DESC, vt.PerformedAt DESC";
            return connection.Query<VaultTransaction>(sql);
        }

        public int Add(VaultTransaction entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO VaultTransactions (TransactionType, Category, Amount, TransactionDate, Description, RelatedReconciliationID, RelatedMemberID, RelatedPlanID, PerformedBy, PerformedAt)
                VALUES (@TransactionType, @Category, @Amount, @TransactionDate, @Description, @RelatedReconciliationID, @RelatedMemberID, @RelatedPlanID, @PerformedBy, @PerformedAt);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                TransactionType = entity.TransactionType.ToString(),
                Category = entity.Category.ToString(),
                entity.Amount,
                TransactionDate = entity.TransactionDate.ToString("yyyy-MM-dd"),
                entity.Description,
                entity.RelatedReconciliationID,
                entity.RelatedMemberID,
                entity.RelatedPlanID,
                entity.PerformedBy,
                PerformedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public decimal GetCurrentBalance()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT 
                    COALESCE(SUM(CASE WHEN TransactionType = 'Deposit' THEN Amount ELSE 0 END), 0) -
                    COALESCE(SUM(CASE WHEN TransactionType IN ('Withdrawal', 'Expense') THEN Amount ELSE 0 END), 0)
                FROM VaultTransactions 
                WHERE IsCancelled = 0";
            return connection.ExecuteScalar<decimal>(sql);
        }

        public IEnumerable<VaultTransaction> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT 
                    vt.TransactionID,
                    vt.TransactionType,
                    COALESCE(vt.Category, 'Other') as Category,
                    vt.Amount,
                    vt.TransactionDate,
                    vt.Description,
                    vt.RelatedReconciliationID,
                    vt.RelatedMemberID,
                    vt.RelatedPlanID,
                    vt.PerformedBy,
                    vt.PerformedAt,
                    vt.IsCancelled,
                    vt.CancellationReason,
                    u.Username as UserName,
                    m.Name as MemberName
                FROM VaultTransactions vt
                INNER JOIN Users u ON vt.PerformedBy = u.UserID
                LEFT JOIN Members m ON vt.RelatedMemberID = m.MemberID
                WHERE vt.TransactionDate BETWEEN @StartDate AND @EndDate 
                AND vt.IsCancelled = 0
                ORDER BY vt.TransactionDate DESC, vt.PerformedAt DESC";
            return connection.Query<VaultTransaction>(sql, new 
            { 
                StartDate = startDate.ToString("yyyy-MM-dd"), 
                EndDate = endDate.ToString("yyyy-MM-dd") 
            });
        }

        public bool Cancel(int transactionId, string reason)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "UPDATE VaultTransactions SET IsCancelled = 1, CancellationReason = @Reason WHERE TransactionID = @TransactionID";
            return connection.Execute(sql, new { TransactionID = transactionId, Reason = reason }) > 0;
        }

        /// <summary>
        /// حساب إجمالي السحوبات للعضو (السحبيات)
        /// </summary>
        public decimal GetTotalMemberWithdrawals(int memberId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT COALESCE(SUM(Amount), 0) 
                FROM VaultTransactions 
                WHERE RelatedMemberID = @MemberID 
                AND TransactionType = 'Withdrawal'
                AND Category = 'MemberWithdrawal'
                AND IsCancelled = 0";
            return connection.ExecuteScalar<decimal>(sql, new { MemberID = memberId });
        }
    }
}
