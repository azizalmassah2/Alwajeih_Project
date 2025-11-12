using System;
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
    public class QuickPreviousArrearsEntryViewModel : BaseViewModel
    {
        private readonly SavingPlanRepository _planRepository;
        private readonly AccumulatedArrearsRepository _accumulatedArrearsRepository;
        private readonly AuthenticationService _authService;
        private readonly ArrearService _arrearService;
        
        private ObservableCollection<SavingPlan> _allMembers;
        private ObservableCollection<SavingPlan> _filteredMembers;
        private SavingPlan? _selectedMember;
        private decimal _quickRemaining;
        private decimal _totalOriginalAmount;
        private decimal _calculatedPaid;
        private string _quickNotes = string.Empty;
        private string _searchText = string.Empty;
        private int _autoWeekTo;
        
        public event Action? OnSaveSuccess;

        public QuickPreviousArrearsEntryViewModel()
        {
            _planRepository = new SavingPlanRepository();
            _accumulatedArrearsRepository = new AccumulatedArrearsRepository();
            _arrearService = new ArrearService();
            _authService = AuthenticationService.Instance;
            
            SaveQuickEntryCommand = new RelayCommand(ExecuteSaveQuickEntry, CanExecuteSaveQuickEntry);
            
            LoadMembers();
            CalculateAutoWeeks();
        }

        #region Properties

        public ObservableCollection<SavingPlan> AllMembers
        {
            get => _allMembers;
            set => SetProperty(ref _allMembers, value);
        }

        public ObservableCollection<SavingPlan> FilteredMembers
        {
            get => _filteredMembers;
            set => SetProperty(ref _filteredMembers, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterMembers();
                }
            }
        }

        public SavingPlan? SelectedMember
        {
            get => _selectedMember;
            set
            {
                if (SetProperty(ref _selectedMember, value))
                {
                    LoadMemberPreviousArrears();
                    ((RelayCommand)SaveQuickEntryCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public decimal TotalOriginalAmount
        {
            get => _totalOriginalAmount;
            set
            {
                SetProperty(ref _totalOriginalAmount, value);
                UpdateCalculatedPaid();
            }
        }

        public decimal QuickRemaining
        {
            get => _quickRemaining;
            set
            {
                SetProperty(ref _quickRemaining, value);
                UpdateCalculatedPaid();
                ((RelayCommand)SaveQuickEntryCommand).RaiseCanExecuteChanged();
            }
        }

        public decimal CalculatedPaid
        {
            get => _calculatedPaid;
            set => SetProperty(ref _calculatedPaid, value);
        }

        public string QuickNotes
        {
            get => _quickNotes;
            set => SetProperty(ref _quickNotes, value);
        }

        public int AutoWeekTo
        {
            get => _autoWeekTo;
            set => SetProperty(ref _autoWeekTo, value);
        }

        #endregion

        #region Commands

        public ICommand SaveQuickEntryCommand { get; }

        #endregion

        #region Command Implementations

        private void ExecuteSaveQuickEntry(object parameter)
        {
            try
            {
                // ✅ السماح بـ 0 (يعني لا متأخرات)
                if (SelectedMember == null || QuickRemaining < 0)
                    return;

                var (success, message) = _arrearService.AddDirectPreviousArrears(
                    SelectedMember.PlanID,
                    1,  // من الأسبوع 1
                    AutoWeekTo,  // إلى الأسبوع الحالي - 1
                    TotalOriginalAmount,  // المبلغ الأصلي (الإجمالي - ثابت)
                    QuickRemaining,  // المتبقي (من الإدخال)
                    QuickNotes,
                    _authService.CurrentUser?.UserID ?? 1
                );

                if (success)
                {
                    System.Windows.MessageBox.Show(
                        $"✅ تم تسجيل السابقات للعضو: {SelectedMember.MemberName}\nالمبلغ: {QuickRemaining:N2} ريال",
                        "نجاح",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    // تنظيف الحقول للعضو التالي
                    TotalOriginalAmount = 0;
                    QuickRemaining = 0;
                    CalculatedPaid = 0;
                    QuickNotes = string.Empty;
                    SelectedMember = null;
                    SearchText = string.Empty;
                    
                    // إطلاق حدث النجاح
                    OnSaveSuccess?.Invoke();
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

        private bool CanExecuteSaveQuickEntry(object parameter)
        {
            // ✅ السماح بـ 0 (يعني لا متأخرات)
            return SelectedMember != null && QuickRemaining >= 0;
        }

        #endregion

        #region Helper Methods

        private void LoadMembers()
        {
            try
            {
                var activePlans = _planRepository.GetActive().OrderBy(p => p.MemberName).ToList();
                AllMembers = new ObservableCollection<SavingPlan>(activePlans);
                FilteredMembers = new ObservableCollection<SavingPlan>(activePlans);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحميل الأعضاء: {ex.Message}");
                AllMembers = new ObservableCollection<SavingPlan>();
                FilteredMembers = new ObservableCollection<SavingPlan>();
            }
        }

        private void FilterMembers()
        {
            if (AllMembers == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredMembers = new ObservableCollection<SavingPlan>(AllMembers);
                SelectedMember = null;
            }
            else
            {
                var searchLower = SearchText.Trim().ToLower();
                var filtered = AllMembers.Where(m =>
                    m.MemberName?.ToLower().Contains(searchLower) == true ||
                    m.PlanNumber.ToString().Contains(searchLower)
                ).ToList();

                FilteredMembers = new ObservableCollection<SavingPlan>(filtered);
                
                // اختيار تلقائي إذا كان هناك نتيجة واحدة فقط
                if (filtered.Count == 1)
                {
                    SelectedMember = filtered[0];
                }
                else if (filtered.Count == 0)
                {
                    SelectedMember = null;
                }
            }
        }

        private void UpdateCalculatedPaid()
        {
            if (TotalOriginalAmount > 0 && QuickRemaining >= 0)
            {
                CalculatedPaid = TotalOriginalAmount - QuickRemaining;
            }
            else
            {
                CalculatedPaid = 0;
            }
        }

        private void LoadMemberPreviousArrears()
        {
            if (SelectedMember == null)
            {
                TotalOriginalAmount = 0;
                QuickRemaining = 0;
                return;
            }

            try
            {
                // جلب السابقات المتراكمة من جدول AccumulatedArrears (سجل واحد فقط)
                var accumulated = _accumulatedArrearsRepository.GetByPlanId(SelectedMember.PlanID);

                if (accumulated != null && accumulated.RemainingAmount > 0)
                {
                    // تحميل البيانات من السجل المتراكم
                    TotalOriginalAmount = accumulated.TotalArrears;
                    QuickRemaining = accumulated.RemainingAmount;
                }
                else
                {
                    // لا توجد سابقات متراكمة
                    TotalOriginalAmount = 0;
                    QuickRemaining = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحميل السابقات: {ex.Message}");
                TotalOriginalAmount = 0;
                QuickRemaining = 0;
            }
        }

        private void CalculateAutoWeeks()
        {
            try
            {
                var settings = new SystemSettingsRepository().GetCurrentSettings();
                if (settings != null)
                {
                    WeekHelper.StartDate = settings.StartDate;
                }

                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                AutoWeekTo = currentWeek > 1 ? currentWeek - 1 : 1;
            }
            catch
            {
                AutoWeekTo = 1;
            }
        }

        #endregion
    }
}
