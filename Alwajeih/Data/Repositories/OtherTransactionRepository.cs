using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Alwajeih.Models;

namespace Alwajeih.Data.Repositories
{
    /// <summary>
    /// مستودع الخرجيات والمفقودات
    /// </summary>
    public class OtherTransactionRepository
    {
        /// <summary>
        /// إضافة عملية جديدة
        /// </summary>
        public int Add(OtherTransaction transaction)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            // إنشاء الجدول إذا لم يكن موجوداً
            CreateTableIfNotExists(connection);
            
            string sql = @"
                INSERT INTO OtherTransactions (
                    TransactionType, Amount, PlanID, MemberName, 
                    WeekNumber, DayNumber, TransactionDate, Notes,
                    CreatedBy, CreatedAt, IsCancelled
                ) VALUES (
                    @TransactionType, @Amount, @PlanID, @MemberName,
                    @WeekNumber, @DayNumber, @TransactionDate, @Notes,
                    @CreatedBy, @CreatedAt, 0
                );
                SELECT last_insert_rowid();";
            
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@TransactionType", transaction.TransactionType);
            command.Parameters.AddWithValue("@Amount", transaction.Amount);
            command.Parameters.AddWithValue("@PlanID", transaction.PlanID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MemberName", transaction.MemberName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@WeekNumber", transaction.WeekNumber);
            command.Parameters.AddWithValue("@DayNumber", transaction.DayNumber);
            command.Parameters.AddWithValue("@TransactionDate", transaction.TransactionDate);
            command.Parameters.AddWithValue("@Notes", transaction.Notes ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedBy", transaction.CreatedBy);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            
            return Convert.ToInt32(command.ExecuteScalar());
        }
        
        /// <summary>
        /// الحصول على عمليات أسبوع ويوم معين
        /// </summary>
        public List<OtherTransaction> GetByWeekAndDay(int weekNumber, int dayNumber)
        {
            var transactions = new List<OtherTransaction>();
            
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            // إنشاء الجدول إذا لم يكن موجوداً
            CreateTableIfNotExists(connection);
            
            string sql = @"
                SELECT * FROM OtherTransactions 
                WHERE WeekNumber = @WeekNumber 
                AND DayNumber = @DayNumber 
                AND IsCancelled = 0
                ORDER BY CreatedAt DESC";
            
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            command.Parameters.AddWithValue("@DayNumber", dayNumber);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapFromReader(reader));
            }
            
            return transactions;
        }
        
        /// <summary>
        /// الحصول على عمليات أسبوع معين
        /// </summary>
        public List<OtherTransaction> GetByWeek(int weekNumber)
        {
            var transactions = new List<OtherTransaction>();
            
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            // إنشاء الجدول إذا لم يكن موجوداً
            CreateTableIfNotExists(connection);
            
            string sql = @"
                SELECT * FROM OtherTransactions 
                WHERE WeekNumber = @WeekNumber 
                AND IsCancelled = 0
                ORDER BY TransactionDate DESC";
            
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapFromReader(reader));
            }
            
            return transactions;
        }
        
        /// <summary>
        /// حساب إجمالي عمليات أسبوع معين
        /// </summary>
        public decimal GetTotalByWeek(int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            // إنشاء الجدول إذا لم يكن موجوداً
            CreateTableIfNotExists(connection);
            
            string sql = @"
                SELECT COALESCE(SUM(Amount), 0) 
                FROM OtherTransactions 
                WHERE WeekNumber = @WeekNumber 
                AND IsCancelled = 0";
            
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            
            return Convert.ToDecimal(command.ExecuteScalar());
        }
        
        /// <summary>
        /// حساب إجمالي عمليات يوم معين
        /// </summary>
        public decimal GetTotalByWeekAndDay(int weekNumber, int dayNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            // إنشاء الجدول إذا لم يكن موجوداً
            CreateTableIfNotExists(connection);
            
            string sql = @"
                SELECT COALESCE(SUM(Amount), 0) 
                FROM OtherTransactions 
                WHERE WeekNumber = @WeekNumber 
                AND DayNumber = @DayNumber 
                AND IsCancelled = 0";
            
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            command.Parameters.AddWithValue("@DayNumber", dayNumber);
            
            return Convert.ToDecimal(command.ExecuteScalar());
        }
        
        /// <summary>
        /// إلغاء عملية
        /// </summary>
        public void Cancel(int transactionId, string reason)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            string sql = @"
                UPDATE OtherTransactions 
                SET IsCancelled = 1, CancellationReason = @Reason 
                WHERE TransactionID = @TransactionID";
            
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@TransactionID", transactionId);
            command.Parameters.AddWithValue("@Reason", reason);
            
            command.ExecuteNonQuery();
        }
        
        /// <summary>
        /// إنشاء الجدول إذا لم يكن موجوداً
        /// </summary>
        private void CreateTableIfNotExists(SQLiteConnection connection)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS OtherTransactions (
                    TransactionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionType TEXT NOT NULL,
                    Amount DECIMAL(18, 2) NOT NULL,
                    PlanID INTEGER NULL,
                    MemberName TEXT NULL,
                    WeekNumber INTEGER NOT NULL,
                    DayNumber INTEGER NOT NULL,
                    TransactionDate DATE NOT NULL,
                    Notes TEXT NULL,
                    CreatedBy INTEGER NOT NULL,
                    CreatedAt DATETIME NOT NULL,
                    IsCancelled INTEGER NOT NULL DEFAULT 0,
                    CancellationReason TEXT NULL,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID),
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
                )";
            
            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
        
        /// <summary>
        /// تحويل من DataReader
        /// </summary>
        private OtherTransaction MapFromReader(SQLiteDataReader reader)
        {
            return new OtherTransaction
            {
                TransactionID = reader.GetInt32(reader.GetOrdinal("TransactionID")),
                TransactionType = reader.GetString(reader.GetOrdinal("TransactionType")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                PlanID = reader.IsDBNull(reader.GetOrdinal("PlanID")) ? null : reader.GetInt32(reader.GetOrdinal("PlanID")),
                MemberName = reader.IsDBNull(reader.GetOrdinal("MemberName")) ? null : reader.GetString(reader.GetOrdinal("MemberName")),
                WeekNumber = reader.GetInt32(reader.GetOrdinal("WeekNumber")),
                DayNumber = reader.GetInt32(reader.GetOrdinal("DayNumber")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsCancelled = reader.GetInt32(reader.GetOrdinal("IsCancelled")) == 1,
                CancellationReason = reader.IsDBNull(reader.GetOrdinal("CancellationReason")) ? null : reader.GetString(reader.GetOrdinal("CancellationReason"))
            };
        }
    }
}
