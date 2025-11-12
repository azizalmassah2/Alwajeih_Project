using System;
using System.Collections.Generic;
using System.Linq;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    /// <summary>
    /// مستودع مدفوعات السابقات المتراكمة
    /// </summary>
    public class AccumulatedArrearPaymentRepository
    {
        /// <summary>
        /// إنشاء الجدول إذا لم يكن موجوداً
        /// </summary>
        private void EnsureTableExists(System.Data.SQLite.SQLiteConnection connection)
        {
            Migrations.AccumulatedArrearPaymentsMigration.CreateTable(connection);
        }
        
        /// <summary>
        /// الحصول على جميع مدفوعات السابقات لعضو معين
        /// </summary>
        public IEnumerable<AccumulatedArrearPayment> GetByPlanId(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            EnsureTableExists(connection);
            
            string sql = @"
                SELECT aap.*, 
                       m.Name as MemberName,
                       sp.PlanNumber
                FROM AccumulatedArrearPayments aap
                INNER JOIN SavingPlans sp ON aap.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE aap.PlanID = @PlanID
                ORDER BY aap.WeekNumber, aap.DayNumber";
            
            return connection.Query<AccumulatedArrearPayment>(sql, new { PlanID = planId });
        }
        
        /// <summary>
        /// الحصول على مدفوعات السابقات لأسبوع معين
        /// </summary>
        public IEnumerable<AccumulatedArrearPayment> GetByWeek(int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            EnsureTableExists(connection);
            
            string sql = @"
                SELECT aap.*, 
                       m.Name as MemberName,
                       sp.PlanNumber
                FROM AccumulatedArrearPayments aap
                INNER JOIN SavingPlans sp ON aap.PlanID = sp.PlanID
                INNER JOIN Members m ON sp.MemberID = m.MemberID
                WHERE aap.WeekNumber = @WeekNumber
                ORDER BY m.Name";
            
            return connection.Query<AccumulatedArrearPayment>(sql, new { WeekNumber = weekNumber });
        }
        
        /// <summary>
        /// إضافة دفعة سابقات جديدة
        /// </summary>
        public int Add(AccumulatedArrearPayment payment)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            EnsureTableExists(connection);
            
            string sql = @"
                INSERT INTO AccumulatedArrearPayments 
                (PlanID, WeekNumber, DayNumber, AmountPaid, PaymentDate, RecordedBy, Notes)
                VALUES 
                (@PlanID, @WeekNumber, @DayNumber, @AmountPaid, @PaymentDate, @RecordedBy, @Notes);
                SELECT last_insert_rowid();";
            
            return connection.QuerySingle<int>(sql, payment);
        }
        
        /// <summary>
        /// حساب إجمالي المدفوعات لعضو معين
        /// </summary>
        public decimal GetTotalPaidByPlan(int planId)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            EnsureTableExists(connection);
            
            string sql = @"
                SELECT COALESCE(SUM(AmountPaid), 0)
                FROM AccumulatedArrearPayments
                WHERE PlanID = @PlanID";
            
            return connection.QuerySingle<decimal>(sql, new { PlanID = planId });
        }
    }
}
