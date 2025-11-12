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
    /// <summary>
    /// 💵 ViewModel للتحصيل اليومي
    /// </summary>
    public class DailyCollectionViewModel : BaseViewModel
    {
        private readonly CollectionService _collectionService;
        private readonly SavingPlanRepository _planRepository;
        private readonly ReceiptService _receiptService;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;
        private readonly OtherTransactionRepository _otherTransactionRepository;

        private ObservableCollection<SavingPlan> _duePlans;
        private SavingPlan? _selectedPlan;
        private decimal _amountPaid;
        private PaymentSource _paymentSource = PaymentSource.Cash;
        private List<int> _weeks;
        private int _selectedWeek = 1;
        private List<(int, string)> _days;
        private (int, string) _selectedDay = (1, "السبت");
        private string _currentDayDisplay;
        
        // خصائص الخرجيات والمفقودات
        private ObservableCollection<OtherTransaction> _otherTransactions;
        private decimal _otherAmount;
        private DateTime _otherDate = DateTime.Now;
        private string _otherNotes = string.Empty;
        
        // خصائص المتأخرات
        private ObservableCollection<ArrearSummary> _currentWeekArrears;
        private ArrearSummary? _selectedArrear;
        private decimal _arrearPaymentAmount;
        private string _arrearPaymentNotes = string.Empty;
        
        // خصائص السابقات
        private ObservableCollection<PreviousArrears> _previousArrears;
        private PreviousArrears? _selectedPreviousArrear;
        private decimal _previousPaymentAmount;
        private string _previousPaymentNotes = string.Empty;
        
        // خصائص الإدخال المباشر للسابقات
        private SavingPlan? _selectedPlanForDirectEntry;
        private int _directWeekFrom = 1;
        private int _directWeekTo = 10;
        private decimal _directTotalOriginal;
        private decimal _directAlreadyPaid;
        private decimal _directRemaining;
        private string _directNotes = string.Empty;
        
        // خصائص البحث
        private string _dailySearchText = string.Empty;
        private string _arrearSearchText = string.Empty;
        private string _previousArrearSearchText = string.Empty;
        private ObservableCollection<SavingPlan> _allDuePlans;
        private ObservableCollection<ArrearSummary> _allCurrentWeekArrears;
        private ObservableCollection<PreviousArrears> _allPreviousArrears;
        private bool _isInitializing = true; // ✅ flag لمنع الفحص أثناء التهيئة

        public DailyCollectionViewModel()
        {
            _collectionService = new CollectionService();
            _planRepository = new SavingPlanRepository();
            _receiptService = new ReceiptService();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;
            _otherTransactionRepository = new OtherTransactionRepository();

            DuePlans = new ObservableCollection<SavingPlan>();
            _otherTransactions = new ObservableCollection<OtherTransaction>();
            _currentWeekArrears = new ObservableCollection<ArrearSummary>();
            _previousArrears = new ObservableCollection<PreviousArrears>();

            // تحميل تاريخ البداية من الإعدادات
            LoadStartDateFromSettings();

            // تحميل قوائم الأسابيع والأيام
            Weeks = WeekHelper.GetAllWeeks();
            Days = WeekHelper.GetDaysInWeek();

            // حساب الأسبوع واليوم الحاليين تلقائياً
            SetCurrentWeekAndDay();

            RecordPaymentCommand = new RelayCommand(ExecuteRecordPayment, CanExecuteRecord);
            PrintReceiptCommand = new RelayCommand(ExecutePrintReceipt, CanExecutePrint);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            GoToWeekSummaryCommand = new RelayCommand(ExecuteGoToWeekSummary, _ => true);
            CreateMissingArrearsCommand = new RelayCommand(ExecuteCreateMissingArrears, _ => true);
            QuickPayCommand = new RelayCommand(ExecuteQuickPay, CanExecuteQuickPay);
            GoToDailySummaryCommand = new RelayCommand(ExecuteGoToDailySummary, _ => true);
            AddOtherTransactionCommand = new RelayCommand(ExecuteAddOtherTransaction, _ => true);
            PayArrearCommand = new RelayCommand(ExecutePayArrear, CanExecutePayArrear);
            PayPreviousArrearCommand = new RelayCommand(ExecutePayPreviousArrear, CanExecutePayPreviousArrear);
            AddDirectPreviousArrearsCommand = new RelayCommand(ExecuteAddDirectPreviousArrears, CanExecuteAddDirectPreviousArrears);

            UpdateCurrentDayDisplay();
            LoadDuePlans(); // التحميل الأولي بدون فحص
            LoadOtherTransactions();
            LoadCurrentWeekArrears();
            LoadPreviousArrears();
            
            _isInitializing = false; // ✅ انتهت التهيئة - يمكن الفحص الآن
        }

        #region Properties

        public ObservableCollection<SavingPlan> DuePlans
        {
            get => _duePlans;
            set => SetProperty(ref _duePlans, value);
        }

        public SavingPlan? SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                SetProperty(ref _selectedPlan, value);
                if (value != null)
                {
                    AmountPaid = value.DailyAmount;
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
                UpdateCurrentDayDisplay();
                CheckAndLoadDuePlans(); // ✅ فحص قبل التحميل
            }
        }

        public List<(int, string)> Days
        {
            get => _days;
            set => SetProperty(ref _days, value);
        }

        public (int, string) SelectedDay
        {
            get => _selectedDay;
            set
            {
                SetProperty(ref _selectedDay, value);
                UpdateCurrentDayDisplay();
                CheckAndLoadDuePlans(); // ✅ فحص قبل التحميل
            }
        }

        public string CurrentDayDisplay
        {
            get => _currentDayDisplay;
            set => SetProperty(ref _currentDayDisplay, value);
        }
        
        // خصائص الخرجيات والمفقودات
        public ObservableCollection<OtherTransaction> OtherTransactions
        {
            get => _otherTransactions;
            set => SetProperty(ref _otherTransactions, value);
        }
        
        public decimal OtherAmount
        {
            get => _otherAmount;
            set => SetProperty(ref _otherAmount, value);
        }
        
        public DateTime OtherDate
        {
            get => _otherDate;
            set => SetProperty(ref _otherDate, value);
        }
        
        public string OtherNotes
        {
            get => _otherNotes;
            set => SetProperty(ref _otherNotes, value);
        }
        
        public decimal TotalOtherTransactions => OtherTransactions?.Sum(t => t.Amount) ?? 0;
        
        // خصائص المتأخرات
        public ObservableCollection<ArrearSummary> CurrentWeekArrears
        {
            get => _currentWeekArrears;
            set => SetProperty(ref _currentWeekArrears, value);
        }
        
        public ArrearSummary? SelectedArrear
        {
            get => _selectedArrear;
            set => SetProperty(ref _selectedArrear, value);
        }
        
        public decimal ArrearPaymentAmount
        {
            get => _arrearPaymentAmount;
            set => SetProperty(ref _arrearPaymentAmount, value);
        }
        
        public string ArrearPaymentNotes
        {
            get => _arrearPaymentNotes;
            set => SetProperty(ref _arrearPaymentNotes, value);
        }
        
        public decimal TotalCurrentArrears => CurrentWeekArrears?.Sum(a => a.TotalArrears) ?? 0;
        
        // خصائص السابقات
        public ObservableCollection<PreviousArrears> PreviousArrears
        {
            get => _previousArrears;
            set => SetProperty(ref _previousArrears, value);
        }
        
        public PreviousArrears? SelectedPreviousArrear
        {
            get => _selectedPreviousArrear;
            set => SetProperty(ref _selectedPreviousArrear, value);
        }
        
        public decimal PreviousPaymentAmount
        {
            get => _previousPaymentAmount;
            set => SetProperty(ref _previousPaymentAmount, value);
        }
        
        public string PreviousPaymentNotes
        {
            get => _previousPaymentNotes;
            set => SetProperty(ref _previousPaymentNotes, value);
        }
        
        public decimal TotalPreviousArrears => PreviousArrears?.Sum(a => a.TotalArrears) ?? 0;
        public decimal PaidPreviousArrears => PreviousArrears?.Sum(a => a.PaidAmount) ?? 0;
        public decimal RemainingPreviousArrears => PreviousArrears?.Sum(a => a.RemainingAmount) ?? 0;
        
        // خصائص الإدخال المباشر للسابقات
        public SavingPlan? SelectedPlanForDirectEntry
        {
            get => _selectedPlanForDirectEntry;
            set
            {
                SetProperty(ref _selectedPlanForDirectEntry, value);
                ((RelayCommand)AddDirectPreviousArrearsCommand).RaiseCanExecuteChanged();
            }
        }
        
        public int DirectWeekFrom
        {
            get => _directWeekFrom;
            set => SetProperty(ref _directWeekFrom, value);
        }
        
        public int DirectWeekTo
        {
            get => _directWeekTo;
            set => SetProperty(ref _directWeekTo, value);
        }
        
        public decimal DirectTotalOriginal
        {
            get => _directTotalOriginal;
            set
            {
                SetProperty(ref _directTotalOriginal, value);
                UpdateDirectAlreadyPaid();
            }
        }
        
        public decimal DirectRemaining
        {
            get => _directRemaining;
            set
            {
                SetProperty(ref _directRemaining, value);
                UpdateDirectAlreadyPaid();
            }
        }
        
        public decimal DirectAlreadyPaid
        {
            get => _directAlreadyPaid;
            set => SetProperty(ref _directAlreadyPaid, value);
        }
        
        public string DirectNotes
        {
            get => _directNotes;
            set => SetProperty(ref _directNotes, value);
        }
        
        // خصائص البحث
        public string DailySearchText
        {
            get => _dailySearchText;
            set
            {
                if (SetProperty(ref _dailySearchText, value))
                {
                    FilterDailyPlans();
                }
            }
        }
        
        public string ArrearSearchText
        {
            get => _arrearSearchText;
            set
            {
                if (SetProperty(ref _arrearSearchText, value))
                {
                    FilterArrears();
                }
            }
        }
        
        public string PreviousArrearSearchText
        {
            get => _previousArrearSearchText;
            set
            {
                if (SetProperty(ref _previousArrearSearchText, value))
                {
                    FilterPreviousArrears();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand RecordPaymentCommand { get; }
        public ICommand PrintReceiptCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand GoToWeekSummaryCommand { get; }
        public ICommand QuickPayCommand { get; }
        public ICommand GoToDailySummaryCommand { get; }
        public ICommand CreateMissingArrearsCommand { get; }
        public ICommand AddOtherTransactionCommand { get; }
        public ICommand PayArrearCommand { get; }
        public ICommand PayPreviousArrearCommand { get; }
        public ICommand AddDirectPreviousArrearsCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanExecuteRecord(object parameter)
        {
            return SelectedPlan != null
                && AmountPaid > 0
                && _authService.HasPermission("RecordCollection");
        }

        private void ExecuteRecordPayment(object parameter)
        {
            try
            {
                if (SelectedPlan == null)
                    return;

                var userId = _authService.CurrentUser?.UserID ?? 0;

                // إنشاء كائن DailyCollection مع معلومات الأسبوع واليوم
                var collection = new DailyCollection
                {
                    PlanID = SelectedPlan.PlanID,
                    CollectionDate = DateTime.Now,
                    WeekNumber = SelectedWeek,
                    DayNumber = SelectedDay.Item1,
                    AmountPaid = AmountPaid,
                    PaymentType = PaymentType.Cash,
                    PaymentSource = PaymentSource,
                    CollectedBy = userId,
                };

                var result = _collectionService.RecordCollectionWithWeek(collection);

                if (result.Success)
                {
                    string dayName = WeekHelper.GetArabicDayName(SelectedDay.Item1);
                    System.Windows.MessageBox.Show(
                        $"✅ تم تسجيل التحصيل بنجاح!\n\n"
                            + $"📋 رقم الإيصال: {result.ReceiptNumber}\n"
                            + $"💰 المبلغ: {AmountPaid:N2} ريال\n"
                            + $"📅 {dayName} - الأسبوع {SelectedWeek}",
                        "نجاح ✅",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );

                    // إزالة العضو من القائمة بعد السداد
                    if (SelectedPlan != null)
                    {
                        DuePlans.Remove(SelectedPlan);
                        SelectedPlan = null;
                    }
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

        private bool CanExecutePrint(object parameter)
        {
            return SelectedPlan != null;
        }

        private void ExecutePrintReceipt(object parameter)
        {
            System.Windows.MessageBox.Show(
                "🖨️ وظيفة الطباعة قيد التطوير",
                "معلومات",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information
            );
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadDuePlans();
            LoadPreviousArrears();
            LoadCurrentWeekArrears();
            LoadOtherTransactions();
        }

        private void ExecuteCreateMissingArrears(object parameter)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "⚠️ هل تريد إنشاء متأخرات تلقائية لجميع الأعضاء الذين لم يدفعوا اليوم؟\n\n" +
                    "سيتم إنشاء متأخرة لكل عضو لم يسدد المبلغ اليومي المطلوب.\n\n" +
                    "هل تريد المتابعة؟",
                    "تأكيد إنشاء المتأخرات",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var arrearService = new ArrearService();
                    var (success, message, arrearsCreated) = arrearService.CreateMissingDailyArrears(DateTime.Now);

                    if (success)
                    {
                        System.Windows.MessageBox.Show(
                            $"✅ {message}\n\n" +
                            $"تم إنشاء {arrearsCreated} متأخرة جديدة.",
                            "نجاح",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                        
                        // إعادة تحميل بيانات المتأخرات
                        LoadCurrentWeekArrears();
                        LoadPreviousArrears();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            $"❌ {message}",
                            "خطأ",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ حدث خطأ: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteAddOtherTransaction(object parameter)
        {
            try
            {
                // التحقق من البيانات
                if (OtherAmount <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يجب إدخال مبلغ صحيح",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                // حساب الأسبوع واليوم
                int weekNumber = WeekHelper.GetWeekNumber(OtherDate);
                int dayNumber = WeekHelper.GetDayNumber(OtherDate);
                
                // إنشاء العملية
                var transaction = new OtherTransaction
                {
                    TransactionType = "📦 خرجية", // سيتم ربطها بالـ ComboBox لاحقاً
                    Amount = OtherAmount,
                    WeekNumber = weekNumber,
                    DayNumber = dayNumber,
                    TransactionDate = OtherDate,
                    Notes = OtherNotes,
                    CreatedBy = _authService.CurrentUser?.UserID ?? 1
                };
                
                // حفظ في قاعدة البيانات
                _otherTransactionRepository.Add(transaction);
                
                // إضافة للقائمة
                OtherTransactions.Add(transaction);
                OnPropertyChanged(nameof(TotalOtherTransactions));
                
                // تنظيف الحقول
                OtherAmount = 0;
                OtherNotes = string.Empty;
                OtherDate = DateTime.Now;
                
                System.Windows.MessageBox.Show(
                    "✅ تم تسجيل العملية بنجاح",
                    "نجاح",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ حدث خطأ: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteGoToWeekSummary(object parameter)
        {
            try
            {
                // إنشاء واجهة ملخص الأسبوع
                var weekSummaryView = new Views.Collections.WeekSummaryView();

                // البحث عن Frame في النافذة الرئيسية
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // البحث عن Frame في شجرة العناصر المرئية
                    var frame = FindVisualChild<System.Windows.Controls.Frame>(mainWindow);
                    if (frame != null)
                    {
                        // التنقل إلى صفحة ملخص الأسبوع
                        frame.Navigate(weekSummaryView);
                    }
                    else
                    {
                        // إذا لم يوجد Frame، فتح في نافذة منفصلة
                        var window = new System.Windows.Window
                        {
                            Content = weekSummaryView,
                            Title = "ملخص الأسبوع",
                            Width = 1200,
                            Height = 800,
                            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                            FlowDirection = System.Windows.FlowDirection.RightToLeft,
                            FontFamily = new System.Windows.Media.FontFamily("Tajawal"),
                        };
                        window.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في فتح ملخص الأسبوع: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void ExecuteGoToDailySummary(object parameter)
        {
            try
            {
                var summaryView = new Views.Collections.DailySummaryView(SelectedWeek, SelectedDay.Item1);
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var frame = FindVisualChild<System.Windows.Controls.Frame>(mainWindow);
                    if (frame != null)
                    {
                        frame.Navigate(summaryView);
                    }
                    else
                    {
                        var window = new System.Windows.Window
                        {
                            Content = summaryView,
                            Title = "ملخص اليوم",
                            Width = 1200,
                            Height = 800,
                            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                            FlowDirection = System.Windows.FlowDirection.RightToLeft,
                            FontFamily = new System.Windows.Media.FontFamily("Tajawal"),
                        };
                        window.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في فتح ملخص اليوم: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private static T FindVisualChild<T>(System.Windows.DependencyObject parent)
            where T : System.Windows.DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// فحص التاريخ المحدد قبل تحميل البيانات (فقط من ComboBox)
        /// </summary>
        private void CheckAndLoadDuePlans()
        {
            // ✅ تخطي الفحص أثناء التهيئة
            if (_isInitializing)
            {
                LoadDuePlans();
                return;
            }
            
            var (currentWeek, currentDay) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
            var selectedDate = WeekHelper.GetDateFromWeekAndDay(SelectedWeek, SelectedDay.Item1);
            
            // إذا كان التاريخ المختار قبل اليوم الحالي، تحقق من معالجته
            if (selectedDate < DateTime.Now.Date)
            {
                var arrearService = new ArrearService();
                bool isHistoricalDataProcessed = arrearService.IsHistoricalDataProcessed();
                
                if (isHistoricalDataProcessed)
                {
                    // البيانات تمت معالجتها - لا يمكن التحصيل
                    _allDuePlans = new ObservableCollection<SavingPlan>();
                    DuePlans.Clear();
                    
                    System.Windows.MessageBox.Show(
                        $"⚠️ لا يمكن التحصيل لهذا اليوم\n\n" +
                        $"التاريخ المختار ({selectedDate:yyyy-MM-dd}) قد تمت معالجته.\n" +
                        $"البيانات القديمة تم ترحيلها كمتأخرات وسابقات.\n\n" +
                        $"يمكنك فقط التحصيل لليوم الحالي: {DateTime.Now:yyyy-MM-dd}",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    
                    // الرجوع لليوم الحالي
                    _selectedWeek = currentWeek;
                    OnPropertyChanged(nameof(SelectedWeek));
                    
                    var currentDayTuple = Days.FirstOrDefault(d => d.Item1 == currentDay);
                    _selectedDay = currentDayTuple.Item1 != 0 ? currentDayTuple : Days[0];
                    OnPropertyChanged(nameof(SelectedDay));
                    
                    UpdateCurrentDayDisplay();
                    LoadDuePlans(); // تحميل بيانات اليوم الحالي
                    return;
                }
            }
            
            // التاريخ صحيح - حمل البيانات
            LoadDuePlans();
        }

        private void LoadDuePlans(bool isInitialLoad = false)
        {
            try
            {
                // تحميل الحصص المستحقة للأسبوع واليوم المحدد
                // فلترة: فقط الأعضاء ذوي التحصيل اليومي
                var plans = _planRepository.GetDueForWeekDay(SelectedWeek, SelectedDay.Item1)
                    .Where(p => p.CollectionFrequency == CollectionFrequency.Daily).ToList();
                
                _allDuePlans = new ObservableCollection<SavingPlan>(plans);
                FilterDailyPlans();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل الأسهم: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }
        
        private void FilterDailyPlans()
        {
            if (_allDuePlans == null) return;
            
            var filtered = _allDuePlans.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(DailySearchText))
            {
                var searchLower = DailySearchText.Trim().ToLower();
                filtered = filtered.Where(p => 
                    p.MemberName?.ToLower().Contains(searchLower) == true ||
                    p.PlanNumber.ToString().Contains(searchLower));
            }
            
            DuePlans.Clear();
            foreach (var plan in filtered)
            {
                DuePlans.Add(plan);
            }
        }

        /// <summary>
        /// تحميل الخرجيات والمفقودات لليوم الحالي
        /// </summary>
        private void LoadOtherTransactions()
        {
            try
            {
                int weekNumber = WeekHelper.GetWeekNumber(DateTime.Now);
                int dayNumber = WeekHelper.GetDayNumber(DateTime.Now);
                
                var transactions = _otherTransactionRepository.GetByWeekAndDay(weekNumber, dayNumber);
                
                OtherTransactions.Clear();
                foreach (var transaction in transactions)
                {
                    OtherTransactions.Add(transaction);
                }
                
                OnPropertyChanged(nameof(TotalOtherTransactions));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل الخرجيات: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// تحميل متأخرات الأسبوع الحالي (مجمّعة حسب العضو)
        /// </summary>
        private void LoadCurrentWeekArrears()
        {
            try
            {
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                var arrearRepository = new ArrearRepository();
                var planRepository = new SavingPlanRepository();
                
                // جلب جميع المتأخرات (المسددة وغير المسددة) لحساب الإجماليات بشكل صحيح
                var allArrears = arrearRepository.GetArrearsByWeek(currentWeek).ToList();
                
                // تجميع المتأخرات حسب PlanID - نعرض فقط من لديهم متأخرات غير مسددة
                var groupedArrears = allArrears
                    .GroupBy(a => a.PlanID)
                    .Where(g => g.Any(a => !a.IsPaid)) // فقط الأعضاء الذين لديهم متأخرات غير مسددة
                    .Select(g =>
                    {
                        var plan = planRepository.GetById(g.Key);
                        var unpaidArrears = g.Where(a => !a.IsPaid).ToList();
                        
                        return new ArrearSummary
                        {
                            PlanID = g.Key,
                            MemberName = plan?.MemberName ?? "غير معروف",
                            DaysCount = unpaidArrears.Count, // عدد الأيام غير المسددة
                            TotalArrears = g.Sum(a => a.AmountDue), // إجمالي المتأخرات الأصلي
                            PaidAmount = g.Sum(a => a.PaidAmount), // المبلغ المسدد
                            RemainingAmount = unpaidArrears.Sum(a => a.RemainingAmount) // المتبقي غير المسدد
                        };
                    })
                    .ToList();
                
                _allCurrentWeekArrears = new ObservableCollection<ArrearSummary>(groupedArrears);
                FilterArrears();
                
                OnPropertyChanged(nameof(TotalCurrentArrears));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل المتأخرات: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }
        
        private void FilterArrears()
        {
            if (_allCurrentWeekArrears == null) return;
            
            var filtered = _allCurrentWeekArrears.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(ArrearSearchText))
            {
                var searchLower = ArrearSearchText.Trim().ToLower();
                filtered = filtered.Where(a => 
                    a.MemberName?.ToLower().Contains(searchLower) == true);
            }
            
            CurrentWeekArrears.Clear();
            foreach (var arrear in filtered.OrderByDescending(a => a.TotalArrears))
            {
                CurrentWeekArrears.Add(arrear);
            }
        }
        
        private void FilterPreviousArrears()
        {
            if (_allPreviousArrears == null) return;
            
            var filtered = _allPreviousArrears.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(PreviousArrearSearchText))
            {
                var searchLower = PreviousArrearSearchText.Trim().ToLower();
                filtered = filtered.Where(a => 
                    a.MemberName?.ToLower().Contains(searchLower) == true ||
                    a.WeeksRange?.Contains(searchLower) == true);
            }
            
            PreviousArrears.Clear();
            foreach (var arrear in filtered)
            {
                PreviousArrears.Add(arrear);
            }
        }
        
        /// <summary>
        /// تحميل السابقات المتراكمة (سجل واحد لكل عضو)
        /// </summary>
        private void LoadPreviousArrears()
        {
            try
            {
                var accumulatedRepository = new AccumulatedArrearsRepository();
                
                // الحصول على جميع السابقات المتراكمة (غير المسددة فقط)
                var accumulatedArrears = accumulatedRepository.GetAll().ToList();
                
                var displayList = new List<PreviousArrears>();
                
                foreach (var accumulated in accumulatedArrears)
                {
                    // تحويل من AccumulatedArrears إلى PreviousArrears للعرض
                    displayList.Add(new PreviousArrears
                    {
                        PlanID = accumulated.PlanID,
                        MemberName = accumulated.MemberName,
                        PlanNumber = accumulated.PlanNumber,
                        WeekNumber = accumulated.LastWeekNumber,
                        TotalArrears = accumulated.TotalArrears,
                        RemainingAmount = accumulated.RemainingAmount,
                        PaidAmount = accumulated.PaidAmount,
                        IsPaid = false,
                        WeeksRange = $"1-{accumulated.LastWeekNumber}"
                    });
                }
                
                _allPreviousArrears = new ObservableCollection<PreviousArrears>(displayList.OrderBy(p => p.MemberName));
                FilterPreviousArrears();
                
                OnPropertyChanged(nameof(TotalPreviousArrears));
                OnPropertyChanged(nameof(PaidPreviousArrears));
                OnPropertyChanged(nameof(RemainingPreviousArrears));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل السابقات: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }
        
        /// <summary>
        /// سداد متأخرة
        /// </summary>
        private void ExecutePayArrear(object parameter)
        {
            try
            {
                if (SelectedArrear == null)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يجب اختيار متأخرة للسداد",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                if (ArrearPaymentAmount <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يجب إدخال مبلغ صحيح",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                if (ArrearPaymentAmount > SelectedArrear.RemainingAmount)
                {
                    System.Windows.MessageBox.Show(
                        $"⚠️ المبلغ المدفوع ({ArrearPaymentAmount:N2}) أكبر من المتبقي ({SelectedArrear.RemainingAmount:N2})",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                // سداد المتأخرات من خلال PlanID - يدعم سداد عدة أيام
                var arrearRepository = new ArrearRepository();
                var arrearService = new ArrearService();
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                
                // جلب جميع المتأخرات غير المسددة للعضو مرتبة حسب اليوم
                var unpaidArrears = arrearRepository.GetArrearsByPlanAndWeek(SelectedArrear.PlanID, currentWeek)
                    .Where(a => !a.IsPaid)
                    .OrderBy(a => a.DayNumber)
                    .ToList();
                
                if (!unpaidArrears.Any())
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ لم يتم العثور على متأخرات لهذا العضو",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                // سداد المتأخرات بالترتيب
                decimal remainingPayment = ArrearPaymentAmount;
                int paidCount = 0;
                var paidDays = new System.Collections.Generic.List<string>();
                
                foreach (var arrear in unpaidArrears)
                {
                    if (remainingPayment <= 0) break;
                    
                    decimal amountToPay = Math.Min(remainingPayment, arrear.RemainingAmount);
                    
                    var (success, message) = arrearService.PayArrear(
                        arrear.ArrearID,
                        amountToPay,
                        PaymentSource.Cash,
                        ArrearPaymentNotes,
                        _authService.CurrentUser?.UserID ?? 1
                    );
                    
                    if (success)
                    {
                        remainingPayment -= amountToPay;
                        paidCount++;
                        paidDays.Add(arrear.DayName ?? $"اليوم {arrear.DayNumber}");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            $"❌ خطأ في سداد {arrear.DayName}: {message}",
                            "خطأ",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        break;
                    }
                }
                
                if (paidCount > 0)
                {
                    string daysText = string.Join("، ", paidDays);
                    System.Windows.MessageBox.Show(
                        $"✅ تم سداد {paidCount} يوم بنجاح\n" +
                        $"الأيام: {daysText}\n" +
                        $"المبلغ المسدد: {ArrearPaymentAmount - remainingPayment:N2} ريال",
                        "نجاح",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    
                    // تحديث البيانات
                    LoadCurrentWeekArrears();
                    ArrearPaymentAmount = 0;
                    ArrearPaymentNotes = string.Empty;
                    SelectedArrear = null;
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "❌ لم يتم سداد أي متأخرات",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ حدث خطأ: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecutePayArrear(object parameter)
        {
            return SelectedArrear != null && ArrearPaymentAmount > 0;
        }
        
        /// <summary>
        /// سداد سابقة
        /// </summary>
        private void ExecutePayPreviousArrear(object parameter)
        {
            try
            {
                if (SelectedPreviousArrear == null)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يجب اختيار سابقة للسداد",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                if (PreviousPaymentAmount <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يجب إدخال مبلغ صحيح",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                if (PreviousPaymentAmount > SelectedPreviousArrear.RemainingAmount)
                {
                    System.Windows.MessageBox.Show(
                        $"⚠️ المبلغ المدفوع ({PreviousPaymentAmount:N2}) أكبر من المتبقي ({SelectedPreviousArrear.RemainingAmount:N2})",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                // سداد السابقات من الإجمالي المتراكم وتسجيل في تحصيل اليوم
                var arrearService = new ArrearService();
                
                var (success, message) = arrearService.PayPreviousArrear(
                    SelectedPreviousArrear.PlanID,
                    PreviousPaymentAmount,
                    PaymentSource.Cash,
                    PreviousPaymentNotes,
                    _authService.CurrentUser?.UserID ?? 1
                );
                
                if (!success)
                {
                    System.Windows.MessageBox.Show(
                        $"❌ {message}",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                System.Windows.MessageBox.Show(
                    $"✅ {message}\n" +
                    $"تم تسجيل الدفع في تحصيل اليوم",
                    "نجاح",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                
                // تحديث البيانات
                LoadPreviousArrears();
                PreviousPaymentAmount = 0;
                PreviousPaymentNotes = string.Empty;
                SelectedPreviousArrear = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ حدث خطأ: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecutePayPreviousArrear(object parameter)
        {
            return SelectedPreviousArrear != null && PreviousPaymentAmount > 0;
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
                    System.Diagnostics.Debug.WriteLine($"✅ تم تحميل تاريخ بداية الجمعية: {settings.StartDate:yyyy-MM-dd}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ لم يتم العثور على إعدادات، سيتم استخدام التاريخ الافتراضي");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحميل تاريخ البداية: {ex.Message}");
            }
        }

        private void SetCurrentWeekAndDay()
        {
            var today = DateTime.Today;

            // حساب رقم الأسبوع واليوم بناءً على تاريخ اليوم
            var (weekNumber, dayNumber) = WeekHelper.GetWeekAndDayFromDate(today);

            // تعيين الأسبوع واليوم الحاليين
            SelectedWeek = weekNumber;
            SelectedDay = (dayNumber, WeekHelper.GetArabicDayName(dayNumber));
            
            System.Diagnostics.Debug.WriteLine($"📅 الأسبوع الحالي: {weekNumber}, اليوم: {dayNumber} ({WeekHelper.GetArabicDayName(dayNumber)})");
        }

        private void UpdateCurrentDayDisplay()
        {
            CurrentDayDisplay = $"{SelectedDay.Item2} - الأسبوع {SelectedWeek}";
        }

        private bool CanExecuteQuickPay(object parameter)
        {
            return parameter is SavingPlan && _authService.HasPermission("RecordCollection");
        }

        private void ExecuteQuickPay(object parameter)
        {
            try
            {
                if (parameter is not SavingPlan plan)
                    return;

                var userId = _authService.CurrentUser?.UserID ?? 0;

                // إنشاء كائن DailyCollection مع المبلغ اليومي الكامل
                var collection = new DailyCollection
                {
                    PlanID = plan.PlanID,
                    CollectionDate = DateTime.Now,
                    WeekNumber = SelectedWeek,
                    DayNumber = SelectedDay.Item1,
                    AmountPaid = plan.DailyAmount,
                    PaymentType = PaymentType.Cash,
                    PaymentSource = PaymentSource,
                    CollectedBy = userId,
                };

                var result = _collectionService.RecordCollectionWithWeek(collection);

                if (result.Success)
                {
                    string dayName = WeekHelper.GetArabicDayName(SelectedDay.Item1);
                    System.Windows.MessageBox.Show(
                        $"✅ تم تسجيل التحصيل بنجاح!\n\n"
                            + $"👤 العضو: {plan.MemberName}\n"
                            + $"📋 رقم الإيصال: {result.ReceiptNumber}\n"
                            + $"💰 المبلغ: {plan.DailyAmount:N2} ريال\n"
                            + $"📅 {dayName} - الأسبوع {SelectedWeek}",
                        "نجاح ✅",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );

                    // إزالة العضو من القائمة بعد السداد
                    DuePlans.Remove(plan);
                    if (SelectedPlan == plan)
                    {
                        SelectedPlan = null;
                    }
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
        
        /// <summary>
        /// إدخال سابقات مباشرة
        /// </summary>
        private void ExecuteAddDirectPreviousArrears(object parameter)
        {
            try
            {
                if (SelectedPlanForDirectEntry == null)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يجب اختيار عضو",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                if (DirectRemaining <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يجب إدخال مبلغ متبقي صحيح",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                var arrearService = new ArrearService();
                var (success, message) = arrearService.AddDirectPreviousArrears(
                    SelectedPlanForDirectEntry.PlanID,
                    DirectWeekFrom,
                    DirectWeekTo,
                    DirectTotalOriginal,
                    DirectRemaining,
                    DirectNotes,
                    _authService.CurrentUser?.UserID ?? 1
                );
                
                if (success)
                {
                    System.Windows.MessageBox.Show(
                        message,
                        "نجاح",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    
                    // تنظيف الحقول
                    SelectedPlanForDirectEntry = null;
                    DirectWeekFrom = 1;
                    DirectWeekTo = 10;
                    DirectTotalOriginal = 0;
                    DirectRemaining = 0;
                    DirectAlreadyPaid = 0;
                    DirectNotes = string.Empty;
                    
                    // تحديث البيانات
                    LoadPreviousArrears();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"❌ {message}",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecuteAddDirectPreviousArrears(object parameter)
        {
            return SelectedPlanForDirectEntry != null && DirectRemaining > 0;
        }
        
        private void UpdateDirectAlreadyPaid()
        {
            if (DirectTotalOriginal > 0 && DirectRemaining >= 0)
            {
                DirectAlreadyPaid = DirectTotalOriginal - DirectRemaining;
            }
        }

        #endregion
    }
    
    /// <summary>
    /// ملخص متأخرات عضو (مجمّع من عدة أيام)
    /// </summary>
    public class ArrearSummary
    {
        public int PlanID { get; set; }
        public string MemberName { get; set; }
        public int DaysCount { get; set; }
        public decimal TotalArrears { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
