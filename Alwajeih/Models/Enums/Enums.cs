namespace Alwajeih.Models
{
    /// <summary>
    /// نوع العضو
    /// </summary>
    public enum MemberType
    {
        /// <summary>
        /// عضو أساسي - يعامل بنظام الجمعية (182 يوم، مبلغ محدد)
        /// </summary>
        Regular,
        
        /// <summary>
        /// خلف الجمعية - أمانة (تحصيل يومي + سحب متى أراد، بدون إجمالي محدد)
        /// </summary>
        BehindAssociation
    }

    /// <summary>
    /// تكرار التحصيل
    /// </summary>
    public enum CollectionFrequency
    {
        /// <summary>
        /// تحصيل يومي
        /// </summary>
        Daily,
        
        /// <summary>
        /// تحصيل أسبوعي
        /// </summary>
        Weekly
    }

    /// <summary>
    /// دور المستخدم في النظام
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// مشاهد - يمكنه فقط عرض البيانات والتقارير
        /// </summary>
        Viewer,
        
        /// <summary>
        /// أمين صندوق - يمكنه جميع عمليات التحصيل والجرد
        /// </summary>
        Cashier,
        
        /// <summary>
        /// مدير - جميع الصلاحيات بما فيها إدارة المستخدمين والنسخ الاحتياطي
        /// </summary>
        Manager
    }

    /// <summary>
    /// حالة الحصة
    /// </summary>
    public enum PlanStatus
    {
        /// <summary>
        /// نشطة - قيد التحصيل
        /// </summary>
        Active,
        
        /// <summary>
        /// مكتملة - انتهت المدة وتم السداد
        /// </summary>
        Completed,
        
        /// <summary>
        /// مؤرشفة - تم أرشفتها من قبل المدير
        /// </summary>
        Archived
    }

    /// <summary>
    /// نوع الدفع
    /// </summary>
    public enum PaymentType
    {
        /// <summary>
        /// نقدي
        /// </summary>
        Cash,
        
        /// <summary>
        /// إلكتروني
        /// </summary>
        Electronic
    }

    /// <summary>
    /// مصدر الدفع
    /// </summary>
    public enum PaymentSource
    {
        /// <summary>
        /// نقدي
        /// </summary>
        Cash,
        
        /// <summary>
        /// كريمي
        /// </summary>
        Karimi,
        
        /// <summary>
        /// تحويل بنكي
        /// </summary>
        BankTransfer,
        
        /// <summary>
        /// مصدر آخر
        /// </summary>
        Other
    }

    /// <summary>
    /// نوع معاملة الخزنة
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// إيداع
        /// </summary>
        Deposit,
        
        /// <summary>
        /// سحب
        /// </summary>
        Withdrawal,
        
        /// <summary>
        /// مصروف
        /// </summary>
        Expense
    }

    /// <summary>
    /// تصنيف معاملات الخزنة (فئة فرعية لتحديد نوع المعاملة بدقة)
    /// </summary>
    public enum VaultTransactionCategory
    {
        /// <summary>
        /// ترحيل من الجرد الأسبوعي
        /// </summary>
        WeeklyReconciliation,
        
        /// <summary>
        /// سحب لعضو/عميل (مكافأة، قرض، إلخ)
        /// </summary>
        MemberWithdrawal,
        
        /// <summary>
        /// سحب لعضو خلف الجمعية (أمانة)
        /// </summary>
        BehindAssociationWithdrawal,
        
        /// <summary>
        /// خرجيات المدير (مصروفات إدارية، رواتب، إلخ)
        /// </summary>
        ManagerWithdrawals,
        
        /// <summary>
        /// خلف الجمعية (ديون الجمعية)
        /// </summary>
        AssociationDebt,
        
        /// <summary>
        /// مفقود (نقص في المحاسبة)
        /// </summary>
        Missing,
        
        /// <summary>
        /// إيداع من عضو (دفعة إضافية، تبرع، إلخ)
        /// </summary>
        MemberDeposit,
        
        /// <summary>
        /// مصروف تشغيلي (إيجار، كهرباء، قرطاسية، إلخ)
        /// </summary>
        OperatingExpense,
        
        /// <summary>
        /// أخرى (غير مصنف)
        /// </summary>
        Other
    }

    /// <summary>
    /// حالة الجرد الأسبوعي
    /// </summary>
    public enum ReconciliationStatus
    {
        /// <summary>
        /// قيد الانتظار
        /// </summary>
        Pending,
        
        /// <summary>
        /// مكتمل
        /// </summary>
        Completed
    }

    /// <summary>
    /// حالة المدفوعات الخارجية
    /// </summary>
    public enum ExternalPaymentStatus
    {
        /// <summary>
        /// قيد الانتظار
        /// </summary>
        Pending,
        
        /// <summary>
        /// مطابق
        /// </summary>
        Matched,
        
        /// <summary>
        /// غير مطابق
        /// </summary>
        Unmatched
    }

    /// <summary>
    /// نوع العملية في سجل التدقيق
    /// </summary>
    public enum AuditAction
    {
        Create,
        Update,
        Delete,
        Archive,
        Login,
        Logout,
        Cancel,
        Restore,
        Backup
    }

    /// <summary>
    /// نوع الكيان في سجل التدقيق
    /// </summary>
    public enum EntityType
    {
        User,
        Member,
        SavingPlan,
        DailyCollection,
        DailyArrear,
        PreviousArrears,
        VaultTransaction,
        WeeklyReconciliation,
        ExternalPayment,
        Receipt,
        AdvancePayment
    }
}
