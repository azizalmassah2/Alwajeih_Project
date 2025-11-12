using System;
using System.Collections.Generic;
using System.Linq;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class ArrearRepository
    {
        public DailyArrear? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM DailyArrears WHERE ArrearID = @ArrearID";
            return connection.QueryFirstOrDefault<DailyArrear>(sql, new { ArrearID = id });
        }

        public IEnumerable<DailyArrear> GetByPlanId(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM DailyArrears WHERE PlanID = @PlanID ORDER BY ArrearDate";
            return connection.Query<DailyArrear>(sql, new { PlanID = planId });
        }

        public IEnumerable<DailyArrear> GetUnpaidArrears(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM DailyArrears WHERE PlanID = @PlanID AND IsPaid = 0 ORDER BY ArrearDate";
            return connection.Query<DailyArrear>(sql, new { PlanID = planId });
        }

        public int Add(DailyArrear entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO DailyArrears (PlanID, WeekNumber, DayNumber, ArrearDate, AmountDue, RemainingAmount, CreatedDate)
                VALUES (@PlanID, @WeekNumber, @DayNumber, @ArrearDate, @AmountDue, @RemainingAmount, @CreatedDate);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.PlanID,
                entity.WeekNumber,
                entity.DayNumber,
                ArrearDate = entity.ArrearDate.ToString("yyyy-MM-dd"),
                entity.AmountDue,
                RemainingAmount = entity.AmountDue,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public bool Update(DailyArrear entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                UPDATE DailyArrears 
                SET IsPaid = @IsPaid, PaidDate = @PaidDate, PaidAmount = @PaidAmount, RemainingAmount = @RemainingAmount
                WHERE ArrearID = @ArrearID";
            
            return connection.Execute(sql, new
            {
                entity.ArrearID,
                entity.IsPaid,
                PaidDate = entity.PaidDate?.ToString("yyyy-MM-dd"),
                entity.PaidAmount,
                entity.RemainingAmount
            }) > 0;
        }

        /// <summary>
        /// الحصول على سابقة واحدة بـ ID
        /// </summary>
        public PreviousArrears? GetPreviousArrearById(int previousArrearId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM PreviousArrears WHERE PreviousArrearID = @PreviousArrearID";
            return connection.QueryFirstOrDefault<PreviousArrears>(sql, new { PreviousArrearID = previousArrearId });
        }
        
        /// <summary>
        /// الحصول على جميع السابقات لسهم معين
        /// </summary>
        public IEnumerable<PreviousArrears> GetPreviousArrearsByPlanId(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM PreviousArrears WHERE PlanID = @PlanID ORDER BY WeekNumber DESC";
            return connection.Query<PreviousArrears>(sql, new { PlanID = planId });
        }
        
        /// <summary>
        /// الحصول على السابقات لسهم معين في أسبوع معين
        /// </summary>
        public IList<PreviousArrears> GetPreviousArrearsByPlanAndWeek(int planId, int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM PreviousArrears WHERE PlanID = @PlanID AND WeekNumber = @WeekNumber";
            return connection.Query<PreviousArrears>(sql, new { PlanID = planId, WeekNumber = weekNumber }).ToList();
        }
        
        /// <summary>
        /// الحصول على جميع السابقات غير المسددة
        /// </summary>
        public IEnumerable<PreviousArrears> GetUnpaidPreviousArrears()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT pa.*, 
                       m.Name as MemberName,
                       sp.PlanNumber
                FROM PreviousArrears pa
                INNER JOIN SavingPlans sp ON pa.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE pa.RemainingAmount > 0
                ORDER BY m.Name, pa.WeekNumber";
            return connection.Query<PreviousArrears>(sql);
        }

        /// <summary>
        /// الحصول على متأخرات أسبوع معين
        /// </summary>
        public IEnumerable<DailyArrear> GetArrearsByWeek(int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT da.*, 
                       m.Name as MemberName,
                       sp.PlanNumber
                FROM DailyArrears da
                INNER JOIN SavingPlans sp ON da.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE da.WeekNumber = @WeekNumber
                  AND m.MemberType != 'BehindAssociation'
                ORDER BY m.Name, da.DayNumber";
            return connection.Query<DailyArrear>(sql, new { WeekNumber = weekNumber });
        }

        /// <summary>
        /// الحصول على متأخرات سهم معين في أسبوع معين
        /// </summary>
        public IEnumerable<DailyArrear> GetArrearsByPlanAndWeek(int planId, int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM DailyArrears WHERE PlanID = @PlanID AND WeekNumber = @WeekNumber ORDER BY DayNumber";
            return connection.Query<DailyArrear>(sql, new { PlanID = planId, WeekNumber = weekNumber });
        }

        public int AddPreviousArrears(PreviousArrears entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO PreviousArrears (PlanID, WeekNumber, TotalArrears, RemainingAmount, IsPaid, CreatedDate, LastUpdated)
                VALUES (@PlanID, @WeekNumber, @TotalArrears, @RemainingAmount, @IsPaid, @CreatedDate, @LastUpdated);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.PlanID,
                entity.WeekNumber,
                entity.TotalArrears,
                entity.RemainingAmount,
                entity.IsPaid,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public bool UpdatePreviousArrears(PreviousArrears entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                UPDATE PreviousArrears 
                SET TotalArrears = @TotalArrears, 
                    RemainingAmount = @RemainingAmount,
                    IsPaid = @IsPaid,
                    PaidDate = @PaidDate,
                    PaidAmount = @PaidAmount,
                    LastUpdated = @LastUpdated
                WHERE PreviousArrearID = @PreviousArrearID";
            
            return connection.Execute(sql, new
            {
                entity.PreviousArrearID,
                entity.TotalArrears,
                entity.RemainingAmount,
                entity.IsPaid,
                PaidDate = entity.PaidDate?.ToString("yyyy-MM-dd"),
                entity.PaidAmount,
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }) > 0;
        }

        public IEnumerable<DailyArrear> GetAllUnpaid()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT da.*, m.Name as MemberName, sp.PlanNumber
                FROM DailyArrears da
                INNER JOIN SavingPlans sp ON da.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE da.IsPaid = 0
                  AND m.MemberType != 'BehindAssociation'
                ORDER BY da.ArrearDate";
            return connection.Query<DailyArrear>(sql);
        }

        public decimal GetTotalArrearForPlan(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT COALESCE(SUM(RemainingAmount), 0) FROM DailyArrears WHERE PlanID = @PlanID AND IsPaid = 0";
            return connection.ExecuteScalar<decimal>(sql, new { PlanID = planId });
        }

        /// <summary>
        /// الحصول على جميع السوابق (متأخرات الأسابيع السابقة)
        /// </summary>
        public IEnumerable<PreviousArrears> GetAllPreviousArrears()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT pa.*, 
                       m.Name as MemberName,
                       sp.PlanNumber
                FROM PreviousArrears pa
                INNER JOIN SavingPlans sp ON pa.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE m.MemberType != 'BehindAssociation'
                ORDER BY m.Name, pa.WeekNumber";
            return connection.Query<PreviousArrears>(sql);
        }
    }
}
