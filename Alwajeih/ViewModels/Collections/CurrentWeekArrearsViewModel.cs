using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Utilities;
using Alwajeih.ViewModels.Base;
using Alwajeih.Data.Repositories;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.ViewModels.Collections
{
    /// <summary>
    /// ViewModel لواجهة المتأخرات - الأسبوع الحالي فقط
    /// </summary>
    public class CurrentWeekArrearsViewModel : BaseViewModel
    {
        private readonly ArrearRepository _arrearRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly ArrearService _arrearService;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;

        private int _currentWeekNumber;
        private ObservableCollection<MemberArrearSummary> _memberArrears;
        private MemberArrearSummary _selectedMember;
        private string _searchText;
        private bool _isLoading;

        public CurrentWeekArrearsViewModel()
        {
            _arrearRepository = new ArrearRepository();
            _planRepository = new SavingPlanRepository();
            _arrearService = new ArrearService();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;

            MemberArrears = new ObservableCollection<MemberArrearSummary>();

            // تحميل تاريخ البداية
            LoadStartDateFromSettings();

            // الأسبوع الحالي
            CurrentWeekNumber = WeekHelper.GetCurrentWeekNumber();

            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            PayArrearCommand = new RelayCommand(ExecutePayArrear, CanExecutePayArrear);

            LoadData();
        }

        #region Properties

        public int CurrentWeekNumber
        {
            get => _currentWeekNumber;
            set => SetProperty(ref _currentWeekNumber, value);
        }

        public ObservableCollection<MemberArrearSummary> MemberArrears
        {
            get => _memberArrears;
            set => SetProperty(ref _memberArrears, value);
        }

        public MemberArrearSummary SelectedMember
        {
            get => _selectedMember;
            set
            {
                SetProperty(ref _selectedMember, value);
                ((RelayCommand)PayArrearCommand).RaiseCanExecuteChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterData();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand PayArrearCommand { get; }

        #endregion

        #region Methods

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الإعدادات: {ex.Message}");
            }
        }

        private void LoadData()
        {
            try
            {
                IsLoading = true;

                // الحصول على جميع الأسهم النشطة
                var activePlans = _planRepository.GetActive();

                var memberArrearsList = new System.Collections.Generic.List<MemberArrearSummary>();

                foreach (var plan in activePlans)
                {
                    // حساب إجمالي متأخرات الأسبوع الحالي
                    decimal currentWeekArrears = _arrearService.GetCurrentWeekArrearsTotal(plan.PlanID, CurrentWeekNumber);

                    if (currentWeekArrears > 0)
                    {
                        // الحصول على تفاصيل المتأخرات اليومية
                        var dailyArrears = _arrearRepository.GetArrearsByPlanAndWeek(plan.PlanID, CurrentWeekNumber)
                            .Where(a => !a.IsPaid)
                            .OrderBy(a => a.DayNumber)
                            .ToList();

                        memberArrearsList.Add(new MemberArrearSummary
                        {
                            PlanID = plan.PlanID,
                            MemberName = plan.MemberName,
                            PlanNumber = plan.PlanNumber,
                            WeekNumber = CurrentWeekNumber,
                            TotalArrears = currentWeekArrears,
                            DaysCount = dailyArrears.Count,
                            DailyArrears = dailyArrears
                        });
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MemberArrears.Clear();
                    foreach (var item in memberArrearsList.OrderByDescending(m => m.TotalArrears))
                    {
                        MemberArrears.Add(item);
                    }
                });
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

        private void FilterData()
        {
            LoadData(); // إعادة تحميل مع الفلترة
        }

        private void ExecuteRefresh(object parameter)
        {
            CurrentWeekNumber = WeekHelper.GetCurrentWeekNumber();
            LoadData();
        }

        private bool CanExecutePayArrear(object parameter)
        {
            return SelectedMember != null && _authService.HasPermission("ManageCollections");
        }

        private void ExecutePayArrear(object parameter)
        {
            if (SelectedMember == null) return;

            // فتح نافذة السداد
            System.Windows.MessageBox.Show(
                $"سداد متأخرات العضو: {SelectedMember.MemberName}\n" +
                $"الإجمالي: {SelectedMember.TotalArrears:N2} ريال\n" +
                $"عدد الأيام: {SelectedMember.DaysCount}",
                "سداد متأخرات",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        #endregion
    }
}
