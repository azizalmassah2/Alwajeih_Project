using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Utilities;
using Alwajeih.Utilities.Helpers;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Collections
{
    /// <summary>
    /// ViewModel لملخص الأسبوع
    /// </summary>
    public class WeekSummaryViewModel : BaseViewModel
    {
        private readonly WeekSummaryService _summaryService;

        private int _currentWeek = 1;
        private string _searchText;
        private ObservableCollection<WeekSummaryItem> _summaryItems;
        private decimal _totalExpected;
        private decimal _totalCollected;
        private decimal _totalRemaining;
        private decimal _previousBalance;
        private decimal _otherTransactions;
        private decimal _managerWithdrawals;
        private decimal _associationDebts;
        private decimal _finalBalance;

        public WeekSummaryViewModel()
        {
            _summaryService = new WeekSummaryService();
            SummaryItems = new ObservableCollection<WeekSummaryItem>();

            PreviousWeekCommand = new RelayCommand(ExecutePreviousWeek, CanGoToPreviousWeek);
            NextWeekCommand = new RelayCommand(ExecuteNextWeek, CanGoToNextWeek);
            ItemClickCommand = new RelayCommand(ExecuteItemClick, _ => true);
            SearchCommand = new RelayCommand(ExecuteSearch, _ => true);
            GoToCollectionCommand = new RelayCommand(ExecuteGoToCollection, _ => true);
            GoToArrearsCommand = new RelayCommand(ExecuteGoToArrears, _ => true);

            // تحديد الأسبوع الحالي تلقائياً
            int currentWeek = WeekHelper.GetCurrentWeekNumber();
            
            // التأكد من أن الأسبوع في النطاق الصحيح (1-26)
            if (currentWeek < 1)
                currentWeek = 1;
            else if (currentWeek > WeekHelper.TotalWeeks)
                currentWeek = WeekHelper.TotalWeeks;
                
            _currentWeek = currentWeek;
            OnPropertyChanged(nameof(CurrentWeek));

            LoadWeekSummary();
        }

        #region Properties

        public int CurrentWeek
        {
            get => _currentWeek;
            set
            {
                SetProperty(ref _currentWeek, value);
                OnPropertyChanged(nameof(PreviousWeek));
                LoadWeekSummary();
                ((RelayCommand)PreviousWeekCommand).RaiseCanExecuteChanged();
                ((RelayCommand)NextWeekCommand).RaiseCanExecuteChanged();
            }
        }

        public int PreviousWeek => CurrentWeek > 1 ? CurrentWeek - 1 : 0;

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public ObservableCollection<WeekSummaryItem> SummaryItems
        {
            get => _summaryItems;
            set => SetProperty(ref _summaryItems, value);
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
        
        public decimal PreviousBalance
        {
            get => _previousBalance;
            set => SetProperty(ref _previousBalance, value);
        }
        
        public decimal OtherTransactions
        {
            get => _otherTransactions;
            set => SetProperty(ref _otherTransactions, value);
        }
        
        public decimal ManagerWithdrawals
        {
            get => _managerWithdrawals;
            set => SetProperty(ref _managerWithdrawals, value);
        }
        
        public decimal AssociationDebts
        {
            get => _associationDebts;
            set => SetProperty(ref _associationDebts, value);
        }
        
        public decimal FinalBalance
        {
            get => _finalBalance;
            set => SetProperty(ref _finalBalance, value);
        }

        #endregion

        #region Commands

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand ItemClickCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand GoToCollectionCommand { get; }
        public ICommand GoToArrearsCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanGoToPreviousWeek(object parameter)
        {
            return CurrentWeek > 1;
        }

        private void ExecutePreviousWeek(object parameter)
        {
            if (CurrentWeek > 1)
            {
                CurrentWeek--;
            }
        }

        private bool CanGoToNextWeek(object parameter)
        {
            return CurrentWeek < WeekHelper.TotalWeeks;
        }

        private void ExecuteNextWeek(object parameter)
        {
            if (CurrentWeek < WeekHelper.TotalWeeks)
            {
                CurrentWeek++;
            }
        }

        private void ExecuteItemClick(object parameter)
        {
            if (parameter is WeekSummaryItem item)
            {
                // فتح صفحة التفاصيل اليومية
                ShowDailyDetails(item);
            }
        }

        private void ExecuteSearch(object parameter)
        {
            LoadWeekSummary();
        }

        private void ExecuteGoToCollection(object parameter)
        {
            try
            {
                // إنشاء واجهة التحصيل اليومي
                var collectionView = new Views.Collections.DailyCollectionView();

                // البحث عن Frame في النافذة الرئيسية
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // البحث عن Frame في شجرة العناصر المرئية
                    var frame = FindVisualChild<System.Windows.Controls.Frame>(mainWindow);
                    if (frame != null)
                    {
                        // التنقل إلى صفحة التحصيل اليومي
                        frame.Navigate(collectionView);
                    }
                    else
                    {
                        // إذا لم يوجد Frame، فتح في نافذة منفصلة
                        var window = new System.Windows.Window
                        {
                            Content = collectionView,
                            Title = "التحصيل اليومي",
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
                    $"❌ خطأ في فتح صفحة التحصيل: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void ExecuteGoToArrears(object parameter)
        {
            try
            {
                var arrearsView = new Views.Collections.ArrearsManagementView();
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var frame = FindVisualChild<System.Windows.Controls.Frame>(mainWindow);
                    if (frame != null)
                    {
                        frame.Navigate(arrearsView);
                    }
                    else
                    {
                        var window = new System.Windows.Window
                        {
                            Content = arrearsView,
                            Title = "المتأخرات والسابقات",
                            Width = 1400,
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
                    $"❌ خطأ في فتح صفحة المتأخرات: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        #endregion

        #region Helper Methods

        private void LoadWeekSummary()
        {
            try
            {
                // التأكد من أن الأسبوع في النطاق الصحيح
                if (CurrentWeek < 1 || CurrentWeek > WeekHelper.TotalWeeks)
                {
                    System.Windows.MessageBox.Show(
                        $"رقم الأسبوع ({CurrentWeek}) غير صحيح. يجب أن يكون بين 1 و {WeekHelper.TotalWeeks}",
                        "تنبيه",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    
                    // تعيين الأسبوع إلى 1 أو 26
                    CurrentWeek = CurrentWeek < 1 ? 1 : WeekHelper.TotalWeeks;
                    return;
                }
                
                SummaryItems.Clear();

                // جلب البيانات الفعلية من قاعدة البيانات
                var summary = _summaryService.GetWeekSummary(CurrentWeek);

                // حساب الإحصائيات الرئيسية
                TotalExpected = summary.TotalDues; // المتوقع = المستحقات فقط
                TotalCollected = summary.TotalIncome; // المحصل = الواردات
                TotalRemaining = summary.TotalDues - summary.TotalIncome; // المتبقي = المستحقات - المحصل
                
                // البيانات الإضافية
                PreviousBalance = summary.PreviousBalance; // السابقات المُراكمة
                OtherTransactions = summary.TotalOtherTransactions; // الخرجيات والمفقودات
                ManagerWithdrawals = summary.ManagerWithdrawals; // خرجيات المدير
                AssociationDebts = summary.AssociationDebts; // خلف الجمعية
                FinalBalance = summary.FinalBalance; // الرصيد النهائي

                // 1. واردات الأسبوع (التحصيلات)
                if (summary.TotalIncome > 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.Income,
                            Title = $"يومي {CurrentWeek}",
                            Amount = summary.TotalIncome,
                            MembersCount = summary.IncomeMembersCount,
                            AmountColor = new SolidColorBrush(Colors.Green),
                            WeekNumber = CurrentWeek,
                        }
                    );
                }

                // 2. ماعليكم (المتبقي = المستحقات - المحصل)
                decimal remaining = summary.TotalDues - summary.TotalIncome;
                if (remaining > 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.Dues,
                            Title = $"ماعليكم ل {CurrentWeek}",
                            Amount = remaining, // المتبقي وليس الإجمالي
                            MembersCount = summary.DuesMembersCount,
                            AmountColor = new SolidColorBrush(Colors.Orange),
                            WeekNumber = CurrentWeek,
                        }
                    );
                }

                // 3. متأخرات (الأيام الفائتة من الأسبوع الحالي)
                if (summary.TotalArrears > 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.Arrears,
                            Title = $"متأخرات {CurrentWeek}",
                            Amount = summary.TotalArrears,
                            MembersCount = summary.ArrearsMembersCount,
                            AmountColor = new SolidColorBrush(Colors.Red),
                            WeekNumber = CurrentWeek,
                        }
                    );
                }

                // 4. سابقات (المتراكم من جميع الأسابيع السابقة)
                if (CurrentWeek > 1 && summary.PreviousBalance != 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.PreviousBalance,
                            Title = $"سابقات (1-{CurrentWeek - 1})",
                            Amount = summary.PreviousBalance,
                            MembersCount = 0,
                            AmountColor =
                                summary.PreviousBalance >= 0
                                    ? new SolidColorBrush(Colors.Green)
                                    : new SolidColorBrush(Colors.Red),
                            WeekNumber = CurrentWeek - 1,
                        }
                    );
                }

                // 5. خرجيات المدير (مصروفات)
                if (summary.ManagerWithdrawals > 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.ManagerWithdrawals,
                            Title = "خرجيات وجيه",
                            Amount = summary.ManagerWithdrawals,
                            MembersCount = 0,
                            AmountColor = new SolidColorBrush(Colors.Orange),
                            WeekNumber = CurrentWeek,
                        }
                    );
                }

                // 6. خلف الجمعية (ديون)
                if (summary.AssociationDebts != 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.AssociationDebts,
                            Title = "خلف الجمعية",
                            Amount = summary.AssociationDebts,
                            MembersCount = 0,
                            AmountColor = new SolidColorBrush(Colors.Red),
                            WeekNumber = CurrentWeek,
                        }
                    );
                }

                // 7. مفقود
                if (summary.Missing > 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.Missing,
                            Title = "مفقود",
                            Amount = summary.Missing,
                            MembersCount = 0,
                            AmountColor = new SolidColorBrush(Colors.DarkOrange),
                            WeekNumber = CurrentWeek,
                        }
                    );
                }

                // 8. الخريجات
                if (summary.Graduates > 0)
                {
                    SummaryItems.Add(
                        new WeekSummaryItem
                        {
                            Type = WeekSummaryItemType.Graduates,
                            Title = "خريجات وجيه",
                            Amount = summary.Graduates,
                            MembersCount = summary.GraduatesCount,
                            AmountColor = new SolidColorBrush(Colors.Blue),
                            WeekNumber = CurrentWeek,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل ملخص الأسبوع: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void ShowDailyDetails(WeekSummaryItem item)
        {
            try
            {
                // إنشاء نافذة التفاصيل اليومية
                var detailsView = new Views.Collections.DailyDetailsView();
                var detailsViewModel = new DailyDetailsViewModel();

                // تحميل البيانات
                detailsViewModel.LoadData(item.Type, item.WeekNumber);

                // ربط ViewModel بالـ View
                detailsView.DataContext = detailsViewModel;

                // فتح النافذة
                var window = new System.Windows.Window
                {
                    Content = detailsView,
                    Title = $"تفاصيل {item.Title}",
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    FlowDirection = System.Windows.FlowDirection.RightToLeft,
                    FontFamily = new System.Windows.Media.FontFamily("Tajawal"),
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في فتح التفاصيل: {ex.Message}",
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
    }

    /// <summary>
    /// عنصر في ملخص الأسبوع
    /// </summary>
    public class WeekSummaryItem
    {
        public WeekSummaryItemType Type { get; set; }
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public int MembersCount { get; set; }
        public Brush AmountColor { get; set; }
        public int WeekNumber { get; set; }
    }
}
