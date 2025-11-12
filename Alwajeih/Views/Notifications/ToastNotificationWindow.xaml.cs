using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Alwajeih.Views.Notifications
{
    public partial class ToastNotificationWindow : Window
    {
        private DispatcherTimer _autoCloseTimer;
        private Action _primaryAction;
        private Action _secondaryAction;

        public ToastNotificationWindow()
        {
            InitializeComponent();
            SetWindowIcon();
        }

        private void SetWindowIcon()
        {
            try
            {
                var icon = Alwajeih.Utilities.IconGenerator.CreateAlwajeihIcon(256);
                this.Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تعيين أيقونة Toast: {ex.Message}");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // وضع النافذة في أعلى يمين الشاشة
            var workArea = SystemParameters.WorkArea;
            var targetLeft = workArea.Right - this.ActualWidth - 20;
            
            // البداية من خارج الشاشة (اليمين)
            this.Left = workArea.Right + 50;
            this.Top = 50;

            // تشغيل الرسوم المتحركة للانزلاق من اليمين
            var showAnimation = (Storyboard)this.Resources["ShowAnimation"];
            var leftAnimation = (DoubleAnimation)showAnimation.Children[1];
            leftAnimation.To = targetLeft;
            showAnimation.Begin(this);

            // إغلاق تلقائي بعد 5 ثوان
            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Interval = TimeSpan.FromSeconds(8);
            _autoCloseTimer.Tick += AutoClose_Tick;
            _autoCloseTimer.Start();
        }

        private void AutoClose_Tick(object sender, EventArgs e)
        {
            _autoCloseTimer.Stop();
            CloseWithAnimation();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            CloseWithAnimation();
        }

        private void PrimaryAction_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            _primaryAction?.Invoke();
            CloseWithAnimation();
        }

        private void SecondaryAction_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            _secondaryAction?.Invoke();
            CloseWithAnimation();
        }

        private void CloseWithAnimation()
        {
            var hideAnimation = (Storyboard)this.Resources["HideAnimation"];
            var leftAnimation = (DoubleAnimation)hideAnimation.Children[1];
            leftAnimation.To = SystemParameters.WorkArea.Right + 50; // الخروج من اليمين
            hideAnimation.Completed += (s, e) => this.Close();
            hideAnimation.Begin(this);
        }

        public void SetPrimaryAction(Action action)
        {
            _primaryAction = action;
        }

        public void SetSecondaryAction(Action action)
        {
            _secondaryAction = action;
        }

        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _autoCloseTimer?.Stop(); // إيقاف الإغلاق التلقائي عند مرور الماوس
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            // إعادة تشغيل العداد
            _autoCloseTimer?.Start();
        }
    }
}
