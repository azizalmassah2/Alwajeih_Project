using System;
using System.Data.SQLite;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Data.Migrations
{
    /// <summary>
    /// ترحيل قاعدة البيانات - تحديث جداول المتأخرات والسابقات
    /// </summary>
    public static class ArrearsMigration
    {
        /// <summary>
        /// تحديث جدول DailyArrears لإضافة WeekNumber و DayNumber
        /// </summary>
        public static void UpdateDailyArrearsTable()
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();

            try
            {
                // إضافة WeekNumber إذا لم يكن موجوداً
                AddColumnIfNotExists(connection, "DailyArrears", "WeekNumber", "INTEGER DEFAULT 0");
                
                // إضافة DayNumber إذا لم يكن موجوداً
                AddColumnIfNotExists(connection, "DailyArrears", "DayNumber", "INTEGER DEFAULT 0");

                System.Diagnostics.Debug.WriteLine("✅ تم تحديث جدول DailyArrears بنجاح");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحديث DailyArrears: {ex.Message}");
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// تحديث جدول PreviousArrears لإضافة الحقول الجديدة
        /// </summary>
        public static void UpdatePreviousArrearsTable()
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();

            try
            {
                // إضافة WeekNumber إذا لم يكن موجوداً
                AddColumnIfNotExists(connection, "PreviousArrears", "WeekNumber", "INTEGER DEFAULT 0");
                
                // إضافة IsPaid إذا لم يكن موجوداً
                AddColumnIfNotExists(connection, "PreviousArrears", "IsPaid", "INTEGER DEFAULT 0");
                
                // إضافة PaidDate إذا لم يكن موجوداً
                AddColumnIfNotExists(connection, "PreviousArrears", "PaidDate", "TEXT");
                
                // إضافة PaidAmount إذا لم يكن موجوداً
                AddColumnIfNotExists(connection, "PreviousArrears", "PaidAmount", "REAL DEFAULT 0");
                
                // إضافة RemainingAmount إذا لم يكن موجوداً
                AddColumnIfNotExists(connection, "PreviousArrears", "RemainingAmount", "REAL DEFAULT 0");
                
                // إضافة CreatedDate إذا لم يكن موجوداً (بدون DEFAULT لأن SQLite لا يدعم CURRENT_TIMESTAMP في ALTER TABLE)
                AddColumnIfNotExists(connection, "PreviousArrears", "CreatedDate", "TEXT");

                // إزالة قيد UNIQUE من PlanID لأن كل أسبوع سابق له سجل منفصل
                // سنحتاج لإعادة إنشاء الجدول
                RecreateTableWithoutUniqueConstraint(connection);

                System.Diagnostics.Debug.WriteLine("✅ تم تحديث جدول PreviousArrears بنجاح");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحديث PreviousArrears: {ex.Message}");
            }
            finally
            {
                connection.Close();
            }
        }

        private static void RecreateTableWithoutUniqueConstraint(SQLiteConnection connection)
        {
            try
            {
                // إنشاء جدول مؤقت
                string createTempTable = @"
                    CREATE TABLE IF NOT EXISTS PreviousArrears_Temp (
                        PreviousArrearID INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlanID INTEGER NOT NULL,
                        WeekNumber INTEGER DEFAULT 0,
                        TotalArrears REAL DEFAULT 0,
                        IsPaid INTEGER DEFAULT 0,
                        PaidDate TEXT,
                        PaidAmount REAL DEFAULT 0,
                        RemainingAmount REAL DEFAULT 0,
                        CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                        LastUpdated TEXT DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID),
                        UNIQUE(PlanID, WeekNumber)
                    )";
                ExecuteNonQuery(connection, createTempTable);

                // نسخ البيانات
                string copyData = @"
                    INSERT INTO PreviousArrears_Temp (PreviousArrearID, PlanID, WeekNumber, TotalArrears, IsPaid, PaidDate, PaidAmount, RemainingAmount, CreatedDate, LastUpdated)
                    SELECT 
                        PreviousArrearID, 
                        PlanID, 
                        COALESCE(WeekNumber, 0), 
                        TotalArrears, 
                        COALESCE(IsPaid, 0),
                        PaidDate,
                        COALESCE(PaidAmount, 0),
                        COALESCE(RemainingAmount, TotalArrears),
                        COALESCE(CreatedDate, CURRENT_TIMESTAMP),
                        LastUpdated
                    FROM PreviousArrears";
                ExecuteNonQuery(connection, copyData);

                // حذف الجدول القديم
                ExecuteNonQuery(connection, "DROP TABLE PreviousArrears");

                // إعادة تسمية الجدول المؤقت
                ExecuteNonQuery(connection, "ALTER TABLE PreviousArrears_Temp RENAME TO PreviousArrears");

                System.Diagnostics.Debug.WriteLine("✅ تم إعادة إنشاء جدول PreviousArrears بنجاح");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إعادة إنشاء الجدول: {ex.Message}");
                throw;
            }
        }

        private static void AddColumnIfNotExists(SQLiteConnection connection, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                // التحقق من وجود العمود
                string checkSql = $"PRAGMA table_info({tableName})";
                using var cmd = new SQLiteCommand(checkSql, connection);
                using var reader = cmd.ExecuteReader();

                bool columnExists = false;
                while (reader.Read())
                {
                    if (reader["name"].ToString() == columnName)
                    {
                        columnExists = true;
                        break;
                    }
                }

                // إضافة العمود إذا لم يكن موجوداً
                if (!columnExists)
                {
                    string alterSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                    ExecuteNonQuery(connection, alterSql);
                    System.Diagnostics.Debug.WriteLine($"✅ تمت إضافة العمود {columnName} إلى {tableName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ℹ️ العمود {columnName} موجود بالفعل في {tableName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إضافة العمود {columnName}: {ex.Message}");
                throw;
            }
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, string sql)
        {
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// تنفيذ جميع الترحيلات
        /// </summary>
        public static void RunAllMigrations()
        {
            UpdateDailyArrearsTable();
            UpdatePreviousArrearsTable();
        }
    }
}
