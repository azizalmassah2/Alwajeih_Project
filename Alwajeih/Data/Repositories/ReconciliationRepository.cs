using System;
using System.Collections.Generic;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class ReconciliationRepository
    {
        public IEnumerable<WeeklyReconciliation> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT wr.*, u.Username as UserName
                FROM WeeklyReconciliations wr
                INNER JOIN Users u ON wr.PerformedBy = u.UserID
                ORDER BY wr.WeekStartDate DESC";
            return connection.Query<WeeklyReconciliation>(sql);
        }

        public WeeklyReconciliation? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM WeeklyReconciliations WHERE ReconciliationID = @Id";
            return connection.QueryFirstOrDefault<WeeklyReconciliation>(sql, new { Id = id });
        }

        public int Add(WeeklyReconciliation entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO WeeklyReconciliations 
                (WeekNumber, WeekStartDate, WeekEndDate, ExpectedAmount, ActualAmount, Difference, Notes, Status, PerformedBy, PerformedDate)
                VALUES (@WeekNumber, @WeekStartDate, @WeekEndDate, @ExpectedAmount, @ActualAmount, @Difference, @Notes, @Status, @PerformedBy, @PerformedDate);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.WeekNumber,
                WeekStartDate = entity.WeekStartDate.ToString("yyyy-MM-dd"),
                WeekEndDate = entity.WeekEndDate.ToString("yyyy-MM-dd"),
                entity.ExpectedAmount,
                entity.ActualAmount,
                entity.Difference,
                entity.Notes,
                Status = entity.Status.ToString(),
                entity.PerformedBy,
                PerformedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public WeeklyReconciliation? GetByWeek(DateTime weekStartDate)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM WeeklyReconciliations WHERE WeekStartDate = @WeekStartDate";
            return connection.QueryFirstOrDefault<WeeklyReconciliation>(sql, new 
            { 
                WeekStartDate = weekStartDate.ToString("yyyy-MM-dd") 
            });
        }

        public IEnumerable<WeeklyReconciliation> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT * FROM WeeklyReconciliations 
                WHERE WeekStartDate >= @StartDate AND WeekEndDate <= @EndDate
                ORDER BY WeekStartDate DESC";
            return connection.Query<WeeklyReconciliation>(sql, new 
            { 
                StartDate = startDate.ToString("yyyy-MM-dd"), 
                EndDate = endDate.ToString("yyyy-MM-dd") 
            });
        }

        public WeeklyReconciliation? GetByWeekNumber(int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM WeeklyReconciliations WHERE WeekNumber = @WeekNumber";
            return connection.QueryFirstOrDefault<WeeklyReconciliation>(sql, new { WeekNumber = weekNumber });
        }
    }
}
