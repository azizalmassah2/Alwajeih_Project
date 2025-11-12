using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Controls;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Data.Repositories;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Finance
{
    /// <summary>
    /// 🏦 ViewModel لإدارة الخزنة
    /// </summary>
    public class VaultViewModel : BaseViewModel
    {
        private readonly VaultService _vaultService;
        private readonly VaultRepository _vaultRepository;
        private readonly AuthenticationService _authService;
        private readonly MemberRepository _memberRepository;
        private readonly SavingPlanRepository _savingPlanRepository;
        private readonly ExternalPaymentRepository _externalPaymentRepository;
        private readonly Data.Repositories.BehindAssociation.BehindAssociationRepository _behindAssociationRepository;

        private decimal _currentBalance;
        private ObservableCollection<VaultTransaction> _transactions;
        private TransactionType _selectedTransactionType = TransactionType.Withdrawal; // السحب هو الافتراضي
        private decimal _amount;
        private string _description;
        private DateTime _startDate = DateTime.Now.AddDays(-30);
        private DateTime _endDate = DateTime.Now;
        private ComboBoxItem _selectedCategory;
        private Member _selectedMember;
        private ObservableCollection<Member> _members;
        private ObservableCollection<Member> _filteredMembers;
        private string _memberSearchText;
        private PaymentSource _paymentSource = PaymentSource.Cash;
        private ObservableCollection<Member> _behindAssociationMembers;
        private Member _selectedBehindAssociationMember;
        private ComboBoxItem _selectedDepositCategory;
        private int _transactionTypeIndex = 1; // 0=إيداع, 1=سحب

        public VaultViewModel()
        {
            _vaultService = new VaultService();
            _vaultRepository = new VaultRepository();
            _authService = AuthenticationService.Instance;
            _memberRepository = new MemberRepository();
            _savingPlanRepository = new SavingPlanRepository();
            _externalPaymentRepository = new ExternalPaymentRepository();
            _behindAssociationRepository = new Data.Repositories.BehindAssociation.BehindAssociationRepository();

            Transactions = new ObservableCollection<VaultTransaction>();
            Members = new ObservableCollection<Member>();
            FilteredMembers = new ObservableCollection<Member>();
            BehindAssociationMembers = new ObservableCollection<Member>();

            AddTransactionCommand = new RelayCommand(ExecuteAddTransaction, CanExecuteAdd);
            FilterCommand = new RelayCommand(ExecuteFilter, _ => true);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);

            LoadData();
            LoadMembers();
        }

        #region Properties

        public decimal CurrentBalance
        {
            get => _currentBalance;
            set => SetProperty(ref _currentBalance, value);
        }

        public ObservableCollection<VaultTransaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        public TransactionType SelectedTransactionType
        {
            get => _selectedTransactionType;
            set => SetProperty(ref _selectedTransactionType, value);
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                SetProperty(ref _amount, value);
                ((RelayCommand)AddTransactionCommand).RaiseCanExecuteChanged();
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public PaymentSource PaymentSource
        {
            get => _paymentSource;
            set => SetProperty(ref _paymentSource, value);
        }

        public int PaymentSourceIndex
        {
            get => (int)_paymentSource;
            set
            {
                _paymentSource = (PaymentSource)value;
                OnPropertyChanged(nameof(PaymentSource));
                OnPropertyChanged(nameof(PaymentSourceIndex));
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public ComboBoxItem SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                OnPropertyChanged(nameof(IsMemberWithdrawal));
                OnPropertyChanged(nameof(IsBehindAssociationWithdrawal));
            }
        }

        public Member SelectedMember
        {
            get => _selectedMember;
            set => SetProperty(ref _selectedMember, value);
        }

        public ObservableCollection<Member> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        public ObservableCollection<Member> FilteredMembers
        {
            get => _filteredMembers;
            set => SetProperty(ref _filteredMembers, value);
        }

        public string MemberSearchText
        {
            get => _memberSearchText;
            set
            {
                SetProperty(ref _memberSearchText, value);
                FilterMembers();
            }
        }

        public bool IsMemberWithdrawal
        {
            get => SelectedCategory?.Tag?.ToString() == "MemberWithdrawal";
        }

        public ObservableCollection<Member> BehindAssociationMembers
        {
            get => _behindAssociationMembers;
            set => SetProperty(ref _behindAssociationMembers, value);
        }

        public Member SelectedBehindAssociationMember
        {
            get => _selectedBehindAssociationMember;
            set => SetProperty(ref _selectedBehindAssociationMember, value);
        }

        public bool IsBehindAssociationWithdrawal
        {
            get => SelectedCategory?.Tag?.ToString() == "BehindAssociationWithdrawal";
        }
        
        public ComboBoxItem SelectedDepositCategory
        {
            get => _selectedDepositCategory;
            set => SetProperty(ref _selectedDepositCategory, value);
        }
        
        public int TransactionTypeIndex
        {
            get => _transactionTypeIndex;
            set
            {
                SetProperty(ref _transactionTypeIndex, value);
                // تحديث نوع المعاملة
                SelectedTransactionType = value == 0 ? TransactionType.Deposit : TransactionType.Withdrawal;
                OnPropertyChanged(nameof(IsDeposit));
                OnPropertyChanged(nameof(IsWithdrawal));
            }
        }
        
        public bool IsDeposit => SelectedTransactionType == TransactionType.Deposit;
        public bool IsWithdrawal => SelectedTransactionType == TransactionType.Withdrawal;

        #endregion

        #region Commands

        public ICommand AddTransactionCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanExecuteAdd(object parameter)
        {
            return Amount > 0 && _authService.HasPermission("ManageVault");
        }

        private void ExecuteAddTransaction(object parameter)
        {
            try
            {
                var userId = _authService.CurrentUser?.UserID ?? 0;
                
                // تحديد MemberID و Category
                int? relatedMemberId = null;
                VaultTransactionCategory category = VaultTransactionCategory.Other;
                
                // ✅ معالجة الإيداع
                if (IsDeposit)
                {
                    if (SelectedDepositCategory != null && !string.IsNullOrEmpty(SelectedDepositCategory.Tag?.ToString()))
                    {
                        string categoryTag = SelectedDepositCategory.Tag.ToString();
                        category = categoryTag switch
                        {
                            "MemberDeposit" => VaultTransactionCategory.MemberDeposit,
                            "OperatingDeposit" => VaultTransactionCategory.Other,
                            _ => VaultTransactionCategory.Other
                        };
                    }
                    
                    // إضافة وصف تلقائي للإيداع
                    if (string.IsNullOrWhiteSpace(Description))
                    {
                        Description = category == VaultTransactionCategory.MemberDeposit 
                            ? "إيداع من عضو" 
                            : "إيداع في الخزنة";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"💰 إيداع: {Amount:N2} ريال");
                    System.Diagnostics.Debug.WriteLine($"   الفئة: {category}");
                }
                // ✅ معالجة السحب
                else if (IsWithdrawal && SelectedCategory != null && !string.IsNullOrEmpty(SelectedCategory.Tag?.ToString()))
                {
                    string categoryTag = SelectedCategory.Tag.ToString();
                    category = categoryTag switch
                    {
                        "MemberWithdrawal" => VaultTransactionCategory.MemberWithdrawal,
                        "BehindAssociationWithdrawal" => VaultTransactionCategory.BehindAssociationWithdrawal,
                        "ManagerWithdrawals" => VaultTransactionCategory.ManagerWithdrawals,
                        "AssociationDebt" => VaultTransactionCategory.AssociationDebt,
                        "Missing" => VaultTransactionCategory.Missing,
                        "OperatingExpense" => VaultTransactionCategory.OperatingExpense,
                        _ => VaultTransactionCategory.Other
                    };
                }
                
                // ✅ سحب لعضو خلف الجمعية
                if (IsBehindAssociationWithdrawal && SelectedBehindAssociationMember != null)
                {
                    relatedMemberId = SelectedBehindAssociationMember.MemberID;
                    
                    // إضافة وصف تلقائي
                    if (string.IsNullOrWhiteSpace(Description))
                    {
                        Description = $"سحب لعضو خلف الجمعية: {SelectedBehindAssociationMember.Name}";
                    }
                    else
                    {
                        Description = $"سحب لعضو خلف الجمعية: {SelectedBehindAssociationMember.Name} - {Description}";
                    }
                    
                    // تسجيل السحب في نظام خلف الجمعية
                    var (currentWeek, currentDay) = Utilities.Helpers.WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
                    
                    var transaction = new Models.BehindAssociation.BehindAssociationTransaction
                    {
                        MemberID = SelectedBehindAssociationMember.MemberID,
                        WeekNumber = currentWeek,
                        DayNumber = currentDay,
                        TransactionDate = DateTime.Now,
                        Amount = Amount,
                        TransactionType = Models.BehindAssociation.BehindAssociationTransactionType.Withdrawal,
                        Notes = Description,
                        IsCancelled = false
                    };
                    
                    _behindAssociationRepository.AddTransaction(transaction);
                }
                // ✅ سحب لعضو عادي
                else if (IsMemberWithdrawal && SelectedMember != null)
                {
                    relatedMemberId = SelectedMember.MemberID;
                    category = VaultTransactionCategory.MemberWithdrawal; // ✅ تأكيد الفئة
                    
                    // إضافة وصف تلقائي
                    if (string.IsNullOrWhiteSpace(Description))
                    {
                        Description = $"سحب للعضو: {SelectedMember.Name}";
                    }
                    else
                    {
                        Description = $"سحب للعضو: {SelectedMember.Name} - {Description}";
                    }
                }
                
                // ✅ تحققات السحب لعضو - فقط للسحب
                if (IsMemberWithdrawal && SelectedMember != null)
                {
                    System.Diagnostics.Debug.WriteLine($"💰 سحب لعضو: {SelectedMember.Name} (ID: {SelectedMember.MemberID});");
                    System.Diagnostics.Debug.WriteLine($"   المبلغ: {Amount:N2} ريال");
                    System.Diagnostics.Debug.WriteLine($"   الفئة: {category}");
                    
                    // ✅ التحقق 1: هل العضو لديه أسهم نشطة？
                    if (!_savingPlanRepository.HasActivePlans(SelectedMember.MemberID))
                    {
                        System.Windows.MessageBox.Show(
                            $"❌ لا يمكن السحب للعضو: {SelectedMember.Name}\n\n" +
                            "السبب: العضو غير مشترك في أي سهم نشط حالياً.\n" +
                            "يجب أن يكون للعضو سهم نشط للسماح بالسحب له.",
                            "تنبيه",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    
                    // ✅ التحقق 2: هل السحوبات أقل من إجمالي الأسهم؟
                    decimal totalPlansAmount = _savingPlanRepository.GetTotalActivePlansAmount(SelectedMember.MemberID);
                    decimal currentWithdrawals = _vaultRepository.GetTotalMemberWithdrawals(SelectedMember.MemberID);
                    decimal newTotalWithdrawals = currentWithdrawals + Amount;
                    
                    if (newTotalWithdrawals > totalPlansAmount)
                    {
                        decimal availableAmount = totalPlansAmount - currentWithdrawals;
                        System.Windows.MessageBox.Show(
                            $"❌ لا يمكن السحب هذا المبلغ للعضو: {SelectedMember.Name}\n\n" +
                            $"إجمالي الأسهم: {totalPlansAmount:N2} ريال\n" +
                            $"السحوبات السابقة: {currentWithdrawals:N2} ريال\n" +
                            $"المبلغ المتاح للسحب: {availableAmount:N2} ريال\n" +
                            $"المبلغ المطلوب: {Amount:N2} ريال\n\n" +
                            "⚠️ لا يمكن أن تتجاوز السحوبات إجمالي مبلغ الأسهم!",
                            "تحذير",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return;
                    }
                }
                
                // ✅ إضافة وصف تلقائي للأنواع الأخرى (فقط للسحب)
                if (IsWithdrawal && string.IsNullOrWhiteSpace(Description))
                {
                    Description = category switch
                    {
                        VaultTransactionCategory.ManagerWithdrawals => "خرجيات المدير",
                        VaultTransactionCategory.Missing => "مبلغ مفقود",
                        VaultTransactionCategory.OperatingExpense => "مصروف تشغيلي",
                        VaultTransactionCategory.Other => "أخرى",
                        _ => "معاملة"
                    };
                }
                
                System.Diagnostics.Debug.WriteLine($"📝 إضافة معاملة:");
                System.Diagnostics.Debug.WriteLine($"   النوع: {SelectedTransactionType}");
                System.Diagnostics.Debug.WriteLine($"   الفئة: {category}");
                System.Diagnostics.Debug.WriteLine($"   المبلغ: {Amount:N2}");
                System.Diagnostics.Debug.WriteLine($"   العضو ID: {relatedMemberId}");
                
                var result = _vaultService.AddTransaction(
                    SelectedTransactionType,
                    Amount,
                    DateTime.Now,
                    Description,
                    relatedMemberId,
                    userId,
                    category);

                if (result.Success)
                {
                    // إذا كان السحب عبر كريمي، تسجيله في المدفوعات الخارجية
                    if (SelectedTransactionType == TransactionType.Withdrawal && PaymentSource == PaymentSource.Karimi)
                    {
                        // ✅ التحقق: يجب أن يكون هناك عضو مرتبط
                        if (relatedMemberId.HasValue && relatedMemberId.Value > 0)
                        {
                            var externalPayment = new ExternalPayment
                            {
                                MemberID = relatedMemberId.Value,
                                PaymentDate = DateTime.Now,
                                Amount = Amount,
                                PaymentSource = PaymentSource.Karimi,
                                Notes = $"سحب من الخزنة - {Description}",
                                ReferenceNumber = $"VAULT-{DateTime.Now:yyyyMMddHHmmss}",
                                Status = ExternalPaymentStatus.Pending,
                                CreatedBy = userId
                            };
                            _externalPaymentRepository.Add(externalPayment);
                            
                            System.Diagnostics.Debug.WriteLine($"✅ تم تسجيل الدفع الخارجي (كريمي) للعضو ID: {relatedMemberId}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ لا يمكن تسجيل دفع كريمي بدون عضو مرتبط");
                        }
                    }
                    
                    string icon = IsDeposit ? "💰" : "💸";
                    string typeText = IsDeposit ? "إيداع" : "سحب";
                    string memberInfo = IsMemberWithdrawal && SelectedMember != null 
                        ? $"\nالعضو: {SelectedMember.Name}" 
                        : "";
                    string paymentInfo = IsWithdrawal && PaymentSource == PaymentSource.Karimi ? "\nنوع الدفع: كريمي" : "";
                    
                    System.Windows.MessageBox.Show(
                        $"✅ تم {typeText} بنجاح! {icon}\n\n" +
                        $"النوع: {typeText}\n" +
                        $"المبلغ: {Amount:N2} ريال{memberInfo}{paymentInfo}",
                        "نجاح",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    LoadData();
                    ClearForm();
                }
                else
                {
                    System.Windows.MessageBox.Show($"❌ {result.Message}", "خطأ",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteFilter(object parameter)
        {
            LoadTransactions();
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadData();
        }

        #endregion

        #region Helper Methods

        private void LoadData()
        {
            LoadBalance();
            LoadTransactions();
        }

        private void LoadBalance()
        {
            try
            {
                CurrentBalance = _vaultService.GetCurrentBalance();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل الرصيد: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadTransactions()
        {
            try
            {
                var transactions = _vaultRepository.GetByDateRange(StartDate, EndDate);
                Transactions.Clear();
                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل المعاملات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            Amount = 0;
            Description = string.Empty;
            SelectedMember = null;
            SelectedBehindAssociationMember = null;
            SelectedCategory = null;
            SelectedDepositCategory = null;
            MemberSearchText = string.Empty;
            PaymentSource = PaymentSource.Cash;
            TransactionTypeIndex = 1; // إعادة تعيين إلى السحب كقيمة افتراضية
        }

        private string GetTransactionTypeText(TransactionType type)
        {
            return type switch
            {
                TransactionType.Deposit => "إيداع 💰",
                TransactionType.Withdrawal => "سحب 💸",
                TransactionType.Expense => "مصروف 📤",
                _ => type.ToString()
            };
        }

        private void LoadMembers()
        {
            try
            {
                var allMembers = _memberRepository.GetActive();
                
                // الحصول على IDs أعضاء خلف الجمعية
                var behindAssociationMemberIds = _behindAssociationRepository.GetAllTransactions()
                    .Select(t => t.MemberID)
                    .Distinct()
                    .ToList();
                
                // ✅ الأعضاء العاديين = جميع الأعضاء - أعضاء خلف الجمعية
                Members.Clear();
                FilteredMembers.Clear();
                foreach (var member in allMembers.Where(m => !behindAssociationMemberIds.Contains(m.MemberID)))
                {
                    Members.Add(member);
                    FilteredMembers.Add(member);
                }

                // ✅ أعضاء خلف الجمعية فقط
                BehindAssociationMembers.Clear();
                foreach (var member in allMembers.Where(m => behindAssociationMemberIds.Contains(m.MemberID)))
                {
                    BehindAssociationMembers.Add(member);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل الأعضاء: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void FilterMembers()
        {
            FilteredMembers.Clear();
            
            if (string.IsNullOrWhiteSpace(MemberSearchText))
            {
                // إذا كان البحث فارغ، عرض جميع الأعضاء
                foreach (var member in Members)
                {
                    FilteredMembers.Add(member);
                }
            }
            else
            {
                // تصفية الأعضاء حسب النص المدخل
                var searchText = MemberSearchText.Trim().ToLower();
                foreach (var member in Members)
                {
                    if (member.Name.ToLower().Contains(searchText) || 
                        (member.Phone != null && member.Phone.Contains(searchText)))
                    {
                        FilteredMembers.Add(member);
                    }
                }
            }
        }

        #endregion
    }
}
