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
    /// ViewModel لواجهة المتأخرات والسابقات
    /// </summary>
    public class ArrearsAndBalanceViewModel : BaseViewModel
    {
        private readonly SavingPlanRepository _planRepo;
        private readonly DailyCollectionRepository _collectionRepo;
        private readonly SystemSettingsRepository _settingsRepository;
        
        private string _searchText;
        private int _selectedWeekFilter;
        private int _selectedDayFilter;
        private System.Collections.Generic.List<int> _weeks;
        private System.Collections.Generic.List<(int, string)> _days;
        private decimal _totalArrears;
        private decimal _totalBalance;
        private ObservableCollection<ArrearItem> _arrearsItems;
        private ObservableCollection<ArrearItem> _filteredArrearsItems;
        private ObservableCollection<WeeklyBalanceItem> _weeklyBalances;
        private ArrearItem _selectedArrear;
        private decimal _paymentAmount;
        private bool _canPayArrear;
        private bool _isLoading;

        public ArrearsAndBalanceViewModel()
        {
            _planRepo = new SavingPlanRepository();
            _collectionRepo = new DailyCollectionRepository();
            _settingsRepository = new SystemSettingsRepository();
            
            ArrearsItems = new ObservableCollection<ArrearItem>();
            FilteredArrearsItems = new ObservableCollection<ArrearItem>();
            WeeklyBalances = new ObservableCollection<WeeklyBalanceItem>();
            
            // تحميل تاريخ البداية من الإعدادات
            LoadStartDateFromSettings();
            
            // تحميل قائمة الأسابيع
            Weeks = new System.Collections.Generic.List<int> { 0 }; // 0 = الكل
            Weeks.AddRange(WeekHelper.GetAllWeeks());
            
            // تحميل قائمة الأيام
            Days = new System.Collections.Generic.List<(int, string)> { (0, "الكل") };
            Days.AddRange(WeekHelper.GetAllDays());
            
            // القيم الافتراضية: الأسبوع واليوم الحاليين
            SelectedWeekFilter = WeekHelper.GetCurrentWeekNumber();
            SelectedDayFilter = WeekHelper.GetCurrentDayNumber();
            
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            PayArrearCommand = new RelayCommand(ExecutePayArrear, _ => CanPayArrear);
            
            // تحميل البيانات بشكل غير متزامن
            System.Threading.Tasks.Task.Run(() => LoadData());
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterArrears();
                }
            }
        }

        public int SelectedWeekFilter
        {
            get => _selectedWeekFilter;
            set
            {
                if (SetProperty(ref _selectedWeekFilter, value))
                {
                    FilterArrears();
                }
            }
        }

        public int SelectedDayFilter
        {
            get => _selectedDayFilter;
            set
            {
                if (SetProperty(ref _selectedDayFilter, value))
                {
                    FilterArrears();
                }
            }
        }

        public System.Collections.Generic.List<int> Weeks
        {
            get => _weeks;
            set => SetProperty(ref _weeks, value);
        }

        public System.Collections.Generic.List<(int, string)> Days
        {
            get => _days;
            set => SetProperty(ref _days, value);
        }

        public decimal TotalArrears
        {
            get => _totalArrears;
            set => SetProperty(ref _totalArrears, value);
        }

        public decimal TotalBalance
        {
            get => _totalBalance;
            set => SetProperty(ref _totalBalance, value);
        }

        public ObservableCollection<ArrearItem> ArrearsItems
        {
            get => _arrearsItems;
            set => SetProperty(ref _arrearsItems, value);
        }

        public ObservableCollection<ArrearItem> FilteredArrearsItems
        {
            get => _filteredArrearsItems;
            set => SetProperty(ref _filteredArrearsItems, value);
        }

        public ObservableCollection<WeeklyBalanceItem> WeeklyBalances
        {
            get => _weeklyBalances;
            set => SetProperty(ref _weeklyBalances, value);
        }

        public ArrearItem SelectedArrear
        {
            get => _selectedArrear;
            set
            {
                if (SetProperty(ref _selectedArrear, value))
                {
                    if (_selectedArrear != null)
                    {
                        PaymentAmount = _selectedArrear.ArrearAmount;
                    }
                    UpdateCanPayArrear();
                }
            }
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                if (SetProperty(ref _paymentAmount, value))
                {
                    UpdateCanPayArrear();
                }
            }
        }

        public bool CanPayArrear
        {
            get => _canPayArrear;
            set => SetProperty(ref _canPayArrear, value);
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

        #region Command Implementations

        private void ExecuteRefresh(object parameter)
        {
            LoadData();
        }

        private void ExecutePayArrear(object parameter)
        {
            try
            {
                if (SelectedArrear == null || PaymentAmount <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يرجى اختيار متأخرة وإدخال مبلغ صحيح",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning
                    );
                    return;
                }

                if (PaymentAmount > SelectedArrear.ArrearAmount)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"المبلغ المدخل ({PaymentAmount:N2}) أكبر من المتأخر ({SelectedArrear.ArrearAmount:N2})\nهل تريد المتابعة؟",
                        "تأكيد",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question
                    );
                    
                    if (result != System.Windows.MessageBoxResult.Yes)
                        return;
                }

                // إنشاء تحصيل جديد
                var collection = new DailyCollection
                {
                    PlanID = SelectedArrear.PlanID,
                    WeekNumber = SelectedArrear.WeekNumber,
                    DayNumber = SelectedArrear.DayNumber,
                    AmountPaid = PaymentAmount,
                    CollectionDate = DateTime.Now,
                    CollectedAt = DateTime.Now,
                    PaymentType = PaymentType.Cash,
                    PaymentSource = PaymentSource.Cash,
                    CollectedBy = 1,
                    Notes = "دفع متأخرة"
                };

                _collectionRepo.Add(collection);

                System.Windows.MessageBox.Show(
                    $"✅ تم دفع المتأخرة بنجاح\nالمبلغ: {PaymentAmount:N2} ريال",
                    "نجح",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information
                );

                // تحديث القائمة
                LoadData();
                SelectedArrear = null;
                PaymentAmount = 0;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في دفع المتأخرة: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        #endregion

        #region Helper Methods

        private void LoadData()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => IsLoading = true);
            
            try
            {
                LoadArrears();
                LoadPreviousBalances();
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsLoading = false);
            }
        }

        private void LoadArrears()
        {
            try
            {
                var tempItems = new System.Collections.Generic.List<ArrearItem>();
                
                var currentWeek = WeekHelper.GetCurrentWeekNumber();
                var currentDay = WeekHelper.GetCurrentDayNumber();
                
                // جلب جميع الخطط النشطة مرة واحدة
                var allActivePlans = _planRepo.GetAll().Where(p => p.IsActive).ToList();
                
                if (allActivePlans.Count == 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ArrearsItems.Clear();
                        FilteredArrearsItems.Clear();
                        TotalArrears = 0;
                    });
                    return;
                }
                
                // جلب جميع التحصيلات حتى الأسبوع الحالي مرة واحدة
                var allCollections = _collectionRepo.GetAll()
                    .Where(c => c.WeekNumber <= currentWeek)
                    .ToList();
                
                // تجميع التحصيلات حسب PlanID, Week, Day للوصول السريع
                var collectionsLookup = allCollections
                    .GroupBy(c => new { c.PlanID, c.WeekNumber, c.DayNumber })
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(c => c.AmountPaid)
                    );
                
                // حساب المتأخرات بشكل أكثر كفاءة
                // بدلاً من 3 حلقات متداخلة، نستخدم LINQ
                var arrears = from plan in allActivePlans
                             from week in Enumerable.Range(1, currentWeek)
                             let maxDay = (week == currentWeek) ? currentDay : WeekHelper.DaysPerWeek
                             from day in Enumerable.Range(1, maxDay)
                             where plan.CollectionDays.Contains(day)
                             let key = new { PlanID = plan.PlanID, WeekNumber = week, DayNumber = day }
                             let collected = collectionsLookup.ContainsKey(key) ? collectionsLookup[key] : 0
                             let expected = plan.DailyAmount
                             let arrear = expected - collected
                             where arrear > 0
                             select new ArrearItem
                             {
                                 PlanID = plan.PlanID,
                                 MemberName = plan.MemberName,
                                 WeekNumber = week,
                                 DayNumber = day,
                                 DayName = WeekHelper.GetArabicDayName(day),
                                 ExpectedAmount = expected,
                                 PaidAmount = collected,
                                 Date = WeekHelper.GetDateFromWeekAndDay(week, day)
                             };
                
                tempItems = arrears.ToList();
                
                // تحديث الـ UI في الـ UI Thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ArrearsItems.Clear();
                    foreach (var item in tempItems)
                    {
                        ArrearsItems.Add(item);
                    }
                    TotalArrears = ArrearsItems.Sum(a => a.ArrearAmount);
                    FilterArrears();
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"❌ خطأ في تحميل المتأخرات: {ex.Message}",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                });
            }
        }

        private void LoadPreviousBalances()
        {
            try
            {
                var tempBalances = new System.Collections.Generic.List<WeeklyBalanceItem>();
                
                var currentWeek = WeekHelper.GetCurrentWeekNumber();
                decimal cumulativeBalance = 0;
                
                // جلب جميع التحصيلات مرة واحدة
                var allCollections = _collectionRepo.GetAll()
                    .Where(c => c.WeekNumber < currentWeek)
                    .ToList();
                
                // جلب جميع الخطط النشطة مرة واحدة
                var allActivePlans = _planRepo.GetAll()
                    .Where(p => p.IsActive)
                    .ToList();
                
                // تجميع التحصيلات حسب الأسبوع
                var collectionsByWeek = allCollections
                    .GroupBy(c => c.WeekNumber)
                    .ToDictionary(g => g.Key, g => g.Sum(c => c.AmountPaid));
                
                for (int week = 1; week < currentWeek; week++)
                {
                    decimal income = collectionsByWeek.ContainsKey(week) ? collectionsByWeek[week] : 0;
                    
                    // حساب المستحقات لهذا الأسبوع
                    decimal dues = 0;
                    foreach (var plan in allActivePlans)
                    {
                        dues += plan.DailyAmount * plan.CollectionDays.Count;
                    }
                    
                    decimal balance = income - dues;
                    cumulativeBalance += balance;
                    
                    tempBalances.Add(new WeeklyBalanceItem
                    {
                        WeekNumber = week,
                        Income = income,
                        Dues = dues,
                        CumulativeBalance = cumulativeBalance
                    });
                }
                
                // تحديث الـ UI في الـ UI Thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    WeeklyBalances.Clear();
                    foreach (var item in tempBalances)
                    {
                        WeeklyBalances.Add(item);
                    }
                    TotalBalance = cumulativeBalance;
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"❌ خطأ في تحميل السابقات: {ex.Message}",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                });
            }
        }

        private void FilterArrears()
        {
            FilteredArrearsItems.Clear();
            
            var filtered = ArrearsItems.AsEnumerable();
            
            // فلتر حسب الأسبوع
            if (SelectedWeekFilter > 0)
            {
                filtered = filtered.Where(a => a.WeekNumber == SelectedWeekFilter);
            }
            
            // فلتر حسب اليوم
            if (SelectedDayFilter > 0)
            {
                filtered = filtered.Where(a => a.DayNumber == SelectedDayFilter);
            }
            
            // فلتر حسب النص
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(a => a.MemberName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            
            foreach (var item in filtered)
            {
                FilteredArrearsItems.Add(item);
            }
        }

        private void UpdateCanPayArrear()
        {
            CanPayArrear = SelectedArrear != null && PaymentAmount > 0;
        }

        /// <summary>
        /// تحميل تاريخ البداية من الإعدادات
        /// </summary>
        private void LoadStartDateFromSettings()
        {
            try
            {
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    WeekHelper.StartDate = settings.StartDate;
                    System.Diagnostics.Debug.WriteLine($"✅ [المتأخرات] تم تحميل تاريخ بداية الجمعية: {settings.StartDate:yyyy-MM-dd}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ [المتأخرات] لم يتم العثور على إعدادات");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [المتأخرات] خطأ في تحميل تاريخ البداية: {ex.Message}");
            }
        }

        #endregion
    }
}
