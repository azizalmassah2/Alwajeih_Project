using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Members
{
    /// <summary>
    /// 📄 ViewModel لكشف حساب العضو
    /// </summary>
    public class MemberStatementViewModel : BaseViewModel
    {
        private readonly MemberRepository _memberRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly CollectionRepository _collectionRepository;
        private readonly AdvancePaymentRepository _advanceRepository;
        private readonly ArrearRepository _arrearRepository;
        private readonly VaultRepository _vaultRepository;

        private ObservableCollection<Member> _members;
        private Member _selectedMember;
        private ObservableCollection<SavingPlan> _memberPlans;
        private ObservableCollection<DailyCollection> _memberCollections;

        // الملخص المالي
        private decimal _totalExpected;
        private decimal _totalPaid;
        private decimal _totalAdvances;
        private decimal _totalWithdrawals;  // السحوبات
        private decimal _remaining;
        private decimal _totalArrears;
        private decimal _availableBalance;

        public MemberStatementViewModel()
        {
            _memberRepository = new MemberRepository();
            _planRepository = new SavingPlanRepository();
            _collectionRepository = new CollectionRepository();
            _advanceRepository = new AdvancePaymentRepository();
            _arrearRepository = new ArrearRepository();
            _vaultRepository = new VaultRepository();

            Members = new ObservableCollection<Member>();
            MemberPlans = new ObservableCollection<SavingPlan>();
            MemberCollections = new ObservableCollection<DailyCollection>();
            LoadCommand = new RelayCommand(ExecuteLoad, CanExecuteLoad);
            LoadMembers();
        }

        public MemberStatementViewModel(int memberId)
            : this()
        {
            // تحميل العضو المحدد
            var member = _memberRepository.GetById(memberId);
            if (member != null)
            {
                SelectedMember = member;
                LoadMemberPlans();
            }
        }

        public ObservableCollection<Member> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        public Member SelectedMember
        {
            get => _selectedMember;
            set
            {
                SetProperty(ref _selectedMember, value);
                ((RelayCommand)LoadCommand).RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<SavingPlan> MemberPlans
        {
            get => _memberPlans;
            set => SetProperty(ref _memberPlans, value);
        }

        public ObservableCollection<DailyCollection> MemberCollections
        {
            get => _memberCollections;
            set => SetProperty(ref _memberCollections, value);
        }

        public decimal TotalExpected
        {
            get => _totalExpected;
            set => SetProperty(ref _totalExpected, value);
        }

        public decimal TotalPaid
        {
            get => _totalPaid;
            set => SetProperty(ref _totalPaid, value);
        }

        public decimal TotalAdvances
        {
            get => _totalAdvances;
            set => SetProperty(ref _totalAdvances, value);
        }

        public decimal TotalWithdrawals
        {
            get => _totalWithdrawals;
            set => SetProperty(ref _totalWithdrawals, value);
        }

        public decimal Remaining
        {
            get => _remaining;
            set => SetProperty(ref _remaining, value);
        }

        public decimal TotalArrears
        {
            get => _totalArrears;
            set => SetProperty(ref _totalArrears, value);
        }

        public decimal AvailableBalance
        {
            get => _availableBalance;
            set => SetProperty(ref _availableBalance, value);
        }

        public ICommand LoadCommand { get; }

        private bool CanExecuteLoad(object parameter) => SelectedMember != null;

        private void ExecuteLoad(object parameter)
        {
            LoadMemberPlans();
        }

        private void LoadMemberPlans()
        {
            if (SelectedMember == null)
                return;

            try
            {
                var plans = _planRepository.GetByMemberId(SelectedMember.MemberID);
                MemberPlans.Clear();
                foreach (var plan in plans)
                    MemberPlans.Add(plan);

                // تحميل السدادات
                LoadMemberCollections();

                // حساب الملخص المالي
                CalculateFinancialSummary();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void LoadMemberCollections()
        {
            if (SelectedMember == null)
                return;

            try
            {
                MemberCollections.Clear();
                
                // جلب جميع السدادات للعضو من خلال أسهمه
                foreach (var plan in MemberPlans)
                {
                    var collections = _collectionRepository.GetByPlanId(plan.PlanID);
                    foreach (var collection in collections)
                    {
                        // إضافة معلومات إضافية
                        collection.MemberName = SelectedMember.Name;
                        collection.PlanNumber = plan.PlanNumber;
                        MemberCollections.Add(collection);
                    }
                }
                
                // ترتيب حسب التاريخ (الأحدث أولاً)
                var sortedCollections = MemberCollections.OrderByDescending(c => c.CollectionDate)
                    .ThenByDescending(c => c.CollectedAt).ToList();
                MemberCollections.Clear();
                foreach (var collection in sortedCollections)
                {
                    MemberCollections.Add(collection);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل السدادات: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void CalculateFinancialSummary()
        {
            if (SelectedMember == null)
                return;

            TotalExpected = 0;
            TotalPaid = 0;
            TotalAdvances = 0;
            TotalWithdrawals = 0;
            TotalArrears = 0;

            foreach (var plan in MemberPlans)
            {
                // الإجمالي المتوقع
                TotalExpected += plan.TotalAmount;

                // ما تم دفعه
                TotalPaid += _collectionRepository.GetTotalPaidForPlan(plan.PlanID);

                // السُلف
                TotalAdvances += _advanceRepository.GetTotalAdvanceForPlan(plan.PlanID);

                // المتأخرات
                TotalArrears += _arrearRepository.GetTotalArrearForPlan(plan.PlanID);
            }

            // السحوبات من الخزنة
            TotalWithdrawals = _vaultRepository.GetTotalMemberWithdrawals(SelectedMember.MemberID);

            // المتبقي عليه = الإجمالي المتوقع - ما تم دفعه
            Remaining = TotalExpected - TotalPaid;

            // الرصيد المتاح للسحب = الإجمالي المتوقع - السحوبات
            AvailableBalance = TotalExpected - TotalWithdrawals;
        }

        private void LoadMembers()
        {
            var members = _memberRepository.GetActive();
            foreach (var member in members)
                Members.Add(member);
        }
    }
}
