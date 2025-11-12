using System;
using System.Data.SQLite;
using Alwajeih.Data;

namespace Alwajeih.Data.Migrations
{
    /// <summary>
    /// Migration لإضافة حقل PreviousArrearPaymentAmount في جدول DailyCollections
    /// لتخزين مبلغ سداد السابقات بشكل منفصل عن التحصيل العادي
    /// </summary>
    public static class AddPreviousArrearPaymentAmountMigration
    {
        public static void RunMigration()
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();

            try
            {
                // التحقق من وجود العمود
                string checkColumnSql = @"
                    SELECT COUNT(*) 
                    FROM pragma_table_info('DailyCollections') 
                    WHERE name='PreviousArrearPaymentAmount';";

                using (var checkCommand = new SQLiteCommand(checkColumnSql, connection))
                {
                    var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!exists)
                    {
                        // إضافة العمود الجديد
                        string addColumnSql = @"
                            ALTER TABLE DailyCollections 
                            ADD COLUMN PreviousArrearPaymentAmount REAL NOT NULL DEFAULT 0;";

                        using (var addCommand = new SQLiteCommand(addColumnSql, connection))
                        {
                            addCommand.ExecuteNonQuery();
                        }

                        Console.WriteLine("✅ تم إضافة حقل PreviousArrearPaymentAmount إلى جدول DailyCollections");
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ حقل PreviousArrearPaymentAmount موجود مسبقاً في جدول DailyCollections");
                    }
                }
                
                // التحقق من وجود حقل IsPreviousArrearPayment
                string checkBoolColumnSql = @"
                    SELECT COUNT(*) 
                    FROM pragma_table_info('DailyCollections') 
                    WHERE name='IsPreviousArrearPayment';";

                using (var checkCommand = new SQLiteCommand(checkBoolColumnSql, connection))
                {
                    var existsBool = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!existsBool)
                    {
                        // إضافة العمود الجديد
                        string addBoolColumnSql = @"
                            ALTER TABLE DailyCollections 
                            ADD COLUMN IsPreviousArrearPayment INTEGER NOT NULL DEFAULT 0;";

                        using (var addCommand = new SQLiteCommand(addBoolColumnSql, connection))
                        {
                            addCommand.ExecuteNonQuery();
                        }

                        Console.WriteLine("✅ تم إضافة حقل IsPreviousArrearPayment إلى جدول DailyCollections");
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ حقل IsPreviousArrearPayment موجود مسبقاً في جدول DailyCollections");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في Migration: {ex.Message}");
                throw;
            }
        }
    }
}
