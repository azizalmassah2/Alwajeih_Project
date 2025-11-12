using System;
using System.Collections.Generic;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class CollectionRepository : IRepository<DailyCollection>
    {
        public IEnumerable<DailyCollection> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                FROM DailyCollections dc
                INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE dc.IsCancelled = 0
                ORDER BY dc.CollectionDate DESC";
            return connection.Query<DailyCollection>(sql);
        }

        public DailyCollection? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                FROM DailyCollections dc
                INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE dc.CollectionID = @CollectionID";
            return connection.QueryFirstOrDefault<DailyCollection>(sql, new { CollectionID = id });
        }

        public int Add(DailyCollection entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO DailyCollections (PlanID, CollectionDate, AmountPaid, PaymentType, PaymentSource, ReferenceNumber, ReceiptNumber, Notes, CollectedBy, CollectedAt)
                VALUES (@PlanID, @CollectionDate, @AmountPaid, @PaymentType, @PaymentSource, @ReferenceNumber, @ReceiptNumber, @Notes, @CollectedBy, @CollectedAt);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.PlanID,
                CollectionDate = entity.CollectionDate.ToString("yyyy-MM-dd"),
                entity.AmountPaid,
                PaymentType = entity.PaymentType.ToString(),
                PaymentSource = entity.PaymentSource.ToString(),
                entity.ReferenceNumber,
                entity.ReceiptNumber,
                entity.Notes,
                entity.CollectedBy,
                CollectedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public bool Update(DailyCollection entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                UPDATE DailyCollections 
                SET AmountPaid = @AmountPaid, 
                    PaymentType = @PaymentType,
                    PaymentSource = @PaymentSource,
                    ReferenceNumber = @ReferenceNumber,
                    Notes = @Notes
                WHERE CollectionID = @CollectionID";
            
            return connection.Execute(sql, new
            {
                entity.CollectionID,
                entity.AmountPaid,
                PaymentType = entity.PaymentType.ToString(),
                PaymentSource = entity.PaymentSource.ToString(),
                entity.ReferenceNumber,
                entity.Notes
            }) > 0;
        }

        public bool Delete(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "DELETE FROM DailyCollections WHERE CollectionID = @CollectionID";
            return connection.Execute(sql, new { CollectionID = id }) > 0;
        }

        public IEnumerable<DailyCollection> GetByPlanId(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT * FROM DailyCollections 
                WHERE PlanID = @PlanID AND IsCancelled = 0 
                ORDER BY CollectionDate DESC";
            return connection.Query<DailyCollection>(sql, new { PlanID = planId });
        }

        public IEnumerable<DailyCollection> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT dc.*, m.Name as MemberName 
                FROM DailyCollections dc
                INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE dc.CollectionDate BETWEEN @StartDate AND @EndDate 
                AND dc.IsCancelled = 0
                ORDER BY dc.CollectionDate";
            return connection.Query<DailyCollection>(sql, new 
            { 
                StartDate = startDate.ToString("yyyy-MM-dd"), 
                EndDate = endDate.ToString("yyyy-MM-dd") 
            });
        }

        public decimal GetTotalPaidForPlan(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT COALESCE(SUM(AmountPaid), 0) FROM DailyCollections WHERE PlanID = @PlanID AND IsCancelled = 0";
            return connection.ExecuteScalar<decimal>(sql, new { PlanID = planId });
        }

        public bool Cancel(int collectionId, string reason)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "UPDATE DailyCollections SET IsCancelled = 1, CancellationReason = @Reason WHERE CollectionID = @CollectionID";
            return connection.Execute(sql, new { CollectionID = collectionId, Reason = reason }) > 0;
        }

        /// <summary>
        /// التحقق من وجود سداد مسبق لنفس الحصة في نفس الأسبوع واليوم
        /// </summary>
        public bool HasPaymentForWeekDay(int planId, int weekNumber, int dayNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT COUNT(*) 
                FROM DailyCollections 
                WHERE PlanID = @PlanID 
                AND WeekNumber = @WeekNumber 
                AND DayNumber = @DayNumber 
                AND IsCancelled = 0";
            return connection.ExecuteScalar<int>(sql, new 
            { 
                PlanID = planId, 
                WeekNumber = weekNumber, 
                DayNumber = dayNumber 
            }) > 0;
        }
    }
}
