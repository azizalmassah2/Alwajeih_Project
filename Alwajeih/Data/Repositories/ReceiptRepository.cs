using System;
using System.Collections.Generic;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class ReceiptRepository
    {
        public IEnumerable<Receipt> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT r.*, u.Username as UserName
                FROM Receipts r
                INNER JOIN Users u ON r.PrintedBy = u.UserID
                ORDER BY r.PrintDate DESC";
            return connection.Query<Receipt>(sql);
        }

        public Receipt? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM Receipts WHERE ReceiptID = @Id";
            return connection.QueryFirstOrDefault<Receipt>(sql, new { Id = id });
        }

        public Receipt? GetByReceiptNumber(string receiptNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT r.*, m.Name as MemberName, sp.PlanNumber, dc.AmountPaid, sp.DailyAmount
                FROM Receipts r
                INNER JOIN DailyCollections dc ON r.CollectionID = dc.CollectionID
                INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE r.ReceiptNumber = @ReceiptNumber";
            return connection.QueryFirstOrDefault<Receipt>(sql, new { ReceiptNumber = receiptNumber });
        }

        public int Add(Receipt entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO Receipts (ReceiptNumber, CollectionID, PrintDate, PrintedBy)
                VALUES (@ReceiptNumber, @CollectionID, @PrintDate, @PrintedBy);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.ReceiptNumber,
                entity.CollectionID,
                PrintDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entity.PrintedBy
            });
        }

        public IEnumerable<Receipt> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT r.*, m.Name as MemberName
                FROM Receipts r
                INNER JOIN DailyCollections dc ON r.CollectionID = dc.CollectionID
                INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE DATE(r.PrintDate) BETWEEN @StartDate AND @EndDate
                ORDER BY r.PrintDate DESC";
            return connection.Query<Receipt>(sql, new 
            { 
                StartDate = startDate.ToString("yyyy-MM-dd"), 
                EndDate = endDate.ToString("yyyy-MM-dd") 
            });
        }
    }
}
