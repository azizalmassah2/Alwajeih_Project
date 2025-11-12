using System.Data.SQLite;

namespace Alwajeih.Data.Migrations
{
    /// <summary>
    /// Migration لإنشاء جدول مدفوعات السابقات المتراكمة
    /// </summary>
    public static class AccumulatedArrearPaymentsMigration
    {
        public static void CreateTable(SQLiteConnection connection)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS AccumulatedArrearPayments (
                    PaymentID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanID INTEGER NOT NULL,
                    WeekNumber INTEGER NOT NULL,
                    DayNumber INTEGER NOT NULL,
                    AmountPaid REAL NOT NULL DEFAULT 0,
                    PaymentDate TEXT NOT NULL,
                    RecordedBy INTEGER NOT NULL,
                    Notes TEXT,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID) ON DELETE CASCADE
                );
                
                CREATE INDEX IF NOT EXISTS idx_accumulated_payments_plan 
                ON AccumulatedArrearPayments(PlanID);
                
                CREATE INDEX IF NOT EXISTS idx_accumulated_payments_week 
                ON AccumulatedArrearPayments(WeekNumber);";
            
            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
