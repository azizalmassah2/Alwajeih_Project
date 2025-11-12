using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.ViewModels.Base;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.ViewModels.Finance
{
    /// <summary>
    /// ViewModel لإضافة خرجيات ومفقودات حسب الأسبوع واليوم
    /// </summary>
    public class AddOtherTransactionViewModel : BaseViewModel
    {
        private readonly OtherTransactionRepository _otherTransactionRepository;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;

        private int _selectedWeekNumber;
        private int _selectedDayNumber;
        private decimal _amount;
        private string _description;
        private DateTime _transactionDate;
        private List<int> _availableWeeks;
        private List<DayOption> _availableDays;
        private string _selectedTransactionType;
        private List<TransactionTypeOption> _transactionTypes;

        public event Action? OnSaveSuccess;

        public AddOtherTransactionViewModel()
        {
            _otherTransactionRepository = new OtherTransactionRepository();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;

            // تهيئة الأوامر أولاً
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel, _ => true);

            // تحميل الأسابيع المتاحة
            LoadAvailableWeeks();
            LoadAvailableDays();
            LoadTransactionTypes();

            // تعيين القيم الافتراضية
            var (currentWeek, currentDay) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);
            SelectedWeekNumber = currentWeek;
            SelectedDayNumber = currentDay;
            TransactionDate = DateTime.Now;
            SelectedTransactionType = "خرجيات";
        }

        #region Properties

        public int SelectedWeekNumber
        {
            get => _selectedWeekNumber;
            set
            {
                if (SetProperty(ref _selectedWeekNumber, value))
                {
                    UpdateTransactionDate();
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public int SelectedDayNumber
        {
            get => _selectedDayNumber;
            set
            {
                if (SetProperty(ref _selectedDayNumber, value))
                {
                    UpdateTransactionDate();
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                if (SetProperty(ref _amount, value))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime TransactionDate
        {
            get => _transactionDate;
            set => SetProperty(ref _transactionDate, value);
        }

        public List<int> AvailableWeeks
        {
            get => _availableWeeks;
            set => SetProperty(ref _availableWeeks, value);
        }

        public List<DayOption> AvailableDays
        {
            get => _availableDays;
            set => SetProperty(ref _availableDays, value);
        }

        public string SelectedTransactionType
        {
            get => _selectedTransactionType;
            set
            {
                if (SetProperty(ref _selectedTransactionType, value))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public List<TransactionTypeOption> TransactionTypes
        {
            get => _transactionTypes;
            set => SetProperty(ref _transactionTypes, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Methods

        private void LoadAvailableWeeks()
        {
            try
            {
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null && settings.StartDate != default)
                {
                    var currentDate = DateTime.Now;
                    var totalWeeks = WeekHelper.GetWeekNumber(currentDate);
                    AvailableWeeks = Enumerable.Range(1, totalWeeks).ToList();
                }
                else
                {
                    AvailableWeeks = Enumerable.Range(1, 26).ToList();
                }
            }
            catch
            {
                AvailableWeeks = Enumerable.Range(1, 26).ToList();
            }
        }

        private void LoadAvailableDays()
        {
            AvailableDays = new List<DayOption>
            {
                new DayOption { DayNumber = 1, DayName = "السبت" },
                new DayOption { DayNumber = 2, DayName = "الأحد" },
                new DayOption { DayNumber = 3, DayName = "الاثنين" },
                new DayOption { DayNumber = 4, DayName = "الثلاثاء" },
                new DayOption { DayNumber = 5, DayName = "الأربعاء" },
                new DayOption { DayNumber = 6, DayName = "الخميس" },
                new DayOption { DayNumber = 7, DayName = "الجمعة" }
            };
        }

        private void LoadTransactionTypes()
        {
            TransactionTypes = new List<TransactionTypeOption>
            {
                new TransactionTypeOption { Type = "خرجيات", Description = "خرجيات" },
                new TransactionTypeOption { Type = "مفقودات", Description = "مفقود" },
                new TransactionTypeOption { Type = "مصروفات", Description = "مصروف" },
                new TransactionTypeOption { Type = "أخرى", Description = "عمليات أخرى" }
            };
        }

        private void UpdateTransactionDate()
        {
            try
            {
                TransactionDate = WeekHelper.GetDateFromWeekAndDay(SelectedWeekNumber, SelectedDayNumber);
            }
            catch
            {
                TransactionDate = DateTime.Now;
            }
        }

        private bool CanExecuteSave(object parameter)
        {
            return Amount > 0 && 
                   SelectedWeekNumber > 0 && 
                   SelectedDayNumber > 0 &&
                   !string.IsNullOrEmpty(SelectedTransactionType) &&
                   _authService.HasPermission("ManageReconciliation");
        }

        private void ExecuteSave(object parameter)
        {
            try
            {
                var transaction = new OtherTransaction
                {
                    WeekNumber = SelectedWeekNumber,
                    DayNumber = SelectedDayNumber,
                    TransactionDate = TransactionDate,
                    Amount = Amount,
                    TransactionType = SelectedTransactionType,
                    Notes = Description,
                    CreatedBy = _authService.CurrentUser?.UserID ?? 0,
                    CreatedAt = DateTime.Now
                };

                int transactionId = _otherTransactionRepository.Add(transaction);

                if (transactionId > 0)
                {
                    System.Windows.MessageBox.Show(
                        $"تم تسجيل الخرجية بنجاح\n\nالمبلغ: {Amount:N2} ريال\nالأسبوع {SelectedWeekNumber} - {WeekHelper.GetArabicDayName(SelectedDayNumber)}",
                        "نجاح العملية",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    OnSaveSuccess?.Invoke();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "فشل تسجيل الخرجية. الرجاء المحاولة مرة أخرى.",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"حدث خطأ أثناء حفظ الخرجية:\n{ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel(object parameter)
        {
            OnSaveSuccess?.Invoke(); // لإغلاق النافذة
        }

        #endregion
    }

    public class DayOption
    {
        public int DayNumber { get; set; }
        public string DayName { get; set; }
    }

    public class TransactionTypeOption
    {
        public string Type { get; set; }
        public string Description { get; set; }
    }
}
