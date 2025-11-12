using System.Collections.Generic;
using System.Linq;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    public class AccumulatedArrearsRepository
    {
        /// <summary>
        /// الحصول على السابقات المتراكمة لعضو معين
        /// </summary>
        public AccumulatedArrears? GetByPlanId(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT aa.*, 
                       m.Name as MemberName,
                       sp.PlanNumber
                FROM AccumulatedArrears aa
                INNER JOIN SavingPlans sp ON aa.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE aa.PlanID = @PlanID";
            
            return connection.QueryFirstOrDefault<AccumulatedArrears>(sql, new { PlanID = planId });
        }
        
        /// <summary>
        /// الحصول على جميع السابقات المتراكمة (فقط غير المسددة)
        /// </summary>
        public IEnumerable<AccumulatedArrears> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                SELECT aa.*, 
                       m.Name as MemberName,
                       sp.PlanNumber
                FROM AccumulatedArrears aa
                INNER JOIN SavingPlans sp ON aa.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE aa.RemainingAmount > 0
                  AND m.MemberType != 'BehindAssociation'
                ORDER BY m.Name";
            
            return connection.Query<AccumulatedArrears>(sql);
        }
        
        /// <summary>
        /// إضافة سابقات متراكمة جديدة
        /// </summary>
        public int Add(AccumulatedArrears arrear)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO AccumulatedArrears 
                (PlanID, LastWeekNumber, TotalArrears, PaidAmount, RemainingAmount, IsPaid, CreatedDate, LastUpdated)
                VALUES 
                (@PlanID, @LastWeekNumber, @TotalArrears, @PaidAmount, @RemainingAmount, @IsPaid, @CreatedDate, @LastUpdated);
                SELECT last_insert_rowid();";
            
            return connection.QuerySingle<int>(sql, arrear);
        }
        
        /// <summary>
        /// تحديث السابقات المتراكمة
        /// </summary>
        public bool Update(AccumulatedArrears arrear)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                UPDATE AccumulatedArrears 
                SET LastWeekNumber = @LastWeekNumber,
                    TotalArrears = @TotalArrears,
                    PaidAmount = @PaidAmount,
                    RemainingAmount = @RemainingAmount,
                    IsPaid = @IsPaid,
                    LastUpdated = @LastUpdated
                WHERE AccumulatedArrearID = @AccumulatedArrearID";
            
            return connection.Execute(sql, arrear) > 0;
        }
        
        /// <summary>
        /// حذف السابقات المتراكمة لعضو
        /// </summary>
        public bool Delete(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "DELETE FROM AccumulatedArrears WHERE PlanID = @PlanID";
            return connection.Execute(sql, new { PlanID = planId }) > 0;
        }
    }
}
