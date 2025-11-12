using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Data.Repositories;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Finance
{
    /// <summary>
    /// 🏧 ViewModel للمدفوعات الخارجية (كريمي)
    /// </summary>
    public class ExternalPaymentViewModel : BaseViewModel
    {
        private readonly ExternalPaymentService _paymentService;
        private readonly ExternalPaymentRepository _paymentRepository;
        private readonly MemberRepository _memberRepository;
        private readonly AuthenticationService _authService;

        private ObservableCollection<ExternalPayment> _pendingPayments;
        private ObservableCollection<Member> _members;
        private Member _selectedMember;
        private string _referenceNumber;
        private decimal _amount;
        private DateTime _paymentDate = DateTime.Now;
        private PaymentSource _paymentSource = PaymentSource.Karimi;
        private string _notes;

        public ExternalPaymentViewModel()
        {
            _paymentService = new ExternalPaymentService();
            _paymentRepository = new ExternalPaymentRepository();
            _memberRepository = new MemberRepository();
            _authService = AuthenticationService.Instance;

            PendingPayments = new ObservableCollection<ExternalPayment>();
            Members = new ObservableCollection<Member>();

            RegisterPaymentCommand = new RelayCommand(ExecuteRegisterPayment, CanExecuteRegister);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);

            LoadMembers();
            LoadPendingPayments();
        }

        #region Properties

        public ObservableCollection<ExternalPayment> PendingPayments
        {
            get => _pendingPayments;
            set => SetProperty(ref _pendingPayments, value);
        }

        public ObservableCollection<Member> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        public Member? SelectedMember
        {
            get => _selectedMember;
            set => SetProperty(ref _selectedMember, value);
        }

        public string ReferenceNumber
        {
            get => _referenceNumber;
            set
            {
                SetProperty(ref _referenceNumber, value);
                ((RelayCommand)RegisterPaymentCommand).RaiseCanExecuteChanged();
            }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                SetProperty(ref _amount, value);
                ((RelayCommand)RegisterPaymentCommand).RaiseCanExecuteChanged();
            }
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
        }

        public PaymentSource PaymentSource
        {
            get => _paymentSource;
            set => SetProperty(ref _paymentSource, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        #endregion

        #region Commands

        public ICommand RegisterPaymentCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private void LoadMembers()
        {
            try
            {
                var members = _memberRepository.GetAll();
                Members.Clear();
                foreach (var member in members)
                {
                    Members.Add(member);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل الأعضاء: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteRegister(object parameter)
        {
            return SelectedMember != null &&
                   !string.IsNullOrWhiteSpace(ReferenceNumber) && 
                   Amount > 0 && 
                   _authService.HasPermission("RegisterExternalPayment");
        }

        private void ExecuteRegisterPayment(object parameter)
        {
            try
            {
                if (SelectedMember == null)
                {
                    System.Windows.MessageBox.Show("⚠️ يرجى اختيار العضو", "تنبيه",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var userId = _authService.CurrentUser?.UserID ?? 0;
                
                // إنشاء الدفعة مع ربطها بالعضو
                var payment = new ExternalPayment
                {
                    MemberID = SelectedMember.MemberID,
                    ReferenceNumber = ReferenceNumber,
                    Amount = Amount,
                    PaymentDate = PaymentDate,
                    PaymentSource = PaymentSource,
                    Notes = Notes,
                    CreatedBy = userId
                };

                _paymentRepository.Add(payment);

                System.Windows.MessageBox.Show(
                    $"✅ تم تسجيل الدفعة بنجاح!\n\n" +
                    $"👤 العضو: {SelectedMember.Name}\n" +
                    $"🏧 المرجع: {ReferenceNumber}\n" +
                    $"💰 المبلغ: {Amount:N2} ريال\n" +
                    $"📅 التاريخ: {PaymentDate:yyyy-MM-dd}",
                    "نجاح",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                LoadPendingPayments();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadPendingPayments();
        }

        #endregion

        #region Helper Methods

        private void LoadPendingPayments()
        {
            try
            {
                var payments = _paymentRepository.GetPending();
                PendingPayments.Clear();
                foreach (var payment in payments)
                {
                    PendingPayments.Add(payment);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل الدفعات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            SelectedMember = null;
            ReferenceNumber = string.Empty;
            Amount = 0;
            PaymentDate = DateTime.Now;
            PaymentSource = PaymentSource.Karimi;
            Notes = string.Empty;
        }

        #endregion
    }
}
