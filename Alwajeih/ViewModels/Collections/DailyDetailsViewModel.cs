using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Utilities;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Collections
{
    /// <summary>
    /// ViewModel لصفحة التفاصيل اليومية
    /// </summary>
    public class DailyDetailsViewModel : BaseViewModel
    {
        private readonly WeekSummaryService _summaryService;

        private string _itemTitle;
        private int _weekNumber;
        private decimal _totalAmount;
        private ObservableCollection<DailySummary> _dailyDetails;

        public DailyDetailsViewModel()
        {
            _summaryService = new WeekSummaryService();
            DailyDetails = new ObservableCollection<DailySummary>();
            BackCommand = new RelayCommand(ExecuteBack, _ => true);
        }

        #region Properties

        public string ItemTitle
        {
            get => _itemTitle;
            set => SetProperty(ref _itemTitle, value);
        }

        public int WeekNumber
        {
            get => _weekNumber;
            set => SetProperty(ref _weekNumber, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public ObservableCollection<DailySummary> DailyDetails
        {
            get => _dailyDetails;
            set => SetProperty(ref _dailyDetails, value);
        }

        #endregion

        #region Commands

        public ICommand BackCommand { get; }

        #endregion

        #region Command Implementations

        private void ExecuteBack(object parameter)
        {
            // إغلاق النافذة الحالية للرجوع
            var window = System
                .Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.Content is Views.Collections.DailyDetailsView);

            window?.Close();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// تحميل البيانات بناءً على البند المختار
        /// </summary>
        public void LoadData(WeekSummaryItemType itemType, int weekNumber)
        {
            WeekNumber = weekNumber;
            ItemTitle = GetItemTitle(itemType, weekNumber);

            DailyDetails.Clear();

            // جلب البيانات الفعلية من قاعدة البيانات
            var dailyDetails = _summaryService.GetDailyDetails(weekNumber, itemType);

            foreach (var detail in dailyDetails)
            {
                DailyDetails.Add(detail);
            }

            // حساب المجموع حسب النوع
            TotalAmount = 0;
            foreach (var detail in DailyDetails)
            {
                TotalAmount += itemType switch
                {
                    WeekSummaryItemType.Income => detail.DailyIncome,
                    WeekSummaryItemType.Dues => detail.DailyDues,
                    WeekSummaryItemType.Arrears => detail.DailyArrears,
                    WeekSummaryItemType.ManagerWithdrawals => detail.DailyWithdrawals,
                    _ => detail.DailyIncome
                };
            }
        }

        #endregion

        #region Helper Methods

        private string GetItemTitle(WeekSummaryItemType itemType, int weekNumber)
        {
            return itemType switch
            {
                WeekSummaryItemType.Income => $"واردات الأسبوع {weekNumber}",
                WeekSummaryItemType.PreviousBalance => $"سابقات الأسبوع {weekNumber - 1}",
                WeekSummaryItemType.Dues => $"ماعليكم للأسبوع {weekNumber}",
                WeekSummaryItemType.Arrears => $"متأخرات الأسبوع {weekNumber}",
                WeekSummaryItemType.ManagerWithdrawals => $"خرجيات المدير - الأسبوع {weekNumber}",
                WeekSummaryItemType.AssociationDebts => $"خلف الجمعية - الأسبوع {weekNumber}",
                WeekSummaryItemType.Missing => $"مفقود - الأسبوع {weekNumber}",
                WeekSummaryItemType.Graduates => $"خريجات - الأسبوع {weekNumber}",
                _ => $"الأسبوع {weekNumber}",
            };
        }

        #endregion
    }
}
