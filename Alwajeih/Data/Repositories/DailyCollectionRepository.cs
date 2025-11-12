using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Alwajeih.Models;

namespace Alwajeih.Data.Repositories
{
    /// <summary>
    /// مستودع التحصيلات اليومية
    /// </summary>
    public class DailyCollectionRepository
    {
        /// <summary>
        /// الحصول على جميع تحصيلات أسبوع معين
        /// </summary>
        public List<DailyCollection> GetCollectionsByWeek(int weekNumber)
        {
            var collections = new List<DailyCollection>();
            
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                    FROM DailyCollections dc
                    INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                    INNER JOIN Members m ON sp.MemberID = m.MemberID
                    WHERE dc.WeekNumber = @WeekNumber 
                    AND dc.IsCancelled = 0
                    ORDER BY dc.DayNumber, dc.CollectedAt";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WeekNumber", weekNumber);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collections.Add(MapFromReader(reader));
                        }
                    }
                }
            }
            
            return collections;
        }
        
        /// <summary>
        /// الحصول على جميع التحصيلات
        /// </summary>
        public List<DailyCollection> GetAll()
        {
            var collections = new List<DailyCollection>();
            
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                    FROM DailyCollections dc
                    INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                    INNER JOIN Members m ON sp.MemberID = m.MemberID
                    WHERE dc.IsCancelled = 0
                    ORDER BY dc.WeekNumber, dc.DayNumber, dc.CollectedAt";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collections.Add(MapFromReader(reader));
                        }
                    }
                }
            }
            
            return collections;
        }
        
        /// <summary>
        /// الحصول على تحصيلات يوم معين من أسبوع معين
        /// </summary>
        public List<DailyCollection> GetCollectionsByWeekAndDay(int weekNumber, int dayNumber)
        {
            var collections = new List<DailyCollection>();
            
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                    FROM DailyCollections dc
                    INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                    INNER JOIN Members m ON sp.MemberID = m.MemberID
                    WHERE dc.WeekNumber = @WeekNumber 
                    AND dc.DayNumber = @DayNumber
                    AND dc.IsCancelled = 0
                    ORDER BY dc.CollectedAt";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WeekNumber", weekNumber);
                    command.Parameters.AddWithValue("@DayNumber", dayNumber);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collections.Add(MapFromReader(reader));
                        }
                    }
                }
            }
            
            return collections;
        }
        
        /// <summary>
        /// الحصول على المتأخرات لأسبوع معين
        /// </summary>
        public List<DailyCollection> GetArrearsByWeek(int weekNumber)
        {
            var collections = new List<DailyCollection>();
            
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                    FROM DailyCollections dc
                    INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                    INNER JOIN Members m ON sp.MemberID = m.MemberID
                    WHERE dc.WeekNumber < @WeekNumber 
                    AND dc.IsCancelled = 0
                    AND dc.AmountPaid < sp.DailyAmount
                    ORDER BY dc.WeekNumber, dc.DayNumber";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WeekNumber", weekNumber);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collections.Add(MapFromReader(reader));
                        }
                    }
                }
            }
            
            return collections;
        }
        
        /// <summary>
        /// التحقق من وجود سداد مسبق لنفس الحصة في نفس الأسبوع واليوم
        /// </summary>
        public bool HasExistingPayment(int planId, int weekNumber, int dayNumber)
        {
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    SELECT COUNT(*) 
                    FROM DailyCollections 
                    WHERE PlanID = @PlanID 
                    AND WeekNumber = @WeekNumber 
                    AND DayNumber = @DayNumber
                    AND IsCancelled = 0";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlanID", planId);
                    command.Parameters.AddWithValue("@WeekNumber", weekNumber);
                    command.Parameters.AddWithValue("@DayNumber", dayNumber);
                    
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// الحصول على جميع التحصيلات لأسبوع ويوم معين
        /// </summary>
        public List<DailyCollection> GetByWeekAndDay(int weekNumber, int dayNumber)
        {
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                    FROM DailyCollections dc
                    INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                    INNER JOIN Members m ON sp.MemberID = m.MemberID
                    WHERE dc.WeekNumber = @WeekNumber 
                    AND dc.DayNumber = @DayNumber
                    AND dc.IsCancelled = 0";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WeekNumber", weekNumber);
                    command.Parameters.AddWithValue("@DayNumber", dayNumber);
                    
                    var collections = new List<DailyCollection>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collections.Add(MapFromReader(reader));
                        }
                    }
                    return collections;
                }
            }
        }

        /// <summary>
        /// الحصول على تحصيل معين حسب الحصة والأسبوع واليوم
        /// </summary>
        public DailyCollection? GetByPlanWeekDay(int planId, int weekNumber, int dayNumber)
        {
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    SELECT dc.*, m.Name as MemberName, sp.PlanNumber, sp.DailyAmount
                    FROM DailyCollections dc
                    INNER JOIN SavingPlans sp ON dc.PlanID = sp.PlanID
                    INNER JOIN Members m ON sp.MemberID = m.MemberID
                    WHERE dc.PlanID = @PlanID 
                    AND dc.WeekNumber = @WeekNumber 
                    AND dc.DayNumber = @DayNumber
                    AND dc.IsCancelled = 0
                    LIMIT 1";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlanID", planId);
                    command.Parameters.AddWithValue("@WeekNumber", weekNumber);
                    command.Parameters.AddWithValue("@DayNumber", dayNumber);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapFromReader(reader);
                        }
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// إضافة تحصيل جديد
        /// </summary>
        public int Add(DailyCollection collection)
        {
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    INSERT INTO DailyCollections (
                        PlanID, CollectionDate, WeekNumber, DayNumber, DayName,
                        AmountPaid, PaymentType, PaymentSource, ReferenceNumber,
                        ReceiptNumber, Notes, CollectedBy, CollectedAt
                    ) VALUES (
                        @PlanID, @CollectionDate, @WeekNumber, @DayNumber, @DayName,
                        @AmountPaid, @PaymentType, @PaymentSource, @ReferenceNumber,
                        @ReceiptNumber, @Notes, @CollectedBy, @CollectedAt
                    );
                    SELECT last_insert_rowid();";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlanID", collection.PlanID);
                    command.Parameters.AddWithValue("@CollectionDate", collection.CollectionDate);
                    command.Parameters.AddWithValue("@WeekNumber", collection.WeekNumber);
                    command.Parameters.AddWithValue("@DayNumber", collection.DayNumber);
                    command.Parameters.AddWithValue("@DayName", collection.DayName ?? string.Empty);
                    command.Parameters.AddWithValue("@AmountPaid", collection.AmountPaid);
                    command.Parameters.AddWithValue("@PaymentType", collection.PaymentType.ToString());
                    command.Parameters.AddWithValue("@PaymentSource", collection.PaymentSource.ToString());
                    command.Parameters.AddWithValue("@ReferenceNumber", collection.ReferenceNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ReceiptNumber", collection.ReceiptNumber ?? string.Empty);
                    command.Parameters.AddWithValue("@Notes", collection.Notes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CollectedBy", collection.CollectedBy);
                    command.Parameters.AddWithValue("@CollectedAt", DateTime.Now);
                    
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }
        
        /// <summary>
        /// تحديث تحصيل موجود
        /// </summary>
        public bool Update(DailyCollection collection)
        {
            using (var connection = DatabaseContext.CreateConnection())
            {
                connection.Open();
                string query = @"
                    UPDATE DailyCollections 
                    SET AmountPaid = @AmountPaid,
                        PaymentType = @PaymentType,
                        PaymentSource = @PaymentSource,
                        DayName = @DayName,
                        Notes = @Notes
                    WHERE CollectionID = @CollectionID";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AmountPaid", collection.AmountPaid);
                    command.Parameters.AddWithValue("@PaymentType", collection.PaymentType.ToString());
                    command.Parameters.AddWithValue("@PaymentSource", collection.PaymentSource.ToString());
                    command.Parameters.AddWithValue("@DayName", collection.DayName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Notes", collection.Notes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CollectionID", collection.CollectionID);
                    
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
        
        private DailyCollection MapFromReader(IDataReader reader)
        {
            return new DailyCollection
            {
                CollectionID = reader.GetInt32(reader.GetOrdinal("CollectionID")),
                PlanID = reader.GetInt32(reader.GetOrdinal("PlanID")),
                CollectionDate = reader.GetDateTime(reader.GetOrdinal("CollectionDate")),
                WeekNumber = reader.GetInt32(reader.GetOrdinal("WeekNumber")),
                DayNumber = reader.GetInt32(reader.GetOrdinal("DayNumber")),
                AmountPaid = reader.GetDecimal(reader.GetOrdinal("AmountPaid")),
                PaymentType = Enum.Parse<PaymentType>(reader.GetString(reader.GetOrdinal("PaymentType"))),
                PaymentSource = Enum.Parse<PaymentSource>(reader.GetString(reader.GetOrdinal("PaymentSource"))),
                ReferenceNumber = reader.IsDBNull(reader.GetOrdinal("ReferenceNumber")) ? null : reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                ReceiptNumber = reader.IsDBNull(reader.GetOrdinal("ReceiptNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiptNumber")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                CollectedBy = reader.GetInt32(reader.GetOrdinal("CollectedBy")),
                CollectedAt = reader.GetDateTime(reader.GetOrdinal("CollectedAt")),
                IsCancelled = reader.GetBoolean(reader.GetOrdinal("IsCancelled")),
                // خصائص إضافية
                MemberName = reader.GetString(reader.GetOrdinal("MemberName")),
                PlanNumber = reader.GetInt32(reader.GetOrdinal("PlanNumber")),
                DailyAmount = reader.GetDecimal(reader.GetOrdinal("DailyAmount"))
            };
        }
    }
    
    /// <summary>
    /// Extension method للتحقق من وجود حقل في IDataReader
    /// </summary>
    public static class DataReaderExtensions
    {
        public static bool FieldExists(this IDataReader reader, string fieldName)
        {
            try
            {
                return reader.GetOrdinal(fieldName) >= 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
