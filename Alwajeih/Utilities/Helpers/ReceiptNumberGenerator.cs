using System;
using System.Data.SQLite;
using Alwajeih.Data;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مولد أرقام الإيصالات
    /// </summary>
    public static class ReceiptNumberGenerator
    {
        /// <summary>
        /// توليد رقم إيصال جديد بصيغة: REC-YYYY-0001
        /// </summary>
        public static string GenerateReceiptNumber()
        {
            string year = DateTime.Now.ToString("yyyy");
            string prefix = $"REC-{year}-";

            using var connection = DatabaseContext.CreateConnection();
            connection.Open();

            // الحصول على آخر رقم إيصال من جدول DailyCollections
            string sql = @"
                SELECT ReceiptNumber 
                FROM DailyCollections 
                WHERE ReceiptNumber LIKE @Prefix 
                AND IsCancelled = 0
                ORDER BY CollectionID DESC 
                LIMIT 1";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Prefix", prefix + "%");

            var lastReceipt = command.ExecuteScalar()?.ToString();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastReceipt))
            {
                // استخراج الرقم من آخر إيصال
                var parts = lastReceipt.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}
