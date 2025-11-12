using System;
using System.Data.SQLite;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Data
{
    /// <summary>
    /// مُهيئ قاعدة البيانات - إنشاء الجداول والبيانات الأولية
    /// </summary>
    public class DatabaseInitializer
    {
        /// <summary>
        /// تهيئة قاعدة البيانات (إنشاء الجداول والبيانات الأولية)
        /// </summary>
        public static void Initialize()
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();

            // تفعيل Foreign Keys
            ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");

            // إنشاء جميع الجداول
            CreateUsersTable(connection);
            CreateMembersTable(connection);
            CreateSavingPlansTable(connection);
            CreateDailyCollectionsTable(connection);
            CreateDailyArrearsTable(connection);
            CreatePreviousArrearsTable(connection);
            CreateVaultTransactionsTable(connection);
            CreateWeeklyReconciliationsTable(connection);
            CreateExternalPaymentsTable(connection);
            CreateReceiptsTable(connection);
            CreateAdvancePaymentsTable(connection);
            CreateSystemSettingsTable(connection);
            CreateAuditLogsTable(connection);

            // إنشاء الفهارس
            CreateIndexes(connection);

            // إدخال البيانات الأولية
            InsertDefaultData(connection);
            
            connection.Close();
            
            // تحديث قاعدة البيانات (إضافة MemberType إذا لم يكن موجوداً)
            DatabaseMigration.AddMemberTypeColumn();
            
            // تحديث جداول المتأخرات والسابقات
            Migrations.ArrearsMigration.RunAllMigrations();
            
            // إنشاء جدول السابقات المتراكمة
            Migrations.AccumulatedArrearsMigration.RunMigration();
            
            // إضافة حقل مبلغ سداد السابقات في جدول التحصيل اليومي
            // Migrations.AddPreviousArrearPaymentAmountMigration.RunMigration();
            
            // إنشاء جداول نظام خلف الجمعية
            Migrations.BehindAssociationMigration.RunMigration();
            
            // إنشاء جدول تسجيل مدفوعات السابقات الأسبوعية (لكشف حساب العضو)
            Migrations.WeeklyArrearPaymentHistoryMigration.RunMigration();
        }

        private static void CreateUsersTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    IsActive INTEGER DEFAULT 1,
                    LastLogin TEXT,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateMembersTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS Members (
                    MemberID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Phone TEXT,
                    MemberType TEXT NOT NULL DEFAULT 'Regular',
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    IsArchived INTEGER DEFAULT 0,
                    CreatedBy INTEGER NOT NULL,
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateSavingPlansTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS SavingPlans (
                    PlanID INTEGER PRIMARY KEY AUTOINCREMENT,
                    MemberID INTEGER NOT NULL,
                    PlanNumber INTEGER NOT NULL,
                    DailyAmount REAL NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Status TEXT NOT NULL,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    CreatedBy INTEGER NOT NULL,
                    FOREIGN KEY (MemberID) REFERENCES Members(MemberID),
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
                    UNIQUE(MemberID, PlanNumber)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateDailyCollectionsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS DailyCollections (
                    CollectionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanID INTEGER NOT NULL,
                    CollectionDate TEXT NOT NULL,
                    WeekNumber INTEGER NOT NULL DEFAULT 1,
                    DayNumber INTEGER NOT NULL DEFAULT 1,
                    DayName TEXT,
                    AmountPaid REAL NOT NULL,
                    PaymentType TEXT NOT NULL,
                    PaymentSource TEXT NOT NULL,
                    ReferenceNumber TEXT,
                    ReceiptNumber TEXT,
                    Notes TEXT,
                    CollectedBy INTEGER NOT NULL,
                    CollectedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    IsCancelled INTEGER DEFAULT 0,
                    CancelledBy INTEGER,
                    CancelledAt TEXT,
                    CancellationReason TEXT,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID),
                    FOREIGN KEY (CollectedBy) REFERENCES Users(UserID),
                    FOREIGN KEY (CancelledBy) REFERENCES Users(UserID),
                    CHECK (WeekNumber >= 1 AND WeekNumber <= 26),
                    CHECK (DayNumber >= 1 AND DayNumber <= 7)
                )";
            ExecuteNonQuery(connection, sql);

            // إضافة الأعمدة الجديدة إذا كان الجدول موجوداً مسبقاً
            AddColumnIfNotExists(
                connection,
                "DailyCollections",
                "WeekNumber",
                "INTEGER NOT NULL DEFAULT 1"
            );
            AddColumnIfNotExists(
                connection,
                "DailyCollections",
                "DayNumber",
                "INTEGER NOT NULL DEFAULT 1"
            );
            AddColumnIfNotExists(connection, "DailyCollections", "DayName", "TEXT");
            AddColumnIfNotExists(connection, "DailyCollections", "CancelledBy", "INTEGER");
            AddColumnIfNotExists(connection, "DailyCollections", "CancelledAt", "TEXT");
        }

        private static void CreateDailyArrearsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS DailyArrears (
                    ArrearID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanID INTEGER NOT NULL,
                    ArrearDate TEXT NOT NULL,
                    AmountDue REAL NOT NULL,
                    IsPaid INTEGER DEFAULT 0,
                    PaidDate TEXT,
                    PaidAmount REAL DEFAULT 0,
                    RemainingAmount REAL NOT NULL,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID),
                    UNIQUE(PlanID, ArrearDate)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreatePreviousArrearsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS PreviousArrears (
                    PreviousArrearID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanID INTEGER NOT NULL UNIQUE,
                    TotalArrears REAL DEFAULT 0,
                    LastUpdated TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateVaultTransactionsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS VaultTransactions (
                    TransactionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionType TEXT NOT NULL,
                    Category TEXT NOT NULL DEFAULT 'Other',
                    Amount REAL NOT NULL,
                    TransactionDate TEXT NOT NULL,
                    Description TEXT,
                    RelatedReconciliationID INTEGER,
                    RelatedMemberID INTEGER,
                    RelatedPlanID INTEGER,
                    PerformedBy INTEGER NOT NULL,
                    PerformedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    IsCancelled INTEGER DEFAULT 0,
                    CancellationReason TEXT,
                    FOREIGN KEY (RelatedReconciliationID) REFERENCES WeeklyReconciliations(ReconciliationID),
                    FOREIGN KEY (RelatedMemberID) REFERENCES Members(MemberID),
                    FOREIGN KEY (RelatedPlanID) REFERENCES SavingPlans(PlanID),
                    FOREIGN KEY (PerformedBy) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateWeeklyReconciliationsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS WeeklyReconciliations (
                    ReconciliationID INTEGER PRIMARY KEY AUTOINCREMENT,
                    WeekNumber INTEGER NOT NULL UNIQUE,
                    WeekStartDate TEXT NOT NULL,
                    WeekEndDate TEXT NOT NULL,
                    ExpectedAmount REAL NOT NULL,
                    ActualAmount REAL NOT NULL,
                    Difference REAL NOT NULL,
                    Notes TEXT,
                    Status TEXT DEFAULT 'Pending',
                    PerformedBy INTEGER NOT NULL,
                    ReconciliationDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    PerformedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (PerformedBy) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateExternalPaymentsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS ExternalPayments (
                    ExternalPaymentID INTEGER PRIMARY KEY AUTOINCREMENT,
                    MemberID INTEGER NOT NULL,
                    ReferenceNumber TEXT UNIQUE NOT NULL,
                    Amount REAL NOT NULL,
                    PaymentDate TEXT NOT NULL,
                    PaymentSource TEXT NOT NULL,
                    Status TEXT DEFAULT 'Pending',
                    MatchedWithCollectionID INTEGER,
                    Notes TEXT,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    CreatedBy INTEGER NOT NULL,
                    FOREIGN KEY (MemberID) REFERENCES Members(MemberID),
                    FOREIGN KEY (MatchedWithCollectionID) REFERENCES DailyCollections(CollectionID),
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateReceiptsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS Receipts (
                    ReceiptID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReceiptNumber TEXT UNIQUE NOT NULL,
                    CollectionID INTEGER NOT NULL,
                    PrintDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    PrintedBy INTEGER NOT NULL,
                    FOREIGN KEY (CollectionID) REFERENCES DailyCollections(CollectionID),
                    FOREIGN KEY (PrintedBy) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateAuditLogsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    AuditID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID INTEGER NOT NULL,
                    Action TEXT NOT NULL,
                    EntityType TEXT NOT NULL,
                    EntityID INTEGER,
                    Details TEXT,
                    Reason TEXT,
                    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
                    IPAddress TEXT,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateAdvancePaymentsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS AdvancePayments (
                    AdvanceID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanID INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    PaymentDate TEXT NOT NULL,
                    Description TEXT,
                    ApprovedBy INTEGER NOT NULL,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (PlanID) REFERENCES SavingPlans(PlanID),
                    FOREIGN KEY (ApprovedBy) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateSystemSettingsTable(SQLiteConnection connection)
        {
            string sql =
                @"
                CREATE TABLE IF NOT EXISTS SystemSettings (
                    SettingID INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    CreatedBy INTEGER NOT NULL,
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
                )";
            ExecuteNonQuery(connection, sql);
        }

        private static void CreateIndexes(SQLiteConnection connection)
        {
            // فهارس لتحسين الأداء
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_members_name ON Members(Name)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_savingplans_memberid ON SavingPlans(MemberID)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_savingplans_status ON SavingPlans(Status)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_dailycollections_planid ON DailyCollections(PlanID)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_dailycollections_date ON DailyCollections(CollectionDate)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_dailycollections_week ON DailyCollections(WeekNumber)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_dailycollections_weekday ON DailyCollections(WeekNumber, DayNumber)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_dailycollections_receipt ON DailyCollections(ReceiptNumber)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_dailyarrears_planid ON DailyArrears(PlanID)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_dailyarrears_date ON DailyArrears(ArrearDate)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_vault_date ON VaultTransactions(TransactionDate)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_audit_userid ON AuditLogs(UserID)"
            );
            ExecuteNonQuery(
                connection,
                "CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON AuditLogs(Timestamp)"
            );
        }

        private static void InsertDefaultData(SQLiteConnection connection)
        {
            // التحقق من وجود مستخدم وجيه
            var checkSql = "SELECT COUNT(*) FROM Users WHERE Username = 'وجيه'";
            using var checkCmd = new SQLiteCommand(checkSql, connection);
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (count == 0)
            {
                // إنشاء مستخدم وجيه افتراضي
                var insertSql =
                    @"
                    INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedDate)
                    VALUES (@Username, @PasswordHash, @Role, 1, @CreatedDate)";

                using var insertCmd = new SQLiteCommand(insertSql, connection);
                insertCmd.Parameters.AddWithValue("@Username", "وجيه");
                insertCmd.Parameters.AddWithValue(
                    "@PasswordHash",
                    PasswordHelper.HashPassword("123")
                );
                insertCmd.Parameters.AddWithValue("@Role", "Manager");
                insertCmd.Parameters.AddWithValue(
                    "@CreatedDate",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                );
                insertCmd.ExecuteNonQuery();
            }
            
            // إضافة بيانات تجريبية شاملة عند عدم وجود بيانات مسبقة
            string checkMembersSql = "SELECT COUNT(*) FROM Members";
            using (var membersCmd = new SQLiteCommand(checkMembersSql, connection))
            {
                long membersCount = (long)membersCmd.ExecuteScalar();
                if (membersCount == 0)
                {
                    //InsertTestData(connection);
                }
            }
        }
        
        private static void InsertTestData(SQLiteConnection connection)
        {
            // التحقق من وجود بيانات تجريبية
            string checkSql = "SELECT COUNT(*) FROM Members";
            using (var cmd = new SQLiteCommand(checkSql, connection))
            {
                long count = (long)cmd.ExecuteScalar();
                if (count > 0) return; // البيانات موجودة مسبقاً
            }
            
            // 1. أعضاء  (بداية الجمعية قبل شهرين - أغسطس 2025)
            string membersSql = @"
                INSERT INTO Members (Name, Phone, CreatedDate, IsArchived, CreatedBy) VALUES
                ('أحمد محمد الحداد', '777123456', '2025-08-25', 0, 1),
                ('فاطمة علي المقطري', '773987654', '2025-08-25', 0, 1),
                ('عبدالله سعيد باشراحيل', '770555123', '2025-08-28', 0, 1),
                ('نورة خالد الشامي', '771444789', '2025-09-01', 0, 1),
                ('سارة محمود الأهدل', '772333456', '2025-09-05', 0, 1),
                ('خالد يحيى العمري', '774555666', '2025-09-10', 0, 1),
                ('منى سالم الحميري', '775666777', '2025-09-15', 0, 1),
                ('علي حسن الشيباني', '776777888', '2025-09-20', 0, 1)";
            ExecuteNonQuery(connection, membersSql);
            
            // 2. حصص ادخار (بداية 30 أغسطس 2025 - السبت)
            string plansSql = @"
                INSERT INTO SavingPlans (MemberID, PlanNumber, DailyAmount, StartDate, EndDate, TotalAmount, Status, CreatedBy) VALUES
                (1, 101, 5000.00, '2025-08-30', '2026-02-28', 910000.00, 'Active', 1),
                (2, 102, 7500.00, '2025-08-30', '2026-02-28', 1365000.00, 'Active', 1),
                (3, 103, 10000.00, '2025-08-30', '2026-02-28', 1820000.00, 'Active', 1),
                (4, 104, 6000.00, '2025-09-06', '2026-03-07', 1092000.00, 'Active', 1),
                (5, 105, 4000.00, '2025-08-30', '2026-02-28', 728000.00, 'Active', 1),
                (6, 106, 8000.00, '2025-09-13', '2026-03-14', 1456000.00, 'Active', 1),
                (7, 107, 6500.00, '2025-08-30', '2026-02-28', 1183000.00, 'Active', 1),
                (8, 108, 9000.00, '2025-09-20', '2026-03-21', 1638000.00, 'Active', 1)";
            ExecuteNonQuery(connection, plansSql);
            
            // 3. إعدادات النظام (30 أغسطس 2025 - 28 فبراير 2026 = 6 أشهر)
            string settingsSql = @"
                INSERT INTO SystemSettings (StartDate, EndDate, CreatedAt, CreatedBy) VALUES
                ('2025-08-30', '2026-02-28', datetime('now'), 1)";
            ExecuteNonQuery(connection, settingsSql);
            
            // 4. تحصيلات الأسبوع 1-9 (شهرين كاملين حتى اليوم)
            string collectionsSql = @"
                INSERT INTO DailyCollections (PlanID, CollectionDate, WeekNumber, DayNumber, DayName, AmountPaid, PaymentType, PaymentSource, ReceiptNumber, CollectedBy, CollectedAt) VALUES
                -- الأسبوع 1
                (1, '2025-08-30', 1, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-001', 1, '2025-08-30 09:00:00'),
                (2, '2025-08-30', 1, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-002', 1, '2025-08-30 09:15:00'),
                (3, '2025-08-30', 1, 1, 'السبت', 10000.00, 'Cash', 'Cash', 'RCP-003', 1, '2025-08-30 09:30:00'),
                (5, '2025-08-30', 1, 1, 'السبت', 4000.00, 'Cash', 'Cash', 'RCP-004', 1, '2025-08-30 09:45:00'),
                (7, '2025-08-30', 1, 1, 'السبت', 6500.00, 'Cash', 'Cash', 'RCP-005', 1, '2025-08-30 10:00:00'),
                (1, '2025-08-31', 1, 2, 'الأحد', 5000.00, 'Cash', 'Cash', 'RCP-006', 1, '2025-08-31 09:00:00'),
                (2, '2025-08-31', 1, 2, 'الأحد', 7500.00, 'Cash', 'Cash', 'RCP-007', 1, '2025-08-31 09:15:00'),
                (3, '2025-08-31', 1, 2, 'الأحد', 7500.00, 'Cash', 'Cash', 'RCP-008', 1, '2025-08-31 09:30:00'),
                (5, '2025-08-31', 1, 2, 'الأحد', 4000.00, 'Cash', 'Cash', 'RCP-009', 1, '2025-08-31 09:45:00'),
                -- الأسبوع 2-9
                (1, '2025-09-06', 2, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-020', 1, '2025-09-06 09:00:00'),
                (2, '2025-09-06', 2, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-021', 1, '2025-09-06 09:15:00'),
                (1, '2025-09-13', 3, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-040', 1, '2025-09-13 09:00:00'),
                (2, '2025-09-13', 3, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-041', 1, '2025-09-13 09:15:00'),
                (1, '2025-09-20', 4, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-060', 1, '2025-09-20 09:00:00'),
                (2, '2025-09-20', 4, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-061', 1, '2025-09-20 09:15:00'),
                (1, '2025-09-27', 5, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-080', 1, '2025-09-27 09:00:00'),
                (2, '2025-09-27', 5, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-081', 1, '2025-09-27 09:15:00'),
                (1, '2025-10-04', 6, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-100', 1, '2025-10-04 09:00:00'),
                (2, '2025-10-04', 6, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-101', 1, '2025-10-04 09:15:00'),
                (1, '2025-10-11', 7, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-120', 1, '2025-10-11 09:00:00'),
                (2, '2025-10-11', 7, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-121', 1, '2025-10-11 09:15:00'),
                (1, '2025-10-18', 8, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-140', 1, '2025-10-18 09:00:00'),
                (2, '2025-10-18', 8, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-141', 1, '2025-10-18 09:15:00'),
                (1, '2025-10-25', 9, 1, 'السبت', 5000.00, 'Cash', 'Cash', 'RCP-160', 1, '2025-10-25 09:00:00'),
                (2, '2025-10-25', 9, 1, 'السبت', 7500.00, 'Cash', 'Cash', 'RCP-161', 1, '2025-10-25 09:15:00')";
            ExecuteNonQuery(connection, collectionsSql);
            
            // 5. متأخرات (شهرين)
            string arrearsSql = @"
                INSERT INTO DailyArrears (PlanID, ArrearDate, AmountDue, IsPaid, PaidDate, PaidAmount, RemainingAmount) VALUES
                (3, '2025-08-31', 2500.00, 1, '2025-09-01', 2500.00, 0.00),
                (5, '2025-09-01', 4000.00, 0, NULL, 0.00, 4000.00),
                (3, '2025-09-07', 2500.00, 0, NULL, 0.00, 2500.00),
                (5, '2025-09-14', 4000.00, 1, '2025-09-15', 4000.00, 0.00),
                (7, '2025-09-21', 6500.00, 0, NULL, 0.00, 6500.00),
                (3, '2025-10-05', 2500.00, 0, NULL, 0.00, 2500.00)";
            ExecuteNonQuery(connection, arrearsSql);
            
            // 6. مدفوعات خارجية
            string externalSql = @"
                INSERT INTO ExternalPayments (MemberID, ReferenceNumber, Amount, PaymentDate, PaymentSource, Status, CreatedBy, CreatedDate) VALUES
                (1, 'KRM-YE-001', 25000.00, '2025-09-05', 'Karimi', 'Matched', 1, '2025-09-05'),
                (2, 'BANK-YE-001', 37500.00, '2025-09-12', 'BankTransfer', 'Matched', 1, '2025-09-12'),
                (3, 'KRM-YE-002', 50000.00, '2025-09-20', 'Karimi', 'Matched', 1, '2025-09-20'),
                (4, 'KRM-YE-003', 30000.00, '2025-10-10', 'Karimi', 'Pending', 1, '2025-10-10'),
                (5, 'BANK-YE-002', 20000.00, '2025-10-15', 'BankTransfer', 'Pending', 1, '2025-10-15')";
            ExecuteNonQuery(connection, externalSql);
            
            // 7. جرد أسبوعي (9 أسابيع حتى اليوم)
            string reconciliationSql = @"
                INSERT INTO WeeklyReconciliations (WeekNumber, WeekStartDate, WeekEndDate, ExpectedAmount, ActualAmount, Difference, Notes, PerformedBy, ReconciliationDate) VALUES
                (1, '2025-08-30', '2025-09-05', 186500.00, 186500.00, 0.00, 'جرد الأسبوع 1', 1, '2025-09-05 18:00:00'),
                (2, '2025-09-06', '2025-09-12', 219500.00, 219500.00, 0.00, 'جرد الأسبوع 2', 1, '2025-09-12 18:00:00'),
                (3, '2025-09-13', '2025-09-19', 252500.00, 252000.00, -500.00, 'جرد الأسبوع 3', 1, '2025-09-19 18:00:00'),
                (4, '2025-09-20', '2025-09-26', 285500.00, 285500.00, 0.00, 'جرد الأسبوع 4', 1, '2025-09-26 18:00:00'),
                (5, '2025-09-27', '2025-10-03', 318500.00, 318500.00, 0.00, 'جرد الأسبوع 5', 1, '2025-10-03 18:00:00'),
                (6, '2025-10-04', '2025-10-10', 351500.00, 351500.00, 0.00, 'جرد الأسبوع 6', 1, '2025-10-10 18:00:00'),
                (7, '2025-10-11', '2025-10-17', 384500.00, 384500.00, 0.00, 'جرد الأسبوع 7', 1, '2025-10-17 18:00:00'),
                (8, '2025-10-18', '2025-10-24', 417500.00, 417500.00, 0.00, 'جرد الأسبوع 8', 1, '2025-10-24 18:00:00')";
            ExecuteNonQuery(connection, reconciliationSql);
            
            // 8. معاملات الخزنة (شهرين)
            string vaultSql = @"
                INSERT INTO VaultTransactions (TransactionType, Category, Amount, TransactionDate, Description, RelatedMemberID, PerformedBy) VALUES
                ('Deposit', 'WeeklyReconciliation', 186500.00, '2025-09-05', 'إيداع جرد الأسبوع 1', NULL, 1),
                ('Withdrawal', 'ManagerWithdrawals', 15000.00, '2025-09-08', 'خرجيات المدير', NULL, 1),
                ('Withdrawal', 'MemberWithdrawal', 8000.00, '2025-09-10', 'سحب لأحمد محمد', 1, 1),
                ('Deposit', 'WeeklyReconciliation', 219500.00, '2025-09-12', 'إيداع جرد الأسبوع 2', NULL, 1),
                ('Withdrawal', 'OperatingExpense', 12000.00, '2025-09-15', 'مصاريف تشغيلية', NULL, 1),
                ('Deposit', 'WeeklyReconciliation', 252000.00, '2025-09-19', 'إيداع جرد الأسبوع 3', NULL, 1),
                ('Withdrawal', 'MemberWithdrawal', 20000.00, '2025-09-22', 'سحب لفاطمة علي', 2, 1),
                ('Deposit', 'WeeklyReconciliation', 285500.00, '2025-09-26', 'إيداع جرد الأسبوع 4', NULL, 1),
                ('Deposit', 'WeeklyReconciliation', 318500.00, '2025-10-03', 'إيداع جرد الأسبوع 5', NULL, 1),
                ('Withdrawal', 'ManagerWithdrawals', 18000.00, '2025-10-08', 'خرجيات المدير', NULL, 1),
                ('Deposit', 'WeeklyReconciliation', 351500.00, '2025-10-10', 'إيداع جرد الأسبوع 6', NULL, 1),
                ('Withdrawal', 'MemberWithdrawal', 10000.00, '2025-10-15', 'سحب لعبدالله سعيد', 3, 1),
                ('Deposit', 'WeeklyReconciliation', 384500.00, '2025-10-17', 'إيداع جرد الأسبوع 7', NULL, 1),
                ('Deposit', 'WeeklyReconciliation', 417500.00, '2025-10-24', 'إيداع جرد الأسبوع 8', NULL, 1)";
            ExecuteNonQuery(connection, vaultSql);
        }


        private static void ExecuteNonQuery(SQLiteConnection connection, string sql)
        {
            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private static void AddColumnIfNotExists(
            SQLiteConnection connection,
            string tableName,
            string columnName,
            string columnType
        )
        {
            try
            {
                // التحقق من وجود العمود
                var checkSql = $"PRAGMA table_info({tableName})";
                using var checkCmd = new SQLiteCommand(checkSql, connection);
                using var reader = checkCmd.ExecuteReader();

                bool columnExists = false;
                while (reader.Read())
                {
                    if (reader.GetString(1) == columnName)
                    {
                        columnExists = true;
                        break;
                    }
                }

                // إضافة العمود إذا لم يكن موجوداً
                if (!columnExists)
                {
                    var alterSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
                    ExecuteNonQuery(connection, alterSql);
                }
            }
            catch (Exception ex)
            {
                // تجاهل الخطأ إذا كان العمود موجوداً بالفعل
                System.Diagnostics.Debug.WriteLine(
                    $"Error adding column {columnName}: {ex.Message}"
                );
            }
        }
    }
}
