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
    /// ViewModel لواجهة السابقات - الأسابيع السابقة
    /// </summary>
    public class PreviousArrearsViewModel : BaseViewModel
    {
        private readonly ArrearRepository _arrearRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly ArrearService _arrearService;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;

        private int _selectedWeekNumber;
        private System.Collections.Generic.List<int> _availableWeeks;
        private ObservableCollection<MemberPreviousArrearSummary> _memberPreviousArrears;
        private MemberPreviousArrearSummary _selectedMember;
        private string _searchText;
        private bool _isLoading;

        public PreviousArrearsViewModel()
        {
            _arrearRepository = new ArrearRepository();
            _planRepository = new SavingPlanRepository();
            _arrearService = new ArrearService();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;

            MemberPreviousArrears = new ObservableCollection<MemberPreviousArrearSummary>();

            // تحميل تاريخ البداية
            LoadStartDateFromSettings();

            // تحميل الأسابيع المتاحة (الأسابيع السابقة فقط)
            LoadAvailableWeeks();

            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            PayPreviousArrearCommand = new RelayCommand(ExecutePayPreviousArrear, CanExecutePayPreviousArrear);

            if (AvailableWeeks != null && AvailableWeeks.Any())
            {
                SelectedWeekNumber = AvailableWeeks.First();
                LoadData();
            }
        }

        #region Properties

        public int SelectedWeekNumber
        {
            get => _selectedWeekNumber;
            set
            {
                if (SetProperty(ref _selectedWeekNumber, value))
                {
                    LoadData();
                }
            }
        }

        public System.Collections.Generic.List<int> AvailableWeeks
        {
            get => _availableWeeks;
            set => SetProperty(ref _availableWeeks, value);
        }

        public ObservableCollection<MemberPreviousArrearSummary> MemberPreviousArrears
        {
            get => _memberPreviousArrears;
            set => SetProperty(ref _memberPreviousArrears, value);
        }

        public MemberPreviousArrearSummary SelectedMember
        {
            get => _selectedMember;
            set
            {
                SetProperty(ref _selectedMember, value);
                ((RelayCommand)PayPreviousArrearCommand).RaiseCanExecuteChanged();
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
        public ICommand PayPreviousArrearCommand { get; }

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

        private void LoadAvailableWeeks()
        {
            try
            {
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                
                // الأسابيع السابقة فقط (من 1 إلى الأسبوع الحالي - 1)
                var weeks = new System.Collections.Generic.List<int>();
                for (int i = currentWeek - 1; i >= 1; i--)
                {
                    weeks.Add(i);
                }

                AvailableWeeks = weeks;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الأسابيع: {ex.Message}");
                AvailableWeeks = new System.Collections.Generic.List<int>();
            }
        }

        private void LoadData()
        {
            try
            {
                IsLoading = true;

                if (SelectedWeekNumber == 0) return;

                // الحصول على جميع الأسهم النشطة
                var activePlans = _planRepository.GetActive();

                var memberPreviousArrearsList = new System.Collections.Generic.List<MemberPreviousArrearSummary>();

                foreach (var plan in activePlans)
                {
                    // الحصول على السابقات للأسبوع المحدد
                    var previousArrears = _arrearRepository.GetPreviousArrearsByPlanId(plan.PlanID)
                        .Where(p => p.WeekNumber == SelectedWeekNumber && !p.IsPaid)
                        .ToList();

                    if (previousArrears.Any())
                    {
                        decimal totalArrears = previousArrears.Sum(p => p.RemainingAmount);

                        memberPreviousArrearsList.Add(new MemberPreviousArrearSummary
                        {
                            PlanID = plan.PlanID,
                            MemberName = plan.MemberName,
                            PlanNumber = plan.PlanNumber,
                            WeekNumber = SelectedWeekNumber,
                            TotalArrears = totalArrears,
                            PreviousArrears = previousArrears
                        });
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MemberPreviousArrears.Clear();
                    foreach (var item in memberPreviousArrearsList.OrderByDescending(m => m.TotalArrears))
                    {
                        MemberPreviousArrears.Add(item);
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
            LoadAvailableWeeks();
            LoadData();
        }

        private bool CanExecutePayPreviousArrear(object parameter)
        {
            return SelectedMember != null && _authService.HasPermission("ManageCollections");
        }

        private void ExecutePayPreviousArrear(object parameter)
        {
            if (SelectedMember == null) return;

            // فتح نافذة السداد
            System.Windows.MessageBox.Show(
                $"سداد سابقات العضو: {SelectedMember.MemberName}\n" +
                $"الأسبوع: {SelectedMember.WeekNumber}\n" +
                $"الإجمالي: {SelectedMember.TotalArrears:N2} ريال",
                "سداد سابقات",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        #endregion
    }
}
