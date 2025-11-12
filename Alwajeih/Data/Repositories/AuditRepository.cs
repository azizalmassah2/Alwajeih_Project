using System;
using System.Collections.Generic;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class AuditRepository
    {
        public IEnumerable<AuditLog> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT al.*, u.Username as UserName
                FROM AuditLogs al
                INNER JOIN Users u ON al.UserID = u.UserID
                ORDER BY al.Timestamp DESC
                LIMIT 1000";
            return connection.Query<AuditLog>(sql);
        }

        public int Add(AuditLog entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO AuditLogs (UserID, Action, EntityType, EntityID, Details, Reason, Timestamp, IPAddress)
                VALUES (@UserID, @Action, @EntityType, @EntityID, @Details, @Reason, @Timestamp, @IPAddress);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.UserID,
                Action = entity.Action.ToString(),
                EntityType = entity.EntityType.ToString(),
                entity.EntityID,
                entity.Details,
                entity.Reason,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entity.IPAddress
            });
        }

        public IEnumerable<AuditLog> GetByUserId(int userId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT al.*, u.Username as UserName
                FROM AuditLogs al
                INNER JOIN Users u ON al.UserID = u.UserID
                WHERE al.UserID = @UserID
                ORDER BY al.Timestamp DESC";
            return connection.Query<AuditLog>(sql, new { UserID = userId });
        }

        public IEnumerable<AuditLog> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT al.*, u.Username as UserName
                FROM AuditLogs al
                INNER JOIN Users u ON al.UserID = u.UserID
                WHERE DATE(al.Timestamp) BETWEEN @StartDate AND @EndDate
                ORDER BY al.Timestamp DESC";
            return connection.Query<AuditLog>(sql, new 
            { 
                StartDate = startDate.ToString("yyyy-MM-dd"), 
                EndDate = endDate.ToString("yyyy-MM-dd") 
            });
        }
    }
}
