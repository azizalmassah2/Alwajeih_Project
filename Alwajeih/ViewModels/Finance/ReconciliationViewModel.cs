using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Data.Repositories;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Finance
{
    /// <summary>
    /// 📊 ViewModel للجرد الأسبوعي
    /// </summary>
    public class ReconciliationViewModel : BaseViewModel
    {
        private readonly ReconciliationService _reconciliationService;
        private readonly ReconciliationRepository _reconciliationRepository;
        private readonly AuthenticationService _authService;
        private readonly SystemSettingsRepository _settingsRepository;

        private int _selectedWeek;
        private DateTime _weekStart;
        private DateTime _weekEnd;
        private decimal _expectedAmount;
        private decimal _actualAmount;
        private decimal _difference;
        private string _notes;
        private ObservableCollection<int> _weeks;
        private ObservableCollection<WeeklyReconciliation> _previousReconciliations;
        
        // إحصائيات الجرد التفصيلية
        private decimal _totalIncome;
        private decimal _totalExpenses;
        private decimal _totalWithdrawals;
        private decimal _totalArrears;
        private decimal _totalPreviousArrears;
        private decimal _previousBalance;
        private decimal _finalBalance;
        private int _collectionsCount;
        private int _arrearsCount;

        public ReconciliationViewModel()
        {
            _reconciliationService = new ReconciliationService();
            _reconciliationRepository = new ReconciliationRepository();
            _authService = AuthenticationService.Instance;
            _settingsRepository = new SystemSettingsRepository();

            Weeks = new ObservableCollection<int>();
            PreviousReconciliations = new ObservableCollection<WeeklyReconciliation>();

            CalculateCommand = new RelayCommand(ExecuteCalculate, _ => true);
            SubmitCommand = new RelayCommand(ExecuteSubmit, CanExecuteSubmit);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            PreviousWeekCommand = new RelayCommand(ExecutePreviousWeek, _ => SelectedWeek > 1);
            NextWeekCommand = new RelayCommand(ExecuteNextWeek, _ => SelectedWeek < 26);
            AddOtherTransactionCommand = new RelayCommand(ExecuteAddOtherTransaction, _ => _authService.HasPermission("ManageReconciliation"));

            // تحميل تاريخ البداية من الإعدادات أولاً
            LoadStartDateFromSettings();
            
            LoadWeeks();
            LoadCurrentWeek();
            LoadPreviousReconciliations();
        }

        #region Properties

        public int SelectedWeek
        {
            get => _selectedWeek;
            set
            {
                if (SetProperty(ref _selectedWeek, value))
                {
                    System.Diagnostics.Debug.WriteLine($"📅 تم تغيير الأسبوع إلى: {value}");
                    
                    // ✅ تحديث تواريخ الأسبوع المختار
                    UpdateWeekDates();
                    
                    // ✅ مسح النموذج عند تغيير الأسبوع
                    ClearForm();
                    
                    // ✅ حساب المبلغ المتوقع للأسبوع الجديد
                    ExecuteCalculate(null);
                    
                    // ✅ تحديث أوامر التنقل
                    ((RelayCommand)PreviousWeekCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)NextWeekCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<int> Weeks
        {
            get => _weeks;
            set => SetProperty(ref _weeks, value);
        }

        public DateTime WeekStart
        {
            get => _weekStart;
            set => SetProperty(ref _weekStart, value);
        }

        public DateTime WeekEnd
        {
            get => _weekEnd;
            set => SetProperty(ref _weekEnd, value);
        }

        public decimal ExpectedAmount
        {
            get => _expectedAmount;
            set
            {
                SetProperty(ref _expectedAmount, value);
                CalculateDifference();
            }
        }

        public decimal ActualAmount
        {
            get => _actualAmount;
            set
            {
                SetProperty(ref _actualAmount, value);
                CalculateDifference();
                ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged();
            }
        }

        public decimal Difference
        {
            get => _difference;
            set => SetProperty(ref _difference, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public ObservableCollection<WeeklyReconciliation> PreviousReconciliations
        {
            get => _previousReconciliations;
            set => SetProperty(ref _previousReconciliations, value);
        }

        // إحصائيات الجرد
        public decimal TotalIncome
        {
            get => _totalIncome;
            set => SetProperty(ref _totalIncome, value);
        }

        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set => SetProperty(ref _totalExpenses, value);
        }

        public decimal TotalWithdrawals
        {
            get => _totalWithdrawals;
            set => SetProperty(ref _totalWithdrawals, value);
        }

        public decimal TotalArrears
        {
            get => _totalArrears;
            set => SetProperty(ref _totalArrears, value);
        }

        public decimal TotalPreviousArrears
        {
            get => _totalPreviousArrears;
            set => SetProperty(ref _totalPreviousArrears, value);
        }

        public decimal PreviousBalance
        {
            get => _previousBalance;
            set => SetProperty(ref _previousBalance, value);
        }

        public decimal FinalBalance
        {
            get => _finalBalance;
            set => SetProperty(ref _finalBalance, value);
        }

        public int CollectionsCount
        {
            get => _collectionsCount;
            set => SetProperty(ref _collectionsCount, value);
        }

        public int ArrearsCount
        {
            get => _arrearsCount;
            set => SetProperty(ref _arrearsCount, value);
        }

        // الواردات والخرجيات المجمعة
        public decimal TotalDues => TotalExpenses + TotalWithdrawals;

        #endregion

        #region Commands

        public ICommand CalculateCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand AddOtherTransactionCommand { get; }

        #endregion

        #region Command Implementations

        private void ExecuteCalculate(object parameter)
        {
            try
            {
                if (SelectedWeek < 1 || SelectedWeek > 26)
                    return;

                // ✅ التأكد من تحديث تواريخ الأسبوع المختار
                UpdateWeekDates();
                
                // ✅ حساب المبلغ المتوقع للأسبوع المختار
                ExpectedAmount = _reconciliationService.CalculateExpectedAmount(SelectedWeek);
                
                // ✅ تحميل الإحصائيات التفصيلية للأسبوع المختار
                LoadWeekStatistics();
                
                System.Diagnostics.Debug.WriteLine($"✅ تم حساب الجرد للأسبوع {SelectedWeek} ({WeekStart:yyyy-MM-dd} - {WeekEnd:yyyy-MM-dd})");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في الحساب: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSubmit(object parameter)
        {
            return ActualAmount > 0 && _authService.HasPermission("SubmitReconciliation");
        }

        private void ExecuteSubmit(object parameter)
        {
            try
            {
                // تحقق من وجود فرق كبير
                if (Math.Abs(Difference) > ExpectedAmount * 0.01m && string.IsNullOrWhiteSpace(Notes))
                {
                    System.Windows.MessageBox.Show(
                        "⚠️ يوجد فرق كبير بين المبلغ المتوقع والفعلي!\n\n" +
                        "يرجى إدخال ملاحظات توضيحية.",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    $"📊 ملخص الجرد:\n\n" +
                    $"📅 الأسبوع: {SelectedWeek} ({WeekStart:dd/MM/yyyy} - {WeekEnd:dd/MM/yyyy})\n\n" +
                    $"💰 المتوقع: {ExpectedAmount:N2} ريال\n" +
                    $"💵 الفعلي: {ActualAmount:N2} ريال\n" +
                    $"📉 الفرق: {Difference:N2} ريال\n\n" +
                    $"هل تريد إتمام الجرد وترحيل المبلغ للخزنة؟",
                    $"تأكيد الجرد - الأسبوع {SelectedWeek}",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var userId = _authService.CurrentUser?.UserID ?? 0;
                    
                    System.Diagnostics.Debug.WriteLine($"🔄 جرد الأسبوع {SelectedWeek}: المتوقع={ExpectedAmount:N2}, الفعلي={ActualAmount:N2}");
                    
                    var submitResult = _reconciliationService.SubmitReconciliation(
                        SelectedWeek, ActualAmount, Notes, userId);

                    if (submitResult.Success)
                    {
                        System.Windows.MessageBox.Show(
                            $"✅ تم إتمام جرد الأسبوع {SelectedWeek} بنجاح!\n\n" +
                            "تم ترحيل المبلغ إلى الخزنة 🏦",
                            "نجاح",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        // ✅ الحفاظ على الأسبوع المختار بعد نجاح الجرد
                        int justReconciledWeek = SelectedWeek;
                        
                        // تحديث البيانات
                        ExecuteRefresh(null);
                        
                        System.Diagnostics.Debug.WriteLine($"✅ تم جرد الأسبوع {justReconciledWeek} بنجاح");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"❌ {submitResult.Message}", "خطأ",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRefresh(object parameter)
        {
            // ✅ الحفاظ على الأسبوع المختار وعدم العودة للأسبوع الحالي
            int currentlySelectedWeek = SelectedWeek;
            
            // إعادة حساب البيانات للأسبوع المختار
            ExecuteCalculate(null);
            
            // تحديث السجلات السابقة
            LoadPreviousReconciliations();
            
            System.Diagnostics.Debug.WriteLine($"🔄 تم تحديث بيانات الأسبوع {currentlySelectedWeek}");
        }

        private void ExecutePreviousWeek(object parameter)
        {
            if (SelectedWeek > 1)
            {
                SelectedWeek--;
            }
        }

        private void ExecuteNextWeek(object parameter)
        {
            if (SelectedWeek < 26)
            {
                SelectedWeek++;
            }
        }

        private void ExecuteAddOtherTransaction(object parameter)
        {
            var window = new Views.Finance.AddOtherTransactionWindow();
            if (window.ShowDialog() == true)
            {
                // إعادة حساب الجرد بعد إضافة الخرجية
                ExecuteCalculate(null);
            }
        }

        #endregion

        #region Helper Methods

        private void LoadWeeks()
        {
            Weeks.Clear();
            for (int i = 1; i <= 26; i++)
            {
                Weeks.Add(i);
            }
        }

        private void LoadCurrentWeek()
        {
            SelectedWeek = _reconciliationService.GetCurrentWeekNumber();
            LoadWeekStatistics();
        }

        private void UpdateWeekDates()
        {
            if (SelectedWeek < 1 || SelectedWeek > 26)
                return;

            var (start, end) = Utilities.Helpers.WeekHelper.GetWeekDateRange(SelectedWeek);
            WeekStart = start;
            WeekEnd = end;
        }

        private void LoadPreviousReconciliations()
        {
            try
            {
                var reconciliations = _reconciliationRepository.GetByDateRange(
                    DateTime.Now.AddMonths(-3), DateTime.Now);
                
                PreviousReconciliations.Clear();
                foreach (var rec in reconciliations)
                {
                    PreviousReconciliations.Add(rec);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل السجلات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CalculateDifference()
        {
            Difference = ActualAmount - ExpectedAmount;
        }

        private void ClearForm()
        {
            ActualAmount = 0;
            Notes = string.Empty;
        }

        private void LoadWeekStatistics()
        {
            try
            {
                if (SelectedWeek < 1 || SelectedWeek > 26)
                    return;

                var dailyCollectionRepo = new DailyCollectionRepository();
                var vaultRepo = new VaultRepository();
                var arrearRepo = new ArrearRepository();
                var otherTransactionRepo = new OtherTransactionRepository();
                
                // 1️⃣ التحصيلات في هذا الأسبوع (من DailyCollections)
                var collections = dailyCollectionRepo.GetCollectionsByWeek(SelectedWeek)
                    .Where(c => !c.IsCancelled).ToList();
                
                // التحصيل اليومي
                decimal todayPayments = collections.Sum(c => c.AmountPaid);
                
                // سداد السابقات (من AccumulatedArrears - المبالغ المدفوعة في هذا الأسبوع)
                // نقرأ PaidAmount للأعضاء الذين LastWeekNumber == SelectedWeek
                var accumulatedArrearsRepo = new Data.Repositories.AccumulatedArrearsRepository();
                decimal previousArrearPayments = accumulatedArrearsRepo.GetAll()
                    .Where(a => a.LastWeekNumber == SelectedWeek)
                    .Sum(a => a.PaidAmount);
                
                // سداد متأخرات الأسبوع (من DailyArrears)
                var weekArrears = arrearRepo.GetArrearsByWeek(SelectedWeek);
                decimal arrearsPayments = weekArrears
                    .Where(a => a.IsPaid && a.PaidDate.HasValue && 
                               a.PaidDate.Value.Date >= WeekStart && a.PaidDate.Value.Date <= WeekEnd)
                    .Sum(a => a.PaidAmount);
                
                // ✅ دفعات أعضاء خلف الجمعية (نظام الأمانة)
                var behindAssociationRepo = new Data.Repositories.BehindAssociation.BehindAssociationRepository();
                decimal behindAssociationDeposits = behindAssociationRepo.GetWeekTotalDeposits(SelectedWeek);
                
                // إجمالي التحصيل (ما في يد المستخدم)
                TotalIncome = todayPayments + arrearsPayments + previousArrearPayments + behindAssociationDeposits;
                
                // Debug: طباعة المكونات
                System.Diagnostics.Debug.WriteLine($"💰 الصندوق - الأسبوع {SelectedWeek}:");
                System.Diagnostics.Debug.WriteLine($"  - التحصيل اليومي: {todayPayments:N2}");
                System.Diagnostics.Debug.WriteLine($"  - سداد متأخرات: {arrearsPayments:N2}");
                System.Diagnostics.Debug.WriteLine($"  - سداد سابقات: {previousArrearPayments:N2}");
                System.Diagnostics.Debug.WriteLine($"  - خلف الجمعية: {behindAssociationDeposits:N2}");
                System.Diagnostics.Debug.WriteLine($"  = الإجمالي: {TotalIncome:N2}");
                CollectionsCount = collections.Count;
                
                // 2️⃣ الخرجيات والمفقودات (من OtherTransactions)
                // ✅ الجرد الأسبوعي مستقل عن الخزنة
                var otherTransactions = otherTransactionRepo.GetByWeek(SelectedWeek).ToList();
                TotalExpenses = otherTransactions.Sum(t => t.Amount);
                TotalWithdrawals = 0; // الجرد لا يحسب سحوبات الخزنة
                
                // 4️⃣ المتأخرات الجديدة في هذا الأسبوع (غير المسددة)
                TotalArrears = weekArrears.Where(a => !a.IsPaid).Sum(a => a.RemainingAmount);
                ArrearsCount = weekArrears.Where(a => !a.IsPaid).Select(a => a.PlanID).Distinct().Count();
                
                // 5️⃣ السابقات المتراكمة غير المسددة (من AccumulatedArrears)
                // ✅ السابقات تظهر في الأسبوع الحالي ما لم يكن الأسبوع السابق مجرود
                // نتحقق: هل الأسبوع السابق مجرود؟
                bool isPreviousWeekReconciled = false;
                if (SelectedWeek > 1)
                {
                    var (prevStart, prevEnd) = Utilities.Helpers.WeekHelper.GetWeekDateRange(SelectedWeek - 1);
                    var prevReconciliations = _reconciliationRepository.GetByDateRange(prevStart, prevEnd);
                    isPreviousWeekReconciled = prevReconciliations.Any();
                }
                
                // إذا لم يكن الأسبوع السابق مجرود، نعرض السابقات
                if (!isPreviousWeekReconciled || SelectedWeek == 1)
                {
                    var accumulatedArrears = accumulatedArrearsRepo.GetAll()
                        .Where(a => !a.IsPaid && a.LastWeekNumber <= SelectedWeek).ToList();
                    TotalPreviousArrears = accumulatedArrears.Sum(a => a.RemainingAmount);
                }
                else
                {
                    // الأسبوع السابق مجرود، نعرض السابقات للأسبوع الحالي فقط
                    var accumulatedArrears = accumulatedArrearsRepo.GetAll()
                        .Where(a => !a.IsPaid && a.LastWeekNumber == SelectedWeek).ToList();
                    TotalPreviousArrears = accumulatedArrears.Sum(a => a.RemainingAmount);
                }
                
                // 6️⃣ الرصيد السابق
                // ✅ الجرد الأسبوعي مستقل - لا يحتاج رصيد سابق
                PreviousBalance = 0;
                
                // 7️⃣ الرصيد النهائي المتوقع (صافي الأسبوع)
                // ✅ الجرد = الدخل - الخرجيات فقط
                FinalBalance = TotalIncome - TotalExpenses;
                
                // إشعار بتحديث TotalDues
                OnPropertyChanged(nameof(TotalDues));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل الإحصائيات: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
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
                    Utilities.Helpers.WeekHelper.StartDate = settings.StartDate;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحميل تاريخ البداية: {ex.Message}");
            }
        }
    }
}
#endregion