using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;
using Alwajeih.Utilities.Helpers;
using Alwajeih.ViewModels.Base;
using Alwajeih.Utilities;

namespace Alwajeih.ViewModels.Collections
{
    /// <summary>
    /// ViewModel لملخص اليوم
    /// </summary>
    public class DailySummaryViewModel : BaseViewModel
    {
        private readonly SavingPlanRepository _planRepo;
        private readonly DailyCollectionRepository _collectionRepo;
        
        private int _currentWeek;
        private int _currentDay;
        private string _dayName;
        private DateTime _currentDate;
        private decimal _totalExpected;
        private decimal _totalCollected;
        private decimal _totalRemaining;
        private decimal _actualCashCollected;
        private decimal _difference;
        private int _totalMembers;
        private int _paidMembers;
        private int _unpaidMembers;
        private double _collectionPercentage;
        
        // تفاصيل التحصيل
        private decimal _todayPayments;        // سداد اليوم العادي
        private decimal _arrearsPayments;      // سداد المتأخرات
        private decimal _previousBalancePayments; // سداد السوابق
        private decimal _otherTransactions;    // الخرجيات والمفقودات
        private decimal _totalDayCollection;   // الإجمالي المحصل اليوم
        
        private ObservableCollection<DailySummaryItem> _summaryItems;

        public DailySummaryViewModel()
        {
            _planRepo = new SavingPlanRepository();
            _collectionRepo = new DailyCollectionRepository();
            
            SummaryItems = new ObservableCollection<DailySummaryItem>();
            
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            
            // تحميل بيانات اليوم الحالي
            var (week, day) = WeekHelper.GetCurrentWeekAndDay();
            LoadDailySummary(week, day);
        }

        #region Properties

        public int CurrentWeek
        {
            get => _currentWeek;
            set => SetProperty(ref _currentWeek, value);
        }

        public int CurrentDay
        {
            get => _currentDay;
            set => SetProperty(ref _currentDay, value);
        }

        public string DayName
        {
            get => _dayName;
            set => SetProperty(ref _dayName, value);
        }

        public DateTime CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        public decimal TotalExpected
        {
            get => _totalExpected;
            set => SetProperty(ref _totalExpected, value);
        }

        public decimal TotalCollected
        {
            get => _totalCollected;
            set => SetProperty(ref _totalCollected, value);
        }

        public decimal TotalRemaining
        {
            get => _totalRemaining;
            set => SetProperty(ref _totalRemaining, value);
        }

        public decimal ActualCashCollected
        {
            get => _actualCashCollected;
            set
            {
                if (SetProperty(ref _actualCashCollected, value))
                {
                    // حساب الفرق تلقائياً عند تغيير المبلغ الفعلي
                    // الفرق = الفعلي - المحصل (اليوم + متأخرات + سوابق)
                    Difference = ActualCashCollected - TotalDayCollection;
                    OnPropertyChanged(nameof(HasDifference));
                }
            }
        }

        public decimal Difference
        {
            get => _difference;
            set => SetProperty(ref _difference, value);
        }

        public bool HasDifference => Math.Abs(Difference) > 0.01m;

        public int TotalMembers
        {
            get => _totalMembers;
            set => SetProperty(ref _totalMembers, value);
        }

        public int PaidMembers
        {
            get => _paidMembers;
            set => SetProperty(ref _paidMembers, value);
        }

        public int UnpaidMembers
        {
            get => _unpaidMembers;
            set => SetProperty(ref _unpaidMembers, value);
        }

        public ObservableCollection<DailySummaryItem> SummaryItems
        {
            get => _summaryItems;
            set => SetProperty(ref _summaryItems, value);
        }

        public double CollectionPercentage
        {
            get => _collectionPercentage;
            set => SetProperty(ref _collectionPercentage, value);
        }

        public decimal TodayPayments
        {
            get => _todayPayments;
            set => SetProperty(ref _todayPayments, value);
        }

        public decimal ArrearsPayments
        {
            get => _arrearsPayments;
            set => SetProperty(ref _arrearsPayments, value);
        }

        public decimal PreviousBalancePayments
        {
            get => _previousBalancePayments;
            set => SetProperty(ref _previousBalancePayments, value);
        }

        public decimal TotalDayCollection
        {
            get => _totalDayCollection;
            set => SetProperty(ref _totalDayCollection, value);
        }
        
        public decimal OtherTransactions
        {
            get => _otherTransactions;
            set => SetProperty(ref _otherTransactions, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private void ExecuteRefresh(object parameter)
        {
            LoadDailySummary(CurrentWeek, CurrentDay);
        }

        #endregion

        #region Helper Methods

        public void LoadDailySummary(int weekNumber, int dayNumber)
        {
            try
            {
                CurrentWeek = weekNumber;
                CurrentDay = dayNumber;
                DayName = WeekHelper.GetArabicDayName(dayNumber);
                CurrentDate = WeekHelper.GetDateFromWeekAndDay(weekNumber, dayNumber);

                SummaryItems.Clear();

                // جلب جميع الخطط النشطة (بغض النظر عن الدفع)
                var allActivePlans = _planRepo.GetDueForWeek(weekNumber)
                    .Where(p => p.CollectionDays == null || p.CollectionDays.Count == 0 || p.CollectionDays.Contains(dayNumber))
                    .ToList();
                
                // جلب التحصيلات لهذا اليوم
                var collections = _collectionRepo.GetCollectionsByWeekAndDay(weekNumber, dayNumber);

                TotalExpected = 0;
                TotalCollected = 0;
                TotalMembers = allActivePlans.Count;
                PaidMembers = 0;
                UnpaidMembers = 0;

                decimal tempExpected = 0;
                decimal tempCollected = 0;
                
                System.Diagnostics.Debug.WriteLine($"عدد الخطط النشطة: {allActivePlans.Count}");
                System.Diagnostics.Debug.WriteLine($"عدد التحصيلات: {collections.Count}");
                
                foreach (var plan in allActivePlans)
                {
                    var collection = collections.FirstOrDefault(c => c.PlanID == plan.PlanID);
                    decimal paidAmount = collection?.AmountPaid ?? 0;
                    bool isPaid = paidAmount >= plan.DailyAmount;

                    System.Diagnostics.Debug.WriteLine($"{plan.MemberName}: مطلوب={plan.DailyAmount:N2}, مدفوع={paidAmount:N2}");

                    if (isPaid)
                        PaidMembers++;
                    else
                        UnpaidMembers++;

                    var item = new DailySummaryItem
                    {
                        PlanID = plan.PlanID,
                        MemberName = plan.MemberName,
                        ExpectedAmount = plan.DailyAmount,
                        PaidAmount = paidAmount,
                        PaymentType = GetPaymentTypeInArabic(collection?.PaymentType),
                        Notes = collection?.Notes ?? ""
                    };

                    SummaryItems.Add(item);

                    // المبلغ المطلوب اليوم (ثابت - سداد اليوم فقط)
                    tempExpected += plan.DailyAmount;
                    
                    // المبلغ المحصل من سداد اليوم فقط (للحساب)
                    tempCollected += paidAmount;
                }

                // تحديث القيم بعد الحساب
                TotalExpected = tempExpected;
                TotalCollected = tempCollected;
                
                // المتأخرات = المطلوب - المحصل من سداد اليوم فقط
                TotalRemaining = TotalExpected - TotalCollected;
                
                // حساب تفاصيل التحصيل لهذا اليوم (اليوم + متأخرات + سوابق)
                CalculateDayCollectionDetails(weekNumber, dayNumber);
                
                // Debug: عرض القيم
                System.Diagnostics.Debug.WriteLine($"=== ملخص اليوم ===");
                System.Diagnostics.Debug.WriteLine($"المبلغ المطلوب اليوم: {TotalExpected:N2}");
                System.Diagnostics.Debug.WriteLine($"المبلغ المحصل (سداد اليوم): {TotalCollected:N2}");
                System.Diagnostics.Debug.WriteLine($"المبلغ المحصل (الكل): {TotalDayCollection:N2}");
                System.Diagnostics.Debug.WriteLine($"المتأخرات: {TotalRemaining:N2}");
                
                // المبلغ الفعلي يتم إدخاله يدوياً من قبل المستخدم
                // يبدأ بصفر ويقوم المستخدم بإدخال المبلغ الذي بيده
                ActualCashCollected = 0;
                
                // حساب نسبة التحصيل (بناءً على سداد اليوم فقط)
                CollectionPercentage = TotalExpected > 0 ? (double)(TotalCollected / TotalExpected) * 100 : 0;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل ملخص اليوم: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void CalculateDayCollectionDetails(int weekNumber, int dayNumber)
        {
            try
            {
                // جلب التحصيلات لهذا اليوم من قاعدة البيانات
                var collections = _collectionRepo.GetCollectionsByWeekAndDay(weekNumber, dayNumber);
                
                // تصنيف التحصيلات من DailyCollection
                TodayPayments = 0;           // التحصيل اليومي فقط
                ArrearsPayments = 0;         // سداد متأخرات اليوم (من DailyArrears)
                PreviousBalancePayments = 0; // سداد السابقات (من AccumulatedArrearPayments)
                OtherTransactions = 0;
                
                // التحصيل اليومي (AmountPaid)
                TodayPayments = collections.Sum(c => c.AmountPaid);
                
                // سداد السابقات (من AccumulatedArrears - المبالغ المدفوعة في هذا الأسبوع)
                // ملاحظة: لا يمكن تحديد اليوم المحدد، لذلك نقرأ إجمالي الأسبوع
                var accumulatedArrearsRepo = new Data.Repositories.AccumulatedArrearsRepository();
                PreviousBalancePayments = accumulatedArrearsRepo.GetAll()
                    .Where(a => a.LastWeekNumber == weekNumber)
                    .Sum(a => a.PaidAmount);
                
                // سداد متأخرات اليوم: نجلبها من DailyArrears التي تم سدادها اليوم
                var arrearRepo = new ArrearRepository();
                var todayArrears = arrearRepo.GetArrearsByWeek(weekNumber)
                    .Where(a => a.IsPaid && a.PaidDate.HasValue && 
                                a.PaidDate.Value.Date == WeekHelper.GetDateFromWeekAndDay(weekNumber, dayNumber).Date)
                    .ToList();
                ArrearsPayments = todayArrears.Sum(a => a.PaidAmount);
                
                // حساب الخرجيات والمفقودات
                var otherTransactionRepo = new OtherTransactionRepository();
                var todayOtherTransactions = otherTransactionRepo.GetTotalByWeekAndDay(weekNumber, dayNumber);
                OtherTransactions = todayOtherTransactions;
                
                // الإجمالي المحصل اليوم (ما في يد المستخدم)
                TotalDayCollection = TodayPayments + ArrearsPayments + PreviousBalancePayments;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في حساب تفاصيل التحصيل: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// تحويل نوع الدفع إلى العربية
        /// </summary>
        private string GetPaymentTypeInArabic(PaymentType? paymentType)
        {
            if (paymentType == null)
                return "غير مدفوع";
                
            return paymentType switch
            {
                PaymentType.Cash => "نقدي",
                PaymentType.Electronic => "إلكتروني",
                _ => "غير محدد"
            };
        }

        #endregion
    }
}
