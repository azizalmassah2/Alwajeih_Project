using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;
using Alwajeih.Services;
using Alwajeih.ViewModels.Base;
using Alwajeih.Utilities;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.ViewModels.Dashboard
{
    /// <summary>
    /// ViewModel للوحة التحكم الرئيسية
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MemberRepository _memberRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly CollectionRepository _collectionRepository;
        private readonly VaultService _vaultService;
        private readonly ArrearRepository _arrearRepository;
        private readonly SystemSettingsRepository _settingsRepository;

        private int _totalMembers;
        private int _behindAssociationMembers;
        private int _activePlans;
        private decimal _vaultBalance;
        private decimal _todayDueCollections;
        private decimal _todayCollected;
        private int _totalArrears;
        private string _currentWeek;
        private string _currentDay;
        private decimal _totalExpectedCollection;
        private int _currentWeekArrearsCount;
        private decimal _collectionPercentage;

        public DashboardViewModel()
        {
            _memberRepository = new MemberRepository();
            _planRepository = new SavingPlanRepository();
            _collectionRepository = new CollectionRepository();
            _vaultService = new VaultService();
            _arrearRepository = new ArrearRepository();
            _settingsRepository = new SystemSettingsRepository();

            RefreshDataCommand = new RelayCommand(_ => RefreshData(), _ => true);
            
            // تحميل تاريخ البداية من الإعدادات أولاً
            LoadStartDateFromSettings();
            
            LoadDashboardData();
        }
        
        public ICommand RefreshDataCommand { get; }

        // الإحصائيات
        public int TotalMembers
        {
            get => _totalMembers;
            set => SetProperty(ref _totalMembers, value);
        }
        
        public int BehindAssociationMembers
        {
            get => _behindAssociationMembers;
            set => SetProperty(ref _behindAssociationMembers, value);
        }

        public int ActivePlans
        {
            get => _activePlans;
            set => SetProperty(ref _activePlans, value);
        }

        public decimal VaultBalance
        {
            get => _vaultBalance;
            set => SetProperty(ref _vaultBalance, value);
        }

        public decimal TodayDueCollections
        {
            get => _todayDueCollections;
            set => SetProperty(ref _todayDueCollections, value);
        }

        public decimal TodayCollected
        {
            get => _todayCollected;
            set => SetProperty(ref _todayCollected, value);
        }

        public int TotalArrears
        {
            get => _totalArrears;
            set => SetProperty(ref _totalArrears, value);
        }
        
        public string CurrentWeek
        {
            get => _currentWeek;
            set => SetProperty(ref _currentWeek, value);
        }
        
        public string CurrentDay
        {
            get => _currentDay;
            set => SetProperty(ref _currentDay, value);
        }
        
        public decimal TotalExpectedCollection
        {
            get => _totalExpectedCollection;
            set => SetProperty(ref _totalExpectedCollection, value);
        }
        
        public int CurrentWeekArrearsCount
        {
            get => _currentWeekArrearsCount;
            set => SetProperty(ref _currentWeekArrearsCount, value);
        }
        
        public decimal CollectionPercentage
        {
            get => _collectionPercentage;
            set => SetProperty(ref _collectionPercentage, value);
        }

        public ObservableCollection<SavingPlan> RecentPlans { get; set; } = new();
        public ObservableCollection<DailyCollection> RecentCollections { get; set; } = new();

        private void LoadDashboardData()
        {
            try
            {
                var today = DateTime.Now.Date;
                var currentWeek = WeekHelper.GetCurrentWeekNumber();
                var currentDay = WeekHelper.GetCurrentDayNumber();
                
                // 1️⃣ إجمالي الأعضاء النشطين (بدون أعضاء خلف الجمعية)
                var allMembers = _memberRepository.GetAll().ToList();
                TotalMembers = allMembers.Count(m => m.MemberType != MemberType.BehindAssociation);
                BehindAssociationMembers = allMembers.Count(m => m.MemberType == MemberType.BehindAssociation);
                
                // 2️⃣ الحصص النشطة (أعضاء الجمعية العاديين فقط - تم تصفيتهم بالفعل في GetActivePlans)
                var activePlans = _planRepository.GetActivePlans().ToList();
                ActivePlans = activePlans.Count;
                
                // 3️⃣ رصيد الخزنة
                VaultBalance = _vaultService.GetCurrentBalance();

                // 4️⃣ السوابق المتراكمة غير المسددة
                var accumulatedArrearsRepo = new AccumulatedArrearsRepository();
                var allAccumulatedArrears = accumulatedArrearsRepo.GetAll();
                TotalArrears = allAccumulatedArrears.Count(a => a.RemainingAmount > 0);
                
                // 5️⃣ الأسبوع واليوم الحالي
                CurrentWeek = $"الأسبوع {currentWeek}";
                CurrentDay = WeekHelper.GetDayName(currentDay);
                
                // 6️⃣ المبلغ المستحق اليوم (عدد الحصص النشطة × المبلغ اليومي)
                TodayDueCollections = activePlans.Sum(p => p.DailyAmount);
                
                // 7️⃣ المبلغ المحصّل اليوم (التحصيل العادي فقط - بدون سداد السابقات)
                var dailyCollectionRepo = new DailyCollectionRepository();
                var todayCollections = dailyCollectionRepo.GetByWeekAndDay(currentWeek, currentDay)
                    .Where(c => !c.IsCancelled)
                    .ToList();
                TodayCollected = todayCollections.Sum(c => c.AmountPaid);
                
                // 8️⃣ التحصيل المتوقع للأسبوع
                TotalExpectedCollection = activePlans.Sum(p => p.DailyAmount * 7);
                
                // 9️⃣ متأخرات الأسبوع الحالي (عدد الأعضاء الذين لديهم متأخرات)
                var arrearRepo = new ArrearRepository();
                var currentWeekArrears = arrearRepo.GetArrearsByWeek(currentWeek);
                CurrentWeekArrearsCount = currentWeekArrears
                    .Where(a => !a.IsPaid)
                    .Select(a => a.PlanID)
                    .Distinct()
                    .Count();
                
                // 🔟 نسبة التحصيل اليومية
                if (TodayDueCollections > 0)
                {
                    CollectionPercentage = Math.Round((TodayCollected / TodayDueCollections) * 100, 1);
                }
                else
                {
                    CollectionPercentage = 0;
                }

                // 1️⃣1️⃣ آخر الحصص النشطة (للعرض في الجدول)
                RecentPlans.Clear();
                var recentPlans = activePlans
                    .OrderByDescending(p => p.StartDate)
                    .Take(10);
                foreach (var plan in recentPlans)
                {
                    RecentPlans.Add(plan);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل بيانات اللوحة: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void RefreshData()
        {
            LoadStartDateFromSettings();
            LoadDashboardData();
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحميل تاريخ البداية: {ex.Message}");
            }
        }
    }
}
