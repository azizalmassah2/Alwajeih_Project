using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Utilities;
using Alwajeih.Utilities.Helpers;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Collections
{
    public class WeeklyCollectionViewModel : BaseViewModel
    {
        private readonly CollectionService _collectionService;
        private readonly SavingPlanRepository _planRepository;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;

        private ObservableCollection<SavingPlan> _weeklyPlans;
        private SavingPlan? _selectedPlan;
        private decimal _amountPaid;
        private PaymentSource _paymentSource = PaymentSource.Cash;
        private List<int> _weeks;
        private int _selectedWeek = 1;
        private string _currentWeekDisplay;
        private string _weeklySearchText = string.Empty;
        private ObservableCollection<SavingPlan> _allWeeklyPlans;

        public WeeklyCollectionViewModel()
        {
            _collectionService = new CollectionService();
            _planRepository = new SavingPlanRepository();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;

            WeeklyPlans = new ObservableCollection<SavingPlan>();
            
            LoadStartDateFromSettings();
            
            Weeks = WeekHelper.GetAllWeeks();
            SelectedWeek = WeekHelper.GetCurrentWeekNumber();

            RecordPaymentCommand = new RelayCommand(ExecuteRecordPayment, CanExecuteRecord);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);

            UpdateCurrentWeekDisplay();
            LoadWeeklyPlans();
        }

        #region Properties

        public ObservableCollection<SavingPlan> WeeklyPlans
        {
            get => _weeklyPlans;
            set => SetProperty(ref _weeklyPlans, value);
        }

        public SavingPlan? SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                SetProperty(ref _selectedPlan, value);
                if (value != null)
                {
                    AmountPaid = value.DailyAmount * 7;
                }
                ((RelayCommand)RecordPaymentCommand).RaiseCanExecuteChanged();
            }
        }

        public decimal AmountPaid
        {
            get => _amountPaid;
            set => SetProperty(ref _amountPaid, value);
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

        public List<int> Weeks
        {
            get => _weeks;
            set => SetProperty(ref _weeks, value);
        }

        public int SelectedWeek
        {
            get => _selectedWeek;
            set
            {
                SetProperty(ref _selectedWeek, value);
                UpdateCurrentWeekDisplay();
                LoadWeeklyPlans();
            }
        }

        public string CurrentWeekDisplay
        {
            get => _currentWeekDisplay;
            set => SetProperty(ref _currentWeekDisplay, value);
        }
        
        public string WeeklySearchText
        {
            get => _weeklySearchText;
            set
            {
                if (SetProperty(ref _weeklySearchText, value))
                {
                    FilterWeeklyPlans();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand RecordPaymentCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanExecuteRecord(object parameter)
        {
            return SelectedPlan != null && AmountPaid > 0 && _authService.HasPermission("RecordCollection");
        }

        private void ExecuteRecordPayment(object parameter)
        {
            try
            {
                if (SelectedPlan == null) return;

                var userId = _authService.CurrentUser?.UserID ?? 0;

                var collection = new DailyCollection
                {
                    PlanID = SelectedPlan.PlanID,
                    CollectionDate = DateTime.Now,
                    WeekNumber = SelectedWeek,
                    DayNumber = 1,
                    AmountPaid = AmountPaid,
                    PaymentType = PaymentType.Cash,
                    PaymentSource = PaymentSource,
                    CollectedBy = userId,
                };

                var result = _collectionService.RecordCollectionWithWeek(collection);

                if (result.Success)
                {
                    System.Windows.MessageBox.Show(
                        $"✅ تم تسجيل الدفع الأسبوعي بنجاح!\n\n" +
                        $"العضو: {SelectedPlan.MemberName}\n" +
                        $"الأسبوع: {SelectedWeek}\n" +
                        $"المبلغ: {AmountPaid:N2} ريال",
                        "نجاح",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );

                    LoadWeeklyPlans();
                    SelectedPlan = null;
                    AmountPaid = 0;
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"❌ {result.Message}",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                }
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

        private void ExecuteRefresh(object parameter)
        {
            LoadWeeklyPlans();
        }

        #endregion

        #region Helper Methods

        private void LoadStartDateFromSettings()
        {
            try
            {
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    WeekHelper.StartDate = settings.StartDate;
                }
            }
            catch { }
        }

        private void LoadWeeklyPlans()
        {
            try
            {
                var allPlans = _planRepository.GetAllActive()
                    .Where(p => p.CollectionFrequency == CollectionFrequency.Weekly)
                    .ToList();

                foreach (var plan in allPlans)
                {
                    plan.TotalPaid = _collectionService.GetTotalCollectedForPlan(plan.PlanID);
                    plan.RemainingBalance = plan.TotalAmount - plan.TotalPaid;
                }
                
                _allWeeklyPlans = new ObservableCollection<SavingPlan>(allPlans);
                FilterWeeklyPlans();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل الخطط: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }
        
        private void FilterWeeklyPlans()
        {
            if (_allWeeklyPlans == null) return;
            
            var filtered = _allWeeklyPlans.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(WeeklySearchText))
            {
                var searchLower = WeeklySearchText.Trim().ToLower();
                filtered = filtered.Where(p => 
                    p.MemberName?.ToLower().Contains(searchLower) == true ||
                    p.PlanNumber.ToString().Contains(searchLower));
            }
            
            WeeklyPlans.Clear();
            foreach (var plan in filtered)
            {
                WeeklyPlans.Add(plan);
            }
        }

        private void UpdateCurrentWeekDisplay()
        {
            CurrentWeekDisplay = $"الأسبوع {SelectedWeek}";
        }

        #endregion
    }
}
