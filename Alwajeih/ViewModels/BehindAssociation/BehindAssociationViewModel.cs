using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Models.BehindAssociation;
using Alwajeih.Services;
using Alwajeih.Services.BehindAssociation;
using Alwajeih.ViewModels.Base;
using Alwajeih.Data.Repositories;
using Alwajeih.Utilities;

namespace Alwajeih.ViewModels.BehindAssociation
{
    /// <summary>
    /// ViewModel لإدارة أعضاء "خلف الجمعية"
    /// </summary>
    public class BehindAssociationViewModel : BaseViewModel
    {
        private readonly BehindAssociationService _service;
        private readonly MemberRepository _memberRepository;
        private readonly AuthenticationService _authService;
        
        private ObservableCollection<BehindAssociationSummary> _membersSummaries;
        private BehindAssociationSummary _selectedMember;
        private ObservableCollection<BehindAssociationTransaction> _selectedMemberTransactions;
        private string _searchText;
        private bool _isLoading;
        
        // إحصائيات عامة
        private decimal _totalDeposits;
        private decimal _totalWithdrawals;
        private decimal _totalBalance;
        private int _membersCount;
        
        // نموذج التسجيل
        private int _selectedMemberId;
        private decimal _depositAmount;
        private PaymentSource _selectedPaymentSource;
        private string _referenceNumber;
        private string _notes;
        private bool _isDepositDialogOpen;
        
        public BehindAssociationViewModel()
        {
            _service = new BehindAssociationService();
            _memberRepository = new MemberRepository();
            _authService = AuthenticationService.Instance;
            
            MembersSummaries = new ObservableCollection<BehindAssociationSummary>();
            SelectedMemberTransactions = new ObservableCollection<BehindAssociationTransaction>();
            
            // الأوامر
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            OpenDepositDialogCommand = new RelayCommand(ExecuteOpenDepositDialog, CanExecuteOpenDepositDialog);
            RecordDepositCommand = new RelayCommand(ExecuteRecordDeposit, CanExecuteRecordDeposit);
            CancelDepositCommand = new RelayCommand(ExecuteCancelDeposit, _ => true);
            ViewMemberDetailsCommand = new RelayCommand(ExecuteViewMemberDetails, CanExecuteViewMemberDetails);
            
            // تحميل البيانات
            LoadData();
        }
        
        #region Properties
        
        public ObservableCollection<BehindAssociationSummary> MembersSummaries
        {
            get => _membersSummaries;
            set => SetProperty(ref _membersSummaries, value);
        }
        
        public BehindAssociationSummary SelectedMember
        {
            get => _selectedMember;
            set
            {
                if (SetProperty(ref _selectedMember, value))
                {
                    LoadMemberTransactions();
                }
            }
        }
        
        public ObservableCollection<BehindAssociationTransaction> SelectedMemberTransactions
        {
            get => _selectedMemberTransactions;
            set => SetProperty(ref _selectedMemberTransactions, value);
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterMembers();
                }
            }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        public decimal TotalDeposits
        {
            get => _totalDeposits;
            set => SetProperty(ref _totalDeposits, value);
        }
        
        public decimal TotalWithdrawals
        {
            get => _totalWithdrawals;
            set => SetProperty(ref _totalWithdrawals, value);
        }
        
        public decimal TotalBalance
        {
            get => _totalBalance;
            set => SetProperty(ref _totalBalance, value);
        }
        
        public int MembersCount
        {
            get => _membersCount;
            set => SetProperty(ref _membersCount, value);
        }
        
        // نموذج التسجيل
        public int SelectedMemberId
        {
            get => _selectedMemberId;
            set => SetProperty(ref _selectedMemberId, value);
        }
        
        public decimal DepositAmount
        {
            get => _depositAmount;
            set => SetProperty(ref _depositAmount, value);
        }
        
        public PaymentSource SelectedPaymentSource
        {
            get => _selectedPaymentSource;
            set => SetProperty(ref _selectedPaymentSource, value);
        }
        
        public string ReferenceNumber
        {
            get => _referenceNumber;
            set => SetProperty(ref _referenceNumber, value);
        }
        
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
        
        public bool IsDepositDialogOpen
        {
            get => _isDepositDialogOpen;
            set => SetProperty(ref _isDepositDialogOpen, value);
        }
        
        // قائمة الأعضاء (خلف الجمعية فقط)
        public ObservableCollection<Member> BehindAssociationMembers { get; set; }
        
        // قائمة مصادر الدفع
        public Array PaymentSources => Enum.GetValues(typeof(PaymentSource));
        
        #endregion
        
        #region Commands
        
        public ICommand RefreshCommand { get; }
        public ICommand OpenDepositDialogCommand { get; }
        public ICommand RecordDepositCommand { get; }
        public ICommand CancelDepositCommand { get; }
        public ICommand ViewMemberDetailsCommand { get; }
        
        #endregion
        
        #region Methods
        
        private void LoadData()
        {
            try
            {
                IsLoading = true;
                
                // تحميل جميع الملخصات
                var summaries = _service.GetAllMembersSummaries();
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MembersSummaries.Clear();
                    foreach (var summary in summaries)
                    {
                        MembersSummaries.Add(summary);
                    }
                });
                
                // تحميل الإحصائيات العامة
                LoadOverallSummary();
                
                // تحميل قائمة الأعضاء
                LoadBehindAssociationMembers();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void LoadOverallSummary()
        {
            var summary = _service.GetOverallSummary();
            TotalDeposits = summary.TotalDeposits;
            TotalWithdrawals = summary.TotalWithdrawals;
            TotalBalance = summary.TotalBalance;
            MembersCount = summary.MembersCount;
        }
        
        private void LoadBehindAssociationMembers()
        {
            var members = _memberRepository.GetAll()
                .Where(m => m.MemberType == MemberType.BehindAssociation && !m.IsArchived)
                .ToList();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                BehindAssociationMembers = new ObservableCollection<Member>(members);
                OnPropertyChanged(nameof(BehindAssociationMembers));
            });
        }
        
        private void LoadMemberTransactions()
        {
            if (SelectedMember == null)
            {
                SelectedMemberTransactions.Clear();
                return;
            }
            
            try
            {
                var transactions = _service.GetMemberTransactions(SelectedMember.MemberID);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedMemberTransactions.Clear();
                    foreach (var transaction in transactions)
                    {
                        SelectedMemberTransactions.Add(transaction);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل المعاملات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void FilterMembers()
        {
            // يمكن تطبيق فلتر البحث هنا إذا لزم الأمر
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                LoadData();
                return;
            }
            
            var filtered = _service.GetAllMembersSummaries()
                .Where(m => m.MemberName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           (m.Phone != null && m.Phone.Contains(SearchText)))
                .ToList();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MembersSummaries.Clear();
                foreach (var summary in filtered)
                {
                    MembersSummaries.Add(summary);
                }
            });
        }
        
        private void ExecuteRefresh(object parameter)
        {
            LoadData();
        }
        
        private void ExecuteOpenDepositDialog(object parameter)
        {
            // إعادة تعيين النموذج
            SelectedMemberId = 0;
            DepositAmount = 0;
            SelectedPaymentSource = PaymentSource.Cash;
            ReferenceNumber = string.Empty;
            Notes = string.Empty;
            
            IsDepositDialogOpen = true;
        }
        
        private bool CanExecuteOpenDepositDialog(object parameter)
        {
            return !IsDepositDialogOpen;
        }
        
        private void ExecuteRecordDeposit(object parameter)
        {
            try
            {
                if (SelectedMemberId == 0)
                {
                    System.Windows.MessageBox.Show("يرجى اختيار العضو", "تنبيه",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                if (DepositAmount <= 0)
                {
                    System.Windows.MessageBox.Show("يرجى إدخال مبلغ صحيح", "تنبيه",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                var result = _service.RecordDeposit(
                    SelectedMemberId,
                    DepositAmount,
                    SelectedPaymentSource,
                    ReferenceNumber,
                    Notes,
                    _authService.CurrentUser.UserID
                );
                
                if (result.Success)
                {
                    System.Windows.MessageBox.Show(result.Message, "نجاح",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    
                    IsDepositDialogOpen = false;
                    LoadData();
                }
                else
                {
                    System.Windows.MessageBox.Show(result.Message, "خطأ",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecuteRecordDeposit(object parameter)
        {
            return SelectedMemberId > 0 && DepositAmount > 0;
        }
        
        private void ExecuteCancelDeposit(object parameter)
        {
            IsDepositDialogOpen = false;
        }
        
        private void ExecuteViewMemberDetails(object parameter)
        {
            if (SelectedMember == null)
                return;
            
            // يمكن فتح نافذة تفاصيل العضو هنا
            LoadMemberTransactions();
        }
        
        private bool CanExecuteViewMemberDetails(object parameter)
        {
            return SelectedMember != null;
        }
        
        #endregion
    }
}
