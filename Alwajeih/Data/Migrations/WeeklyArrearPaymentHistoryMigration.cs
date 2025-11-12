using System.Data.SQLite;

namespace Alwajeih.Data.Migrations
{
    /// <summary>
    /// Migration لإنشاء جدول تسجيل تاريخ مدفوعات السابقات الأسبوعية
    /// يستخدم لكشف حساب العضو - كم دفع كل أسبوع من السابقات
    /// </summary>
    public static class WeeklyArrearPaymentHistoryMigration
    {
        public static void Up(SQLiteConnection connection)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS WeeklyArrearPaymentHistory (
                    HistoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanID INTEGER NOT NULL,
                    WeekNumber INTEGER NOT NULL,
                    PaymentDate DATETIME NOT NULL,
                    AmountPaid DECIMAL(18, 2) NOT NULL,
                    RemainingBeforePayment DECIMAL(18, 2) NOT NULL,
                    RemainingAfterPayment DECIMAL(18, 2) NOT NULL,
                    Notes TEXT,
                    RecordedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID)
                );

                CREATE INDEX IF NOT EXISTS idx_payment_history_plan 
                ON WeeklyArrearPaymentHistory(PlanID);
                
                CREATE INDEX IF NOT EXISTS idx_payment_history_week 
                ON WeeklyArrearPaymentHistory(WeekNumber);
            ";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public static void Down(SQLiteConnection connection)
        {
            string sql = @"
                DROP INDEX IF EXISTS idx_payment_history_week;
                DROP INDEX IF EXISTS idx_payment_history_plan;
                DROP TABLE IF EXISTS WeeklyArrearPaymentHistory;
            ";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public static void RunMigration()
        {
            using var connection = new SQLiteConnection(DatabaseContext.GetConnectionString());
            connection.Open();
            Up(connection);
        }
    }
}
