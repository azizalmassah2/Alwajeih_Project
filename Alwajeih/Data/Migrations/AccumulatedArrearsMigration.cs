using System;
using System.Data.SQLite;
using Alwajeih.Data;

namespace Alwajeih.Data.Migrations
{
    /// <summary>
    /// Migration لإنشاء جدول السابقات المتراكمة
    /// </summary>
    public static class AccumulatedArrearsMigration
    {
        public static void RunMigration()
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();

            // إنشاء الجدول
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS AccumulatedArrears (
                    AccumulatedArrearID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanID INTEGER NOT NULL UNIQUE,
                    LastWeekNumber INTEGER NOT NULL DEFAULT 0,
                    TotalArrears REAL NOT NULL DEFAULT 0,
                    PaidAmount REAL NOT NULL DEFAULT 0,
                    RemainingAmount REAL NOT NULL DEFAULT 0,
                    IsPaid INTEGER NOT NULL DEFAULT 0,
                    CreatedDate TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID) ON DELETE CASCADE
                );";

            using (var command = new SQLiteCommand(createTableSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // إنشاء index للبحث السريع
            string createIndexSql = @"
                CREATE INDEX IF NOT EXISTS idx_accumulated_arrears_planid 
                ON AccumulatedArrears(PlanID);
                
                CREATE INDEX IF NOT EXISTS idx_accumulated_arrears_remaining 
                ON AccumulatedArrears(RemainingAmount);";

            using (var command = new SQLiteCommand(createIndexSql, connection))
            {
                command.ExecuteNonQuery();
            }

            Console.WriteLine("✅ تم إنشاء جدول AccumulatedArrears بنجاح");
        }
    }
}
