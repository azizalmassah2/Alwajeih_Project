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
    /// ViewModel موحد لإدارة المتأخرات والسابقات
    /// </summary>
    public class ArrearsManagementViewModel : BaseViewModel
    {
        private readonly ArrearRepository _arrearRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly ArrearService _arrearService;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;

        private int _selectedTabIndex;
        private int _currentWeekNumber;
        private int _selectedCurrentWeekNumber;
        private int _selectedWeekNumber;
        private System.Collections.Generic.List<int> _availableWeeks;
        private System.Collections.Generic.List<int> _availableWeeksForCurrent;
        private ObservableCollection<MemberArrearSummary> _currentWeekArrears;
        private ObservableCollection<MemberPreviousArrearSummary> _previousArrears;
        private MemberArrearSummary _selectedCurrentWeekArrear;
        private MemberPreviousArrearSummary _selectedPreviousArrear;
        private string _currentWeekSearchText;
        private string _previousArrearsSearchText;
        private bool _isLoading;
        private bool _isProcessing; // للمعالجة التاريخية فقط
        private int _progressPercentage;
        private string _progressMessage;

        public ArrearsManagementViewModel()
        {
            _arrearRepository = new ArrearRepository();
            _planRepository = new SavingPlanRepository();
            _arrearService = new ArrearService();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;

            CurrentWeekArrears = new ObservableCollection<MemberArrearSummary>();
            PreviousArrears = new ObservableCollection<MemberPreviousArrearSummary>();

            // تحميل تاريخ البداية
            LoadStartDateFromSettings();

            // الأسبوع الحالي
            CurrentWeekNumber = WeekHelper.GetCurrentWeekNumber();
            SelectedCurrentWeekNumber = CurrentWeekNumber;

            // تحميل الأسابيع المتاحة
            LoadAvailableWeeks();
            LoadAvailableWeeksForCurrent();

            RefreshCurrentWeekCommand = new RelayCommand(ExecuteRefreshCurrentWeek, _ => true);
            RefreshPreviousArrearsCommand = new RelayCommand(ExecuteRefreshPreviousArrears, _ => true);
            ProcessHistoricalDataCommand = new RelayCommand(ExecuteProcessHistoricalData, _ => true);

            // تحميل البيانات بشكل غير متزامن
            System.Threading.Tasks.Task.Run(() =>
            {
                LoadCurrentWeekData();
                // تعيين الأسبوع الحالي كافتراضي للسابقات
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedWeekNumber = CurrentWeekNumber;
                });
            });
        }

        #region Properties

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public int CurrentWeekNumber
        {
            get => _currentWeekNumber;
            set => SetProperty(ref _currentWeekNumber, value);
        }

        public int SelectedCurrentWeekNumber
        {
            get => _selectedCurrentWeekNumber;
            set
            {
                if (SetProperty(ref _selectedCurrentWeekNumber, value))
                {
                    LoadCurrentWeekData();
                }
            }
        }

        public int SelectedWeekNumber
        {
            get => _selectedWeekNumber;
            set
            {
                if (SetProperty(ref _selectedWeekNumber, value))
                {
                    LoadPreviousArrearsData();
                    // تحديث البطاقات الثلاث
                    OnPropertyChanged(nameof(TotalPreviousArrears));
                    OnPropertyChanged(nameof(PaidPreviousArrears));
                    OnPropertyChanged(nameof(RemainingPreviousArrears));
                }
            }
        }

        public System.Collections.Generic.List<int> AvailableWeeks
        {
            get => _availableWeeks;
            set => SetProperty(ref _availableWeeks, value);
        }

        public System.Collections.Generic.List<int> AvailableWeeksForCurrent
        {
            get => _availableWeeksForCurrent;
            set => SetProperty(ref _availableWeeksForCurrent, value);
        }

        public ObservableCollection<MemberArrearSummary> CurrentWeekArrears
        {
            get => _currentWeekArrears;
            set => SetProperty(ref _currentWeekArrears, value);
        }

        public ObservableCollection<MemberPreviousArrearSummary> PreviousArrears
        {
            get => _previousArrears;
            set => SetProperty(ref _previousArrears, value);
        }

        public MemberArrearSummary SelectedCurrentWeekArrear
        {
            get => _selectedCurrentWeekArrear;
            set => SetProperty(ref _selectedCurrentWeekArrear, value);
        }

        public MemberPreviousArrearSummary SelectedPreviousArrear
        {
            get => _selectedPreviousArrear;
            set => SetProperty(ref _selectedPreviousArrear, value);
        }

        public string CurrentWeekSearchText
        {
            get => _currentWeekSearchText;
            set
            {
                if (SetProperty(ref _currentWeekSearchText, value))
                {
                    LoadCurrentWeekData();
                }
            }
        }

        public string PreviousArrearsSearchText
        {
            get => _previousArrearsSearchText;
            set
            {
                if (SetProperty(ref _previousArrearsSearchText, value))
                {
                    LoadPreviousArrearsData();
                }
            }
        }
        
        /// <summary>
        /// إجمالي المتأخرات من الأسبوع المحدد
        /// </summary>
        public decimal TotalCurrentWeekArrears => CurrentWeekArrears?.Sum(a => a.TotalArrears) ?? 0;
        
        /// <summary>
        /// المتبقي غير المسدد من الأسبوع المحدد
        /// </summary>
        public decimal UnpaidCurrentArrears => CurrentWeekArrears?.Where(a => !a.IsPaid).Sum(a => a.RemainingAmount) ?? 0;
        
        /// <summary>
        /// المسدد من الأسبوع المحدد
        /// </summary>
        public decimal PaidCurrentArrears => CurrentWeekArrears?.Sum(a => a.PaidAmount) ?? 0;
        
        /// <summary>
        /// إجمالي السابقات المتراكمة (من جميع الأسابيع)
        /// </summary>
        public decimal TotalPreviousArrears
        {
            get
            {
                try
                {
                    var repo = new AccumulatedArrearsRepository();
                    return repo.GetAll().Sum(a => a.TotalArrears);
                }
                catch { return 0; }
            }
        }
        
        /// <summary>
        /// إجمالي المسدد من السابقات (من جميع الأسابيع)
        /// </summary>
        public decimal PaidPreviousArrears
        {
            get
            {
                try
                {
                    var repo = new AccumulatedArrearsRepository();
                    return repo.GetAll().Sum(a => a.PaidAmount);
                }
                catch { return 0; }
            }
        }
        
        /// <summary>
        /// إجمالي المتبقي من السابقات (من جميع الأسابيع)
        /// </summary>
        public decimal RemainingPreviousArrears
        {
            get
            {
                try
                {
                    var repo = new AccumulatedArrearsRepository();
                    return repo.GetAll()
                        .Where(a => !a.IsPaid)
                        .Sum(a => a.RemainingAmount);
                }
                catch { return 0; }
            }
        }
        
        /// <summary>
        /// الإجمالي الكلي للسابقات قبل السداد (نفس TotalPreviousArrears)
        /// </summary>
        public decimal OriginalTotalPreviousArrears => TotalPreviousArrears;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        /// <summary>
        /// يُستخدم فقط لعرض شريط التقدم عند معالجة البيانات القديمة
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }
        
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }
        
        public string ProgressMessage
        {
            get => _progressMessage;
            set => SetProperty(ref _progressMessage, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCurrentWeekCommand { get; }
        public ICommand RefreshPreviousArrearsCommand { get; }
        public ICommand ProcessHistoricalDataCommand { get; }

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
                
                // جميع الأسابيع حتى الأسبوع الحالي (لعرض السابقات المُراكمة)
                var weeks = new System.Collections.Generic.List<int>();
                for (int i = currentWeek; i >= 1; i--)
                {
                    weeks.Add(i);
                }

                AvailableWeeks = weeks;
                System.Diagnostics.Debug.WriteLine($"الأسابيع السابقة المتاحة: {weeks.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الأسابيع: {ex.Message}");
                AvailableWeeks = new System.Collections.Generic.List<int>();
            }
        }

        private void LoadAvailableWeeksForCurrent()
        {
            try
            {
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                
                // جميع الأسابيع حتى الأسبوع الحالي
                var weeks = new System.Collections.Generic.List<int>();
                for (int i = currentWeek; i >= 1; i--)
                {
                    weeks.Add(i);
                }

                AvailableWeeksForCurrent = weeks;
                System.Diagnostics.Debug.WriteLine($"الأسابيع المتاحة للمتأخرات: {weeks.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الأسابيع: {ex.Message}");
                AvailableWeeksForCurrent = new System.Collections.Generic.List<int>();
            }
        }

        private void LoadCurrentWeekData()
        {
            try
            {
                IsLoading = true;

                var activePlans = _planRepository.GetActive().ToList();
                System.Diagnostics.Debug.WriteLine($"عدد الأسهم النشطة: {activePlans.Count}");
                System.Diagnostics.Debug.WriteLine($"الأسبوع المحدد: {SelectedCurrentWeekNumber}");

                var memberArrearsList = new System.Collections.Generic.List<MemberArrearSummary>();

                foreach (var plan in activePlans)
                {
                    // تطبيق الفلتر
                    if (!string.IsNullOrWhiteSpace(CurrentWeekSearchText) &&
                        !plan.MemberName.Contains(CurrentWeekSearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    decimal currentWeekArrears = _arrearService.GetCurrentWeekArrearsTotal(plan.PlanID, SelectedCurrentWeekNumber);
                    System.Diagnostics.Debug.WriteLine($"العضو: {plan.MemberName}, المتأخرات: {currentWeekArrears}");

                    if (currentWeekArrears > 0)
                    {
                        var dailyArrears = _arrearRepository.GetArrearsByPlanAndWeek(plan.PlanID, SelectedCurrentWeekNumber)
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

                System.Diagnostics.Debug.WriteLine($"إجمالي الأعضاء بمتأخرات: {memberArrearsList.Count}");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentWeekArrears.Clear();
                    foreach (var item in memberArrearsList.OrderByDescending(m => m.TotalArrears))
                    {
                        CurrentWeekArrears.Add(item);
                    }
                    
                    // تحديث البطاقات
                    OnPropertyChanged(nameof(TotalCurrentWeekArrears));
                    OnPropertyChanged(nameof(UnpaidCurrentArrears));
                    OnPropertyChanged(nameof(PaidCurrentArrears));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل المتأخرات: {ex.Message}");
                System.Windows.MessageBox.Show($"خطأ في تحميل المتأخرات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadPreviousArrearsData()
        {
            try
            {
                IsLoading = true;

                if (SelectedWeekNumber == 0) return;

                // جلب جميع السابقات من AccumulatedArrears (بدون فلترة)
                // السبب: السابقات متراكمة من جميع الأسابيع، والمدفوعات مسجلة في AccumulatedArrears فقط
                var accumulatedRepository = new AccumulatedArrearsRepository();
                var accumulatedArrears = accumulatedRepository.GetAll().ToList();
                
                var memberPreviousArrearsList = new System.Collections.Generic.List<MemberPreviousArrearSummary>();

                foreach (var accumulated in accumulatedArrears)
                {
                    // تطبيق فلتر البحث
                    if (!string.IsNullOrWhiteSpace(PreviousArrearsSearchText) &&
                        !accumulated.MemberName.Contains(PreviousArrearsSearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // عرض السابقات المتراكمة حتى الأسبوع المحدد
                    if (accumulated.TotalArrears > 0 || accumulated.RemainingAmount > 0)
                    {
                        memberPreviousArrearsList.Add(new MemberPreviousArrearSummary
                        {
                            PlanID = accumulated.PlanID,
                            MemberName = accumulated.MemberName,
                            PlanNumber = accumulated.PlanNumber,
                            WeekNumber = 1, // من الأسبوع 1
                            LastWeekNumber = accumulated.LastWeekNumber, // آخر أسبوع
                            TotalArrears = accumulated.TotalArrears, // الإجمالي الكلي
                            RemainingAmount = accumulated.RemainingAmount, // المتبقي
                            PaidAmount = accumulated.PaidAmount, // المسدد
                            PreviousArrears = new System.Collections.Generic.List<PreviousArrears>()
                        });
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PreviousArrears.Clear();
                    foreach (var item in memberPreviousArrearsList.OrderByDescending(m => m.TotalArrears))
                    {
                        PreviousArrears.Add(item);
                    }
                    
                    // تحديث الخصائص المحسوبة
                    OnPropertyChanged(nameof(TotalPreviousArrears));
                    OnPropertyChanged(nameof(PaidPreviousArrears));
                    OnPropertyChanged(nameof(RemainingPreviousArrears));
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل السابقات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteRefreshCurrentWeek(object parameter)
        {
            CurrentWeekNumber = WeekHelper.GetCurrentWeekNumber();
            SelectedCurrentWeekNumber = CurrentWeekNumber;
            LoadAvailableWeeksForCurrent();
            LoadCurrentWeekData();
        }

        private void ExecuteRefreshPreviousArrears(object parameter)
        {
            LoadAvailableWeeks();
            LoadPreviousArrearsData();
        }

        private void ExecuteProcessHistoricalData(object parameter)
        {
            try
            {
                // التحقق مما إذا تمت المعالجة مسبقاً
                var arrearService = new ArrearService();
                bool isProcessed = arrearService.IsHistoricalDataProcessed();
                
                string message = "🔄 معالجة البيانات القديمة\n\n" +
                    "سيتم:\n" +
                    "1️⃣ فحص جميع الأسابيع السابقة\n" +
                    "2️⃣ إنشاء متأخرات للأيام التي لم يتم الدفع فيها\n" +
                    "3️⃣ تحويل متأخرات الأسابيع السابقة إلى سابقات\n\n";
                
                if (isProcessed)
                {
                    message += "⚠️ تنبيه: تم اكتشاف بيانات معالجة سابقة!\n" +
                               "• تشغيل المعالجة مرة أخرى سيتخطى البيانات الموجودة\n" +
                               "• سيتم معالجة البيانات الجديدة فقط\n\n";
                }
                
                message += "⏱️ هذه العملية قد تستغرق بعض الوقت\n\n" +
                           "هل تريد المتابعة؟";
                
                var result = System.Windows.MessageBox.Show(
                    message,
                    isProcessed ? "تأكيد إعادة المعالجة" : "تأكيد معالجة البيانات القديمة",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // إنشاء نافذة التقدم
                    var progressWindow = new Alwajeih.Views.Dialogs.ProgressWindow
                    {
                        Owner = System.Windows.Application.Current.MainWindow
                    };

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            // عرض النافذة
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressWindow.Show();
                            });
                            
                            // معالجة البيانات مع تحديثات التقدم
                            var (success, message, arrearsCreated, previousCreated) = 
                                arrearService.ProcessHistoricalData((percentage, msg) =>
                                {
                                    // تحديث نافذة التقدم
                                    progressWindow.UpdateProgress(percentage, msg);
                                });

                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                // إغلاق نافذة التقدم
                                progressWindow.Close();

                                if (success)
                                {
                                    System.Windows.MessageBox.Show(
                                        $"✅ {message}\n\n" +
                                        $"📊 الإحصائيات:\n" +
                                        $"• متأخرات جديدة: {arrearsCreated}\n" +
                                        $"• سابقات جديدة: {previousCreated}\n\n" +
                                        $"تم تحديث البيانات بنجاح!",
                                        "نجاح",
                                        System.Windows.MessageBoxButton.OK,
                                        System.Windows.MessageBoxImage.Information);

                                    // تحديث البيانات
                                    LoadCurrentWeekData();
                                    LoadPreviousArrearsData();
                                }
                                else
                                {
                                    System.Windows.MessageBox.Show(
                                        $"❌ {message}",
                                        "خطأ",
                                        System.Windows.MessageBoxButton.OK,
                                        System.Windows.MessageBoxImage.Error);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressWindow.Close();
                                System.Windows.MessageBox.Show(
                                    $"❌ حدث خطأ: {ex.Message}",
                                    "خطأ",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Error);
                            });
                        }
                    });
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

        #endregion
    }

    /// <summary>
    /// ملخص متأخرات عضو للأسبوع الحالي
    /// </summary>
    public class MemberArrearSummary
    {
        public int PlanID { get; set; }
        public string MemberName { get; set; }
        public int PlanNumber { get; set; }
        public int WeekNumber { get; set; }
        public decimal TotalArrears { get; set; }
        public int DaysCount { get; set; }
        public System.Collections.Generic.List<DailyArrear> DailyArrears { get; set; }
        
        /// <summary>
        /// المبلغ المتبقي (غير المسدد)
        /// </summary>
        public decimal RemainingAmount
        {
            get
            {
                if (DailyArrears == null || !DailyArrears.Any())
                    return TotalArrears;
                return DailyArrears.Where(a => !a.IsPaid).Sum(a => a.RemainingAmount);
            }
        }
        
        /// <summary>
        /// المبلغ المسدد
        /// </summary>
        public decimal PaidAmount
        {
            get
            {
                if (DailyArrears == null || !DailyArrears.Any())
                    return 0;
                return DailyArrears.Sum(a => a.PaidAmount);
            }
        }
        
        /// <summary>
        /// هل تم السداد بالكامل
        /// </summary>
        public bool IsPaid => RemainingAmount == 0;
        
        /// <summary>
        /// الحالة
        /// </summary>
        public string Status
        {
            get
            {
                if (RemainingAmount == 0)
                    return "✅ مسدد";
                else if (PaidAmount > 0)
                    return "🔄 جزئي";
                else
                    return "❌ غير مسدد";
            }
        }
    }

    /// <summary>
    /// ملخص سابقات عضو (مُراكمة)
    /// </summary>
    public class MemberPreviousArrearSummary
    {
        public int PlanID { get; set; }
        public string MemberName { get; set; }
        public int PlanNumber { get; set; }
        public int WeekNumber { get; set; } // الأسبوع الأول الذي بدأت منه السابقات
        public int LastWeekNumber { get; set; } // آخر أسبوع تم ترحيل السابقات فيه
        public decimal TotalArrears { get; set; } // الإجمالي المُراكم
        public System.Collections.Generic.List<PreviousArrears> PreviousArrears { get; set; }
        
        // حقول مباشرة للقيم من AccumulatedArrears
        private decimal? _remainingAmount;
        private decimal? _paidAmount;
        
        /// <summary>
        /// نطاق الأسابيع (مثال: "1-10")
        /// </summary>
        public string WeeksRange
        {
            get
            {
                // إذا كان هناك LastWeekNumber، استخدمه
                if (LastWeekNumber > 0)
                {
                    if (WeekNumber == LastWeekNumber)
                        return WeekNumber.ToString();
                    else
                        return $"{WeekNumber}-{LastWeekNumber}";
                }
                
                // إذا كان هناك PreviousArrears، احسب منها
                if (PreviousArrears != null && PreviousArrears.Any())
                {
                    int minWeek = PreviousArrears.Min(p => p.WeekNumber);
                    int maxWeek = PreviousArrears.Max(p => p.WeekNumber);
                    
                    if (minWeek == maxWeek)
                        return minWeek.ToString();
                    else
                        return $"{minWeek}-{maxWeek}";
                }
                
                return WeekNumber.ToString();
            }
        }
        
        /// <summary>
        /// المبلغ المتبقي (غير المسدد)
        /// </summary>
        public decimal RemainingAmount
        {
            get
            {
                // إذا تم إعداد القيمة مباشرة (من AccumulatedArrears)
                if (_remainingAmount.HasValue)
                    return _remainingAmount.Value;
                
                // إذا لم يتم إعدادها، احسبها من PreviousArrears
                if (PreviousArrears == null || !PreviousArrears.Any())
                    return TotalArrears;
                return PreviousArrears.Sum(p => p.RemainingAmount);
            }
            set => _remainingAmount = value;
        }
        
        /// <summary>
        /// المبلغ المسدد
        /// </summary>
        public decimal PaidAmount
        {
            get
            {
                // إذا تم إعداد القيمة مباشرة (من AccumulatedArrears)
                if (_paidAmount.HasValue)
                    return _paidAmount.Value;
                
                // إذا لم يتم إعدادها، احسبها من PreviousArrears
                if (PreviousArrears == null || !PreviousArrears.Any())
                    return 0;
                return PreviousArrears.Sum(p => p.PaidAmount);
            }
            set => _paidAmount = value;
        }
        
        /// <summary>
        /// الحالة
        /// </summary>
        public string Status
        {
            get
            {
                if (RemainingAmount == 0)
                    return "✅ مسدد";
                else if (PaidAmount > 0)
                    return "🔄 جزئي";
                else
                    return "❌ غير مسدد";
            }
        }
        
        /// <summary>
        /// هل تم السداد بالكامل
        /// </summary>
        public bool IsPaid => RemainingAmount == 0;
    }
}
