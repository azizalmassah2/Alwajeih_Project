using System;
using System.Collections.Generic;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class ExternalPaymentRepository
    {
        public IEnumerable<ExternalPayment> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT ep.*, u.Username as UserName
                FROM ExternalPayments ep
                INNER JOIN Users u ON ep.CreatedBy = u.UserID
                ORDER BY ep.PaymentDate DESC";
            return connection.Query<ExternalPayment>(sql);
        }

        public ExternalPayment? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM ExternalPayments WHERE ExternalPaymentID = @Id";
            return connection.QueryFirstOrDefault<ExternalPayment>(sql, new { Id = id });
        }

        public int Add(ExternalPayment entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO ExternalPayments (ReferenceNumber, Amount, PaymentDate, PaymentSource, Status, Notes, CreatedDate, CreatedBy)
                VALUES (@ReferenceNumber, @Amount, @PaymentDate, @PaymentSource, @Status, @Notes, @CreatedDate, @CreatedBy);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.ReferenceNumber,
                entity.Amount,
                PaymentDate = entity.PaymentDate.ToString("yyyy-MM-dd"),
                PaymentSource = entity.PaymentSource.ToString(),
                Status = entity.Status.ToString(),
                entity.Notes,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entity.CreatedBy
            });
        }

        public bool Update(ExternalPayment entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                UPDATE ExternalPayments 
                SET Status = @Status, MatchedWithCollectionID = @MatchedWithCollectionID, Notes = @Notes
                WHERE ExternalPaymentID = @ExternalPaymentID";
            
            return connection.Execute(sql, new
            {
                entity.ExternalPaymentID,
                Status = entity.Status.ToString(),
                entity.MatchedWithCollectionID,
                entity.Notes
            }) > 0;
        }

        public IEnumerable<ExternalPayment> GetPending()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM ExternalPayments WHERE Status = 'Pending' ORDER BY PaymentDate";
            return connection.Query<ExternalPayment>(sql);
        }

        public bool MatchWithCollection(int externalPaymentId, int collectionId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                UPDATE ExternalPayments 
                SET Status = 'Matched', MatchedWithCollectionID = @CollectionID
                WHERE ExternalPaymentID = @ExternalPaymentID";
            
            return connection.Execute(sql, new { ExternalPaymentID = externalPaymentId, CollectionID = collectionId }) > 0;
        }

        public IEnumerable<ExternalPayment> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT ep.*, m.Name as MemberName, u.Username as UserName
                FROM ExternalPayments ep
                LEFT JOIN Members m ON ep.MemberID = m.MemberID
                INNER JOIN Users u ON ep.CreatedBy = u.UserID
                WHERE DATE(ep.PaymentDate) BETWEEN DATE(@StartDate) AND DATE(@EndDate)
                ORDER BY ep.PaymentDate DESC";
            
            return connection.Query<ExternalPayment>(sql, new
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            });
        }
    }
}
