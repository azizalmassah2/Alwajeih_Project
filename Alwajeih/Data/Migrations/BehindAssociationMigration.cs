using System;
using System.Data.SQLite;

namespace Alwajeih.Data.Migrations
{
    /// <summary>
    /// Migration لإنشاء جداول نظام "خلف الجمعية"
    /// </summary>
    public static class BehindAssociationMigration
    {
        /// <summary>
        /// تشغيل جميع الـ Migrations الخاصة بخلف الجمعية
        /// </summary>
        public static void RunMigration()
        {
            CreateBehindAssociationTransactionsTable();
            // CreateBehindAssociationAccountsTable();
        }
        
        /// <summary>
        /// إنشاء جدول معاملات خلف الجمعية
        /// </summary>
        private static void CreateBehindAssociationTransactionsTable()
        {
            try
            {
                using var connection = DatabaseContext.CreateConnection();
                connection.Open();
                
                // التحقق من وجود الجدول
                string checkTableSql = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='BehindAssociationTransactions'";
                
                using (var checkCmd = new SQLiteCommand(checkTableSql, connection))
                {
                    var result = checkCmd.ExecuteScalar();
                    if (result != null)
                    {
                        Console.WriteLine("✅ جدول BehindAssociationTransactions موجود بالفعل");
                        return;
                    }
                }
                
                // إنشاء الجدول
                string createTableSql = @"
                    CREATE TABLE BehindAssociationTransactions (
                        TransactionID INTEGER PRIMARY KEY AUTOINCREMENT,
                        MemberID INTEGER NOT NULL,
                        TransactionType TEXT NOT NULL,
                        Amount REAL NOT NULL,
                        TransactionDate TEXT NOT NULL,
                        WeekNumber INTEGER NOT NULL,
                        DayNumber INTEGER NOT NULL,
                        PaymentSource TEXT NOT NULL,
                        ReferenceNumber TEXT,
                        Notes TEXT,
                        RecordedBy INTEGER NOT NULL,
                        RecordedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        IsCancelled INTEGER DEFAULT 0,
                        CancellationReason TEXT,
                        FOREIGN KEY (MemberID) REFERENCES Members(MemberID),
                        FOREIGN KEY (RecordedBy) REFERENCES Users(UserID)
                    )";
                
                using (var createCmd = new SQLiteCommand(createTableSql, connection))
                {
                    createCmd.ExecuteNonQuery();
                }
                
                // إنشاء فهارس
                string createIndexSql1 = @"
                    CREATE INDEX IF NOT EXISTS idx_behind_association_transactions_member 
                    ON BehindAssociationTransactions(MemberID)";
                
                string createIndexSql2 = @"
                    CREATE INDEX IF NOT EXISTS idx_behind_association_transactions_week 
                    ON BehindAssociationTransactions(WeekNumber, DayNumber)";
                
                using (var indexCmd1 = new SQLiteCommand(createIndexSql1, connection))
                {
                    indexCmd1.ExecuteNonQuery();
                }
                
                using (var indexCmd2 = new SQLiteCommand(createIndexSql2, connection))
                {
                    indexCmd2.ExecuteNonQuery();
                }
                
                Console.WriteLine("✅ تم إنشاء جدول BehindAssociationTransactions بنجاح");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في إنشاء جدول BehindAssociationTransactions: {ex.Message}");
            }
        }
        
        /// <summary>
        /// إنشاء جدول حسابات خلف الجمعية (اختياري)
        /// </summary>
        // private static void CreateBehindAssociationAccountsTable()
        // {
        //     try
        //     {
        //         using var connection = DatabaseContext.CreateConnection();
        //         connection.Open();
                
        //         // التحقق من وجود الجدول
        //         string checkTableSql = @"
        //             SELECT name FROM sqlite_master 
        //             WHERE type='table' AND name='BehindAssociationAccounts'";
                
        //         using (var checkCmd = new SQLiteCommand(checkTableSql, connection))
        //         {
        //             var result = checkCmd.ExecuteScalar();
        //             if (result != null)
        //             {
        //                 Console.WriteLine("✅ جدول BehindAssociationAccounts موجود بالفعل");
        //                 return;
        //             }
        //         }
                
        //         // إنشاء الجدول
        //         string createTableSql = @"
        //             CREATE TABLE BehindAssociationAccounts (
        //                 AccountID INTEGER PRIMARY KEY AUTOINCREMENT,
        //                 MemberID INTEGER NOT NULL UNIQUE,
        //                 AccountNumber TEXT UNIQUE,
        //                 CurrentBalance REAL NOT NULL DEFAULT 0,
        //                 TotalDeposits REAL NOT NULL DEFAULT 0,
        //                 TotalWithdrawals REAL NOT NULL DEFAULT 0,
        //                 OpenedDate TEXT NOT NULL,
        //                 IsActive INTEGER DEFAULT 1,
        //                 Notes TEXT,
        //                 CreatedBy INTEGER NOT NULL,
        //                 CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
        //                 FOREIGN KEY (MemberID) REFERENCES Members(MemberID),
        //                 FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
        //             )";
                
        //         using (var createCmd = new SQLiteCommand(createTableSql, connection))
        //         {
        //             createCmd.ExecuteNonQuery();
        //         }
                
        //         // إنشاء فهرس
        //         string createIndexSql = @"
        //             CREATE INDEX IF NOT EXISTS idx_behind_association_accounts_member 
        //             ON BehindAssociationAccounts(MemberID)";
                
        //         using (var indexCmd = new SQLiteCommand(createIndexSql, connection))
        //         {
        //             indexCmd.ExecuteNonQuery();
        //         }
                
        //         Console.WriteLine("✅ تم إنشاء جدول BehindAssociationAccounts بنجاح");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ خطأ في إنشاء جدول BehindAssociationAccounts: {ex.Message}");
        //     }
        // }
    }
}
