using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Data.Repositories;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.SavingPlans
{
    /// <summary>
    /// 📈 ViewModel لإدارة الحصص
    /// </summary>
    public class SavingPlanViewModel : BaseViewModel
    {
        private readonly SavingPlanService _planService;
        private readonly SavingPlanRepository _planRepository;
        private readonly MemberRepository _memberRepository;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;

        private ObservableCollection<SavingPlan> _activePlans;
        private ObservableCollection<SavingPlan> _allActivePlans;
        private ObservableCollection<Member> _members;
        private SavingPlan? _selectedPlan;
        private Member? _selectedMember;
        private string _searchText = string.Empty;
        
        // خصائص الحصة الجديدة
        private decimal _dailyAmount;
        private DateTime _startDate = DateTime.Now;
        private DateTime _endDate = DateTime.Now.AddDays(182);

        public SavingPlanViewModel()
        {
            _planService = new SavingPlanService();
            _planRepository = new SavingPlanRepository();
            _memberRepository = new MemberRepository();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;

            ActivePlans = new ObservableCollection<SavingPlan>();
            _allActivePlans = new ObservableCollection<SavingPlan>();
            Members = new ObservableCollection<Member>();

            // الأوامر
            CreatePlanCommand = new RelayCommand(ExecuteCreatePlan, CanExecuteCreate);
            UpdatePlanCommand = new RelayCommand(ExecuteUpdatePlan, CanExecuteUpdate);
            CompletePlanCommand = new RelayCommand(ExecuteCompletePlan, CanExecuteComplete);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);

            LoadData();
            LoadSettingsDates();
        }

        #region Properties

        public ObservableCollection<SavingPlan> ActivePlans
        {
            get => _activePlans;
            set => SetProperty(ref _activePlans, value);
        }

        public ObservableCollection<Member> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterActivePlans();
                }
            }
        }

        public SavingPlan? SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                SetProperty(ref _selectedPlan, value);
                if (value != null)
                {
                    DailyAmount = value.DailyAmount;
                }
                ((RelayCommand)CompletePlanCommand).RaiseCanExecuteChanged();
                ((RelayCommand)UpdatePlanCommand).RaiseCanExecuteChanged();
            }
        }

        public Member? SelectedMember
        {
            get => _selectedMember;
            set
            {
                SetProperty(ref _selectedMember, value);
                ((RelayCommand)CreatePlanCommand).RaiseCanExecuteChanged();
            }
        }

        public decimal DailyAmount
        {
            get => _dailyAmount;
            set
            {
                SetProperty(ref _dailyAmount, value);
                ((RelayCommand)CreatePlanCommand).RaiseCanExecuteChanged();
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        #endregion

        #region Commands

        public ICommand CreatePlanCommand { get; }
        public ICommand UpdatePlanCommand { get; }
        public ICommand CompletePlanCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanExecuteCreate(object parameter)
        {
            // تبسيط الشرط - إزالة التحقق من الصلاحية مؤقتاً للتشخيص
            bool hasSelectedMember = SelectedMember != null;
            bool hasValidAmount = DailyAmount > 0;
            bool hasPermission = _authService.CurrentUser != null;
            
            System.Diagnostics.Debug.WriteLine($"CanExecuteCreate: Member={hasSelectedMember}, Amount={hasValidAmount}, User={hasPermission}");
            
            return hasSelectedMember && hasValidAmount && hasPermission;
        }

        private void ExecuteCreatePlan(object parameter)
        {
            try
            {
                if (SelectedMember == null) return;

                var userId = _authService.CurrentUser?.UserID ?? 0;
                
                // التحقق من عدد الأسهم النشطة (لا يسمح بأكثر من سهم واحد)
                var activeCount = _planRepository.GetActivePlanCountForMember(SelectedMember.MemberID);
                
                if (activeCount >= 1)
                {
                    System.Windows.MessageBox.Show(
                        $"❌ لا يمكن إضافة أكثر من سهم واحد للعضو!\n\n" +
                        $"العضو {SelectedMember.Name} لديه سهم نشط بالفعل.",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning
                    );
                    return;
                }
                
                int planNumber = 1; // دائماً سهم واحد فقط
                
                var result = _planService.CreatePlan(
                    SelectedMember.MemberID,
                    planNumber,
                    DailyAmount,
                    StartDate,
                    userId);

                if (result.Success)
                {
                    var endDate = Utilities.Helpers.DateHelper.GetEndDate(StartDate);
                    var totalAmount = DailyAmount * 182;
                    
                    System.Windows.MessageBox.Show(
                        $"✅ تم إنشاء الحصة بنجاح!\n\n" +
                        $"📋 رقم الحصة: {planNumber}\n" +
                        $"💰 المبلغ اليومي: {DailyAmount:N2} ريال\n" +
                        $"📅 تاريخ البداية: {StartDate:yyyy-MM-dd}\n" +
                        $"📅 تاريخ النهاية: {endDate:yyyy-MM-dd}\n" +
                        $"💵 الإجمالي: {totalAmount:N2} ريال",
                        "نجاح ✅",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    LoadActivePlans();
                    ClearForm();
                }
                else
                {
                    System.Windows.MessageBox.Show($"❌ {result.Message}", "خطأ",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteUpdate(object parameter)
        {
            return SelectedPlan != null && DailyAmount > 0 && _authService.CurrentUser != null;
        }

        private void ExecuteUpdatePlan(object parameter)
        {
            try
            {
                if (SelectedPlan == null) return;

                // حساب الإجمالي الجديد
                decimal newTotalAmount = DailyAmount * 182;

                var result = System.Windows.MessageBox.Show(
                    $"هل تريد تعديل السهم؟\n\n" +
                    $"العضو: {SelectedPlan.MemberName}\n" +
                    $"المبلغ اليومي الحالي: {SelectedPlan.DailyAmount:N2} ريال\n" +
                    $"المبلغ اليومي الجديد: {DailyAmount:N2} ريال\n\n" +
                    $"الإجمالي الحالي: {SelectedPlan.TotalAmount:N2} ريال\n" +
                    $"الإجمالي الجديد: {newTotalAmount:N2} ريال\n\n" +
                    "⚠️ سيتم تحديث جميع البيانات المرتبطة بهذا السهم",
                    "تأكيد التعديل",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // تحديث السهم
                    SelectedPlan.DailyAmount = DailyAmount;
                    SelectedPlan.TotalAmount = newTotalAmount;

                    bool updateResult = _planRepository.Update(SelectedPlan);

                    if (updateResult)
                    {
                        System.Windows.MessageBox.Show(
                            $"✅ تم تعديل السهم بنجاح!\n\n" +
                            $"العضو: {SelectedPlan.MemberName}\n" +
                            $"المبلغ اليومي الجديد: {DailyAmount:N2} ريال\n" +
                            $"الإجمالي الجديد: {newTotalAmount:N2} ريال",
                            "نجاح",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        LoadData();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "❌ فشل تعديل السهم",
                            "خطأ",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تعديل السهم: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteComplete(object parameter)
        {
            return SelectedPlan != null && _authService.CurrentUser != null;
        }

        private void ExecuteCompletePlan(object parameter)
        {
            if (SelectedPlan == null) return;

            var result = System.Windows.MessageBox.Show(
                $"هل أنت متأكد من إتمام الحصة رقم {SelectedPlan.PlanNumber}؟\n\n" +
                $"سيتم إغلاق الحصة وتحويل المبلغ للخزنة.",
                "تأكيد الإتمام 🎉",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var userId = _authService.CurrentUser?.UserID ?? 0;
                var completeResult = _planService.CompletePlan(SelectedPlan.PlanID, userId);

                if (completeResult.Success)
                {
                    System.Windows.MessageBox.Show(
                        $"✅ {completeResult.Message}\n\n" +
                        $"🎉 تم إتمام الحصة بنجاح!",
                        "نجاح",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    LoadActivePlans();
                }
                else
                {
                    System.Windows.MessageBox.Show($"❌ {completeResult.Message}", "خطأ",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadData();
        }

        #endregion

        #region Helper Methods

        private void LoadData()
        {
            LoadMembers();
            LoadActivePlans();
        }

        private void LoadMembers()
        {
            try
            {
                var members = _memberRepository.GetActive();
                Members.Clear();
                foreach (var member in members)
                {
                    Members.Add(member);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل الأعضاء: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadActivePlans()
        {
            try
            {
                // ترتيب حسب المبلغ اليومي (من الأكبر إلى الأصغر)
                var plans = _planRepository.GetActive().OrderByDescending(p => p.DailyAmount);
                
                _allActivePlans.Clear();
                foreach (var plan in plans)
                {
                    _allActivePlans.Add(plan);
                }
                
                FilterActivePlans();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل الحصص: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void FilterActivePlans()
        {
            try
            {
                ActivePlans.Clear();
                
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    // عرض جميع الأسهم
                    foreach (var plan in _allActivePlans)
                    {
                        ActivePlans.Add(plan);
                    }
                }
                else
                {
                    // فلترة حسب اسم العضو
                    var filtered = _allActivePlans.Where(p => 
                        p.MemberName != null && 
                        p.MemberName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                    
                    foreach (var plan in filtered)
                    {
                        ActivePlans.Add(plan);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في الفلترة: {ex.Message}");
            }
        }

        private void ClearForm()
        {
            SelectedMember = null;
            DailyAmount = 0;
            LoadSettingsDates();
        }

        /// <summary>
        /// تحميل تواريخ البداية والنهاية من الإعدادات
        /// </summary>
        private void LoadSettingsDates()
        {
            try
            {
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    StartDate = settings.StartDate;
                    EndDate = settings.EndDate;
                }
                else
                {
                    // إذا لم توجد إعدادات، استخدم القيم الافتراضية
                    StartDate = DateTime.Now;
                    EndDate = DateTime.Now.AddDays(182);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل تواريخ الإعدادات: {ex.Message}\nسيتم استخدام التواريخ الافتراضية.",
                    "تنبيه",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning
                );
                StartDate = DateTime.Now;
                EndDate = DateTime.Now.AddDays(182);
            }
        }

        #endregion
    }
}
