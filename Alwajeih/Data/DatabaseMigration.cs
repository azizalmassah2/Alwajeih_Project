using System;
using System.Data.SQLite;

namespace Alwajeih.Data
{
    /// <summary>
    /// إدارة ترحيل وتحديث قاعدة البيانات بشكل آمن
    /// </summary>
    public static class DatabaseMigration
    {
        /// <summary>
        /// تحديث قاعدة البيانات: إضافة حقل MemberType
        /// هذه الدالة آمنة ولن تحذف أي بيانات
        /// </summary>
        public static bool AddMemberTypeColumn()
        {
            try
            {
                using var connection = DatabaseContext.CreateConnection();
                connection.Open();

                // 1. التحقق من وجود العمود
                bool columnExists = CheckColumnExists(connection, "Members", "MemberType");
                
                if (columnExists)
                {
                    Console.WriteLine("✅ عمود MemberType موجود بالفعل - لا حاجة للتحديث");
                }
                else
                {
                    // 2. إضافة العمود الجديد
                    string sql = @"ALTER TABLE Members ADD COLUMN MemberType TEXT NOT NULL DEFAULT 'Regular'";
                    
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    Console.WriteLine("✅ تم إضافة عمود MemberType بنجاح");
                    
                    // 3. التحقق من البيانات
                    int totalMembers = GetTotalMembers(connection);
                    Console.WriteLine($"📊 عدد الأعضاء الحاليين: {totalMembers}");
                    Console.WriteLine($"📋 جميع الأعضاء الحاليين أصبحوا من نوع 'عضو أساسي' (Regular)");
                }
                
                // 4. إضافة عمود CollectionFrequency إلى SavingPlans
                AddCollectionFrequencyColumn(connection);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في تحديث قاعدة البيانات: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// إضافة حقل CollectionFrequency إلى جدول SavingPlans
        /// </summary>
        private static void AddCollectionFrequencyColumn(SQLiteConnection connection)
        {
            try
            {
                bool columnExists = CheckColumnExists(connection, "SavingPlans", "CollectionFrequency");
                
                if (columnExists)
                {
                    Console.WriteLine("✅ عمود CollectionFrequency موجود بالفعل");
                    return;
                }

                string sql = @"ALTER TABLE SavingPlans ADD COLUMN CollectionFrequency TEXT NOT NULL DEFAULT 'Daily'";
                
                using var command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();

                Console.WriteLine("✅ تم إضافة عمود CollectionFrequency بنجاح");
                Console.WriteLine($"📋 جميع الخطط الحالية أصبحت 'تحصيل يومي' (Daily)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ تحذير: {ex.Message}");
            }
        }

        /// <summary>
        /// التحقق من وجود عمود في جدول
        /// </summary>
        private static bool CheckColumnExists(SQLiteConnection connection, string tableName, string columnName)
        {
            string sql = $"PRAGMA table_info({tableName})";
            
            using var command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                string colName = reader.GetString(1); // العمود الثاني يحتوي على اسم العمود
                if (colName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// الحصول على عدد الأعضاء
        /// </summary>
        private static int GetTotalMembers(SQLiteConnection connection)
        {
            string sql = "SELECT COUNT(*) FROM Members";
            using var command = new SQLiteCommand(sql, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// تحويل عضو معين إلى "خلف الجمعية"
        /// </summary>
        public static bool ConvertMemberToBehindAssociation(int memberId)
        {
            try
            {
                using var connection = DatabaseContext.CreateConnection();
                connection.Open();

                string sql = "UPDATE Members SET MemberType = 'BehindAssociation' WHERE MemberID = @MemberID";
                
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@MemberID", memberId);
                
                int rowsAffected = command.ExecuteNonQuery();
                
                if (rowsAffected > 0)
                {
                    Console.WriteLine($"✅ تم تحويل العضو رقم {memberId} إلى 'خلف الجمعية'");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ لم يتم العثور على العضو رقم {memberId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في تحويل العضو: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// عرض جميع الأعضاء مع أنواعهم
        /// </summary>
        public static void DisplayAllMembersWithTypes()
        {
            try
            {
                using var connection = DatabaseContext.CreateConnection();
                connection.Open();

                string sql = @"
                    SELECT 
                        MemberID,
                        Name,
                        Phone,
                        MemberType,
                        CreatedDate,
                        IsArchived
                    FROM Members
                    ORDER BY MemberID";

                using var command = new SQLiteCommand(sql, connection);
                using var reader = command.ExecuteReader();

                Console.WriteLine("\n📋 قائمة الأعضاء:");
                Console.WriteLine("─────────────────────────────────────────────────────");
                
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    string phone = reader.IsDBNull(2) ? "-" : reader.GetString(2);
                    string memberType = reader.GetString(3);
                    string memberTypeAr = memberType == "Regular" ? "عضو أساسي" : "خلف الجمعية";
                    
                    Console.WriteLine($"#{id} - {name} - {phone} - [{memberTypeAr}]");
                }
                
                Console.WriteLine("─────────────────────────────────────────────────────\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في عرض الأعضاء: {ex.Message}");
            }
        }
    }
}
