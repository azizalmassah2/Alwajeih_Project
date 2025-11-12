using System;
using System.Collections.Generic;
using System.Linq;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class SavingPlanRepository : IRepository<SavingPlan>
    {
        public IEnumerable<SavingPlan> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.Status = 'Active'
                ORDER BY m.Name, sp.PlanNumber";

            var plans = connection.Query<SavingPlan>(sql).ToList();
            foreach (var plan in plans)
            {
                plan.Status = Enum.Parse<PlanStatus>(plan.Status.ToString());
            }
            return plans;
        }

        public SavingPlan? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.PlanID = @PlanID";
            return connection.QueryFirstOrDefault<SavingPlan>(sql, new { PlanID = id });
        }

        public int Add(SavingPlan entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                INSERT INTO SavingPlans (MemberID, PlanNumber, DailyAmount, StartDate, EndDate, TotalAmount, Status, CollectionFrequency, CreatedDate, CreatedBy)
                VALUES (@MemberID, @PlanNumber, @DailyAmount, @StartDate, @EndDate, @TotalAmount, @Status, @CollectionFrequency, @CreatedDate, @CreatedBy);
                SELECT last_insert_rowid();";

            return connection.ExecuteScalar<int>(
                sql,
                new
                {
                    entity.MemberID,
                    entity.PlanNumber,
                    entity.DailyAmount,
                    StartDate = entity.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = entity.EndDate.ToString("yyyy-MM-dd"),
                    entity.TotalAmount,
                    Status = entity.Status.ToString(),
                    CollectionFrequency = entity.CollectionFrequency.ToString(),
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    entity.CreatedBy,
                }
            );
        }

        public bool Update(SavingPlan entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                UPDATE SavingPlans 
                SET DailyAmount = @DailyAmount, 
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    TotalAmount = @TotalAmount,
                    Status = @Status,
                    CollectionFrequency = @CollectionFrequency
                WHERE PlanID = @PlanID";

            return connection.Execute(
                    sql,
                    new
                    {
                        entity.PlanID,
                        entity.DailyAmount,
                        StartDate = entity.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = entity.EndDate.ToString("yyyy-MM-dd"),
                        entity.TotalAmount,
                        Status = entity.Status.ToString(),
                        CollectionFrequency = entity.CollectionFrequency.ToString(),
                    }
                ) > 0;
        }

        public bool Delete(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "DELETE FROM SavingPlans WHERE PlanID = @PlanID";
            return connection.Execute(sql, new { PlanID = id }) > 0;
        }

        public IEnumerable<SavingPlan> GetByMemberId(int memberId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.MemberID = @MemberID
                ORDER BY sp.PlanNumber";
            return connection.Query<SavingPlan>(sql, new { MemberID = memberId });
        }

        public IEnumerable<SavingPlan> GetActivePlans()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.Status = 'Active'
                  AND m.MemberType != 'BehindAssociation'
                ORDER BY m.Name, sp.PlanNumber";
            return connection.Query<SavingPlan>(sql);
        }

        public IEnumerable<SavingPlan> GetActive()
        {
            return GetActivePlans(); // Alias for GetActivePlans
        }

        public int GetActivePlanCountForMember(int memberId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                "SELECT COUNT(*) FROM SavingPlans WHERE MemberID = @MemberID AND Status = 'Active'";
            return connection.ExecuteScalar<int>(sql, new { MemberID = memberId });
        }

        public IEnumerable<SavingPlan> GetDueForCollection(DateTime date)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.Status = 'Active'
                AND sp.StartDate <= @Date
                AND sp.EndDate >= @Date
                ORDER BY m.Name, sp.PlanNumber";
            return connection.Query<SavingPlan>(sql, new { Date = date.ToString("yyyy-MM-dd") });
        }

        /// <summary>
        /// الحصول على الحصص المستحقة لأسبوع معين
        /// </summary>
        public List<SavingPlan> GetDueForWeek(int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.Status = 'Active'
                  AND m.MemberType != 'BehindAssociation'
                ORDER BY m.Name, sp.PlanNumber";
            return connection.Query<SavingPlan>(sql).ToList();
        }

        /// <summary>
        /// الحصول على الحصص المستحقة ليوم معين من أسبوع معين (باستثناء من سدد التحصيل اليومي بالفعل)
        /// </summary>
        public List<SavingPlan> GetDueForWeekDay(int weekNumber, int dayNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.Status = 'Active'
                  AND m.MemberType != 'BehindAssociation'
                AND NOT EXISTS (
                    SELECT 1 FROM DailyCollections dc 
                    WHERE dc.PlanID = sp.PlanID 
                    AND dc.WeekNumber = @WeekNumber 
                    AND dc.DayNumber = @DayNumber 
                    AND dc.IsCancelled = 0
                    AND dc.AmountPaid > 0
                )
                ORDER BY m.Name, sp.PlanNumber";
            
            var allPlans = connection.Query<SavingPlan>(sql, new { WeekNumber = weekNumber, DayNumber = dayNumber }).ToList();
            
            // فلترة الخطط حسب أيام التحصيل
            return allPlans.Where(p => p.CollectionDays == null || p.CollectionDays.Count == 0 || p.CollectionDays.Contains(dayNumber)).ToList();
        }

        /// <summary>
        /// الحصول على إجمالي مبلغ الأسهم النشطة للعضو
        /// </summary>
        public decimal GetTotalActivePlansAmount(int memberId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT COALESCE(SUM(TotalAmount), 0) 
                FROM SavingPlans 
                WHERE MemberID = @MemberID AND Status = 'Active'";
            return connection.ExecuteScalar<decimal>(sql, new { MemberID = memberId });
        }

        /// <summary>
        /// التحقق من وجود أسهم نشطة للعضو
        /// </summary>
        public bool HasActivePlans(int memberId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT COUNT(*) 
                FROM SavingPlans 
                WHERE MemberID = @MemberID AND Status = 'Active'";
            return connection.ExecuteScalar<int>(sql, new { MemberID = memberId }) > 0;
        }

        /// <summary>
        /// الحصول على جميع الخطط النشطة
        /// </summary>
        public IEnumerable<SavingPlan> GetAllActive()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT sp.*, m.Name as MemberName 
                FROM SavingPlans sp
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE sp.Status = 'Active'
                  AND m.MemberType != 'BehindAssociation'
                ORDER BY m.Name";
            return connection.Query<SavingPlan>(sql);
        }
    }
}
