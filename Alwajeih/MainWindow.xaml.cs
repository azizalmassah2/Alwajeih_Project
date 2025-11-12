using System;
using System.Windows;
using System.Windows.Controls;
using Alwajeih.Services;
using Alwajeih.Views;
using Alwajeih.Views.Authentication;
using Alwajeih.Views.Collections;
using Alwajeih.Views.Dashboard;
using Alwajeih.Views.Finance;
using Alwajeih.Views.Management;
using Alwajeih.Views.Members;
using Alwajeih.Views.Reports;
using Alwajeih.Views.Receipts;
using Alwajeih.Views.SavingPlans;

namespace Alwajeih
{
    /// <summary>
    /// النافذة الرئيسية للتطبيق
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly DailyArrearSchedulerService _arrearScheduler;
        private readonly ReminderSchedulerService _reminderScheduler;
        private readonly SystemTrayService _systemTrayService;
        private bool _isRealClose = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // تعيين أيقونة النافذة
            SetWindowIcon();
            
            _authService = AuthenticationService.Instance;
            LoadUserInfo();

            // بدء خدمة جدولة المتأخرات التلقائية
            _arrearScheduler = new DailyArrearSchedulerService();
            _arrearScheduler.Start();

            // بدء خدمة التذكيرات اليومية والأسبوعية
            _reminderScheduler = new ReminderSchedulerService();
            _reminderScheduler.Start();

            // تفعيل System Tray
            _systemTrayService = new SystemTrayService();

            // التعامل مع حدث إغلاق النافذة (إخفاء بدلاً من إغلاق)
            this.Closing += MainWindow_Closing;

            // تفعيل بدء التشغيل التلقائي (إذا لم يكن مفعلاً)
            if (!StartupService.IsStartupEnabled())
            {
                StartupService.EnableStartup();
            }

            // التحقق من معاملات سطر الأوامر (البدء المصغر)
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "-minimized")
            {
                // بدء مصغر - إخفاء النافذة
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Loaded += (s, e) => this.Hide();
            }
            else
            {
                // عرض لوحة التحكم افتراضياً
                NavigateTo("Dashboard");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // إذا لم يكن إغلاق حقيقي، قم بإخفاء النافذة فقط
            if (!_isRealClose)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
                
                _systemTrayService?.ShowBalloonTip(
                    "الوجيه - نظام الادخار",
                    "التطبيق يعمل في الخلفية. انقر نقراً مزدوجاً على الأيقونة لإظهاره مرة أخرى.");
                
                System.Diagnostics.Debug.WriteLine("✅ تم إخفاء النافذة - التطبيق يعمل في الخلفية");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✅ إغلاق حقيقي - إيقاف الخدمات");
            }
        }

        private void LoadUserInfo()
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser != null)
            {
                UserNameTextBlock.Text = currentUser.Username;
                UserRoleTextBlock.Text = GetRoleDisplayName(currentUser.Role);
            }
        }

        private string GetRoleDisplayName(Models.UserRole role)
        {
            return role switch
            {
                Models.UserRole.Manager => "مدير",
                Models.UserRole.Cashier => "أمين صندوق",
                Models.UserRole.Viewer => "مشاهد",
                _ => role.ToString(),
            };
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                // تحديث حالة الأزرار (تفعيل الزر المضغوط وإلغاء تفعيل الباقي)
                UpdateMenuButtonsState(button);

                NavigateTo(tag);
            }
        }

        private void UpdateMenuButtonsState(Button activeButton)
        {
            var menuButtons = new[]
            {
                DashboardButton,
                MembersButton,
                PlansButton,
                CollectionButton,
                ArrearsManagementButton,
                ReconciliationButton,
                VaultButton,
                ReportsButton,
                ExternalPaymentButton,
                ReceiptButton,
                BehindAssociationButton,
                SettingsButton,
                ArchiveButton
            };

            // إعادة تعيين جميع الأزرار إلى النمط العادي
            foreach (var button in menuButtons)
            {
                button.Style = (Style)FindResource("SideMenuButton");
            }

            // تطبيق النمط النشط على الزر المضغوط
            activeButton.Style = (Style)FindResource("SideMenuButtonActive");
        }

        public void NavigateTo(string pageName)
        {
            switch (pageName)
            {
                case "Dashboard":
                    MainContentFrame.Navigate(new DashboardView());
                    break;
                case "Members":
                    MainContentFrame.Navigate(new MemberManagementView());
                    break;
                case "Plans":
                    MainContentFrame.Navigate(new SavingPlanView());
                    break;
                case "Collection":
                    MainContentFrame.Navigate(new DailyCollectionView());
                    break;
                case "ArrearsManagement":
                    MainContentFrame.Navigate(new Views.Collections.ArrearsManagementView());
                    break;
                case "Vault":
                    MainContentFrame.Navigate(new VaultView());
                    break;
                case "Reconciliation":
                    MainContentFrame.Navigate(new ReconciliationView());
                    break;
                case "Reports":
                    MainContentFrame.Navigate(new ReportView());
                    break;
                case "ExternalPayment":
                    MainContentFrame.Navigate(new ExternalPaymentView());
                    break;
                case "Receipt":
                    MainContentFrame.Navigate(new ReceiptView());
                    break;
                case "BehindAssociation":
                    MainContentFrame.Navigate(new Views.BehindAssociation.BehindAssociationView());
                    break;
                case "Settings":
                    MainContentFrame.Navigate(new SettingsView());
                    break;
                case "Archive":
                    MainContentFrame.Navigate(new ArchiveView());
                    break;
                default:
                    MainContentFrame.Navigate(new DashboardView());
                    break;
            }
        }

        private void ShowPlaceholder(string title)
        {
            var textBlock = new TextBlock
            {
                Text = $"صفحة {title}\n\nقيد التطوير...",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };

            MainContentFrame.Content = textBlock;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "هل أنت متأكد من تسجيل الخروج؟",
                "تسجيل الخروج",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();

                var loginWindow = new LoginWindow();
                loginWindow.Show();

                _isRealClose = true; // السماح بالإغلاق الحقيقي
                this.Close();
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // إيقاف جميع الخدمات عند الإغلاق النهائي
            _arrearScheduler?.Stop();
            _arrearScheduler?.Dispose();
            _reminderScheduler?.Stop();
            _reminderScheduler?.Dispose();
            _systemTrayService?.Dispose();
            
            base.OnClosed(e);
        }
        
        /// <summary>
        /// إظهار النافذة من System Tray
        /// </summary>
        public void ShowFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        /// <summary>
        /// تعيين أيقونة النافذة
        /// </summary>
        private void SetWindowIcon()
        {
            try
            {
                var icon = Utilities.IconGenerator.CreateAlwajeihIcon(256);
                this.Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تعيين أيقونة النافذة: {ex.Message}");
            }
        }
    }
}
