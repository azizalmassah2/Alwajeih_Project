using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Alwajeih.Models;
using Alwajeih.Models.BehindAssociation;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Data.Repositories.BehindAssociation
{
    /// <summary>
    /// مستودع بيانات أعضاء "خلف الجمعية"
    /// </summary>
    public class BehindAssociationRepository
    {
        /// <summary>
        /// إضافة معاملة دفع لعضو خلف الجمعية
        /// </summary>
        public int AddTransaction(BehindAssociationTransaction transaction)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                INSERT INTO BehindAssociationTransactions 
                (MemberID, TransactionType, Amount, TransactionDate, WeekNumber, DayNumber, 
                 PaymentSource, ReferenceNumber, Notes, RecordedBy, RecordedAt)
                VALUES 
                (@MemberID, @TransactionType, @Amount, @TransactionDate, @WeekNumber, @DayNumber,
                 @PaymentSource, @ReferenceNumber, @Notes, @RecordedBy, @RecordedAt);
                SELECT last_insert_rowid();";
            
            command.Parameters.AddWithValue("@MemberID", transaction.MemberID);
            command.Parameters.AddWithValue("@TransactionType", transaction.TransactionType.ToString());
            command.Parameters.AddWithValue("@Amount", transaction.Amount);
            command.Parameters.AddWithValue("@TransactionDate", transaction.TransactionDate);
            command.Parameters.AddWithValue("@WeekNumber", transaction.WeekNumber);
            command.Parameters.AddWithValue("@DayNumber", transaction.DayNumber);
            command.Parameters.AddWithValue("@PaymentSource", transaction.PaymentSource.ToString());
            command.Parameters.AddWithValue("@ReferenceNumber", transaction.ReferenceNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Notes", transaction.Notes ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RecordedBy", transaction.RecordedBy);
            command.Parameters.AddWithValue("@RecordedAt", transaction.RecordedAt);
            
            return Convert.ToInt32(command.ExecuteScalar());
        }
        
        /// <summary>
        /// الحصول على جميع معاملات عضو
        /// </summary>
        public List<BehindAssociationTransaction> GetMemberTransactions(int memberId)
        {
            var transactions = new List<BehindAssociationTransaction>();
            
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(@"
                SELECT t.*, m.Name as MemberName
                FROM BehindAssociationTransactions t
                INNER JOIN Members m ON t.MemberID = m.MemberID
                WHERE t.MemberID = @MemberID 
                  AND t.IsCancelled = 0
                ORDER BY t.TransactionDate DESC, t.TransactionID DESC", connection);
            
            command.Parameters.AddWithValue("@MemberID", memberId);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapTransaction(reader));
            }
            
            return transactions;
        }
        
        /// <summary>
        /// الحصول على جميع المعاملات
        /// </summary>
        public List<BehindAssociationTransaction> GetAllTransactions()
        {
            var transactions = new List<BehindAssociationTransaction>();
            
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(@"
                SELECT t.*, m.Name as MemberName
                FROM BehindAssociationTransactions t
                INNER JOIN Members m ON t.MemberID = m.MemberID
                WHERE t.IsCancelled = 0
                ORDER BY t.TransactionDate DESC, t.TransactionID DESC", connection);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapTransaction(reader));
            }
            
            return transactions;
        }
        
        /// <summary>
        /// الحصول على معاملات أسبوع معين (لأغراض الجرد)
        /// </summary>
        public List<BehindAssociationTransaction> GetWeekTransactions(int weekNumber)
        {
            var transactions = new List<BehindAssociationTransaction>();
            
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(@"
                SELECT t.*, m.Name as MemberName
                FROM BehindAssociationTransactions t
                INNER JOIN Members m ON t.MemberID = m.MemberID
                WHERE t.WeekNumber = @WeekNumber 
                  AND t.IsCancelled = 0
                  AND t.TransactionType = 'Deposit'
                ORDER BY t.TransactionDate, t.TransactionID", connection);
            
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapTransaction(reader));
            }
            
            return transactions;
        }
        
        /// <summary>
        /// الحصول على معاملات يوم معين (لأغراض الملخص اليومي)
        /// </summary>
        public List<BehindAssociationTransaction> GetDayTransactions(int weekNumber, int dayNumber)
        {
            var transactions = new List<BehindAssociationTransaction>();
            
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(@"
                SELECT t.*, m.Name as MemberName
                FROM BehindAssociationTransactions t
                INNER JOIN Members m ON t.MemberID = m.MemberID
                WHERE t.WeekNumber = @WeekNumber 
                  AND t.DayNumber = @DayNumber
                  AND t.IsCancelled = 0
                  AND t.TransactionType = 'Deposit'
                ORDER BY t.TransactionDate, t.TransactionID", connection);
            
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            command.Parameters.AddWithValue("@DayNumber", dayNumber);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapTransaction(reader));
            }
            
            return transactions;
        }
        
        /// <summary>
        /// حساب ملخص حساب عضو
        /// </summary>
        public BehindAssociationSummary GetMemberSummary(int memberId)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            // الحصول على معلومات العضو
            var member = GetMemberInfo(connection, memberId);
            if (member == null)
                return null;
            
            var summary = new BehindAssociationSummary
            {
                MemberID = memberId,
                MemberName = member.Name,
                Phone = member.Phone
            };
            
            // حساب إجمالي الإيداعات من جدول BehindAssociationTransactions
            using (var command = new SQLiteCommand(@"
                SELECT 
                    COALESCE(SUM(Amount), 0) as TotalDeposits,
                    COUNT(*) as TransactionCount,
                    MAX(TransactionDate) as LastDepositDate
                FROM BehindAssociationTransactions
                WHERE MemberID = @MemberID 
                  AND TransactionType = 'Deposit'
                  AND IsCancelled = 0", connection))
            {
                command.Parameters.AddWithValue("@MemberID", memberId);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    summary.TotalDeposits = reader.GetDecimal(0);
                    summary.TransactionCount = reader.GetInt32(1);
                    if (!reader.IsDBNull(2))
                        summary.LastDepositDate = reader.GetDateTime(2);
                }
            }
            
            // الحصول على مبلغ آخر دفعة
            using (var command = new SQLiteCommand(@"
                SELECT Amount 
                FROM BehindAssociationTransactions
                WHERE MemberID = @MemberID 
                  AND TransactionType = 'Deposit'
                  AND IsCancelled = 0
                ORDER BY TransactionDate DESC, TransactionID DESC
                LIMIT 1", connection))
            {
                command.Parameters.AddWithValue("@MemberID", memberId);
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    summary.LastDepositAmount = Convert.ToDecimal(result);
            }
            
            // حساب إجمالي السحوبات من جدول BehindAssociationTransactions
            using (var command = new SQLiteCommand(@"
                SELECT 
                    COALESCE(SUM(Amount), 0) as TotalWithdrawals,
                    MAX(TransactionDate) as LastWithdrawalDate
                FROM BehindAssociationTransactions
                WHERE MemberID = @MemberID 
                  AND TransactionType = 'Withdrawal'
                  AND IsCancelled = 0", connection))
            {
                command.Parameters.AddWithValue("@MemberID", memberId);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    summary.TotalWithdrawals = reader.GetDecimal(0);
                    if (!reader.IsDBNull(1))
                        summary.LastWithdrawalDate = reader.GetDateTime(1);
                }
            }
            
            // الحصول على مبلغ آخر سحب
            using (var command = new SQLiteCommand(@"
                SELECT Amount
                FROM BehindAssociationTransactions
                WHERE MemberID = @MemberID 
                  AND TransactionType = 'Withdrawal'
                  AND IsCancelled = 0
                ORDER BY TransactionDate DESC, TransactionID DESC
                LIMIT 1", connection))
            {
                command.Parameters.AddWithValue("@MemberID", memberId);
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    summary.LastWithdrawalAmount = Convert.ToDecimal(result);
            }
            
            // الحصول على تاريخ أول معاملة
            using (var command = new SQLiteCommand(@"
                SELECT MIN(TransactionDate)
                FROM BehindAssociationTransactions
                WHERE MemberID = @MemberID AND IsCancelled = 0", connection))
            {
                command.Parameters.AddWithValue("@MemberID", memberId);
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    summary.FirstTransactionDate = Convert.ToDateTime(result);
            }
            
            return summary;
        }
        
        /// <summary>
        /// الحصول على جميع ملخصات الأعضاء
        /// </summary>
        public List<BehindAssociationSummary> GetAllMembersSummaries()
        {
            var summaries = new List<BehindAssociationSummary>();
            
            // الحصول على جميع أعضاء خلف الجمعية
            var memberRepository = new MemberRepository();
            var members = memberRepository.GetAll()
                .Where(m => m.MemberType == MemberType.BehindAssociation && !m.IsArchived)
                .ToList();
            
            foreach (var member in members)
            {
                var summary = GetMemberSummary(member.MemberID);
                if (summary != null)
                    summaries.Add(summary);
            }
            
            return summaries.OrderByDescending(s => s.CurrentBalance).ToList();
        }
        
        /// <summary>
        /// إلغاء معاملة
        /// </summary>
        public bool CancelTransaction(int transactionId, string reason, int userId)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(@"
                UPDATE BehindAssociationTransactions
                SET IsCancelled = 1, CancellationReason = @Reason
                WHERE TransactionID = @TransactionID", connection);
            
            command.Parameters.AddWithValue("@TransactionID", transactionId);
            command.Parameters.AddWithValue("@Reason", reason);
            
            return command.ExecuteNonQuery() > 0;
        }
        
        /// <summary>
        /// حساب إجمالي دفعات أسبوع معين
        /// </summary>
        public decimal GetWeekTotalDeposits(int weekNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(@"
                SELECT COALESCE(SUM(Amount), 0)
                FROM BehindAssociationTransactions
                WHERE WeekNumber = @WeekNumber 
                  AND TransactionType = 'Deposit'
                  AND IsCancelled = 0", connection);
            
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToDecimal(result) : 0;
        }
        
        /// <summary>
        /// حساب إجمالي دفعات يوم معين
        /// </summary>
        public decimal GetDayTotalDeposits(int weekNumber, int dayNumber)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            using var command = new SQLiteCommand(@"
                SELECT COALESCE(SUM(Amount), 0)
                FROM BehindAssociationTransactions
                WHERE WeekNumber = @WeekNumber 
                  AND DayNumber = @DayNumber
                  AND TransactionType = 'Deposit'
                  AND IsCancelled = 0", connection);
            
            command.Parameters.AddWithValue("@WeekNumber", weekNumber);
            command.Parameters.AddWithValue("@DayNumber", dayNumber);
            
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToDecimal(result) : 0;
        }
        
        /// <summary>
        /// حساب إجمالي رصيد جميع الأعضاء (لأغراض التقارير)
        /// </summary>
        public decimal GetTotalBalance()
        {
            var summaries = GetAllMembersSummaries();
            return summaries.Sum(s => s.CurrentBalance);
        }
        
        // ============= Helper Methods =============
        
        private BehindAssociationTransaction MapTransaction(SQLiteDataReader reader)
        {
            return new BehindAssociationTransaction
            {
                TransactionID = reader.GetInt32(reader.GetOrdinal("TransactionID")),
                MemberID = reader.GetInt32(reader.GetOrdinal("MemberID")),
                TransactionType = Enum.Parse<BehindAssociationTransactionType>(reader.GetString(reader.GetOrdinal("TransactionType"))),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                WeekNumber = reader.GetInt32(reader.GetOrdinal("WeekNumber")),
                DayNumber = reader.GetInt32(reader.GetOrdinal("DayNumber")),
                PaymentSource = Enum.Parse<PaymentSource>(reader.GetString(reader.GetOrdinal("PaymentSource"))),
                ReferenceNumber = reader.IsDBNull(reader.GetOrdinal("ReferenceNumber")) ? null : reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                RecordedBy = reader.GetInt32(reader.GetOrdinal("RecordedBy")),
                RecordedAt = reader.GetDateTime(reader.GetOrdinal("RecordedAt")),
                IsCancelled = reader.GetBoolean(reader.GetOrdinal("IsCancelled")),
                CancellationReason = reader.IsDBNull(reader.GetOrdinal("CancellationReason")) ? null : reader.GetString(reader.GetOrdinal("CancellationReason")),
                MemberName = reader.GetString(reader.GetOrdinal("MemberName"))
            };
        }
        
        private Member GetMemberInfo(SQLiteConnection connection, int memberId)
        {
            using var command = new SQLiteCommand(@"
                SELECT MemberID, Name, Phone
                FROM Members
                WHERE MemberID = @MemberID", connection);
            
            command.Parameters.AddWithValue("@MemberID", memberId);
            
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Member
                {
                    MemberID = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Phone = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }
            
            return null;
        }
    }
}
