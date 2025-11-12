using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Alwajeih.Data;

namespace Alwajeih.Views.Authentication
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            Loaded += SplashScreenWindow_Loaded;
        }

        private async void SplashScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // بدء الأنيميشن
            var storyboard = (Storyboard)FindResource("LoadingAnimation");
            storyboard.Begin();

            // تهيئة قاعدة البيانات
            await Task.Run(() =>
            {
                try
                {
                    DatabaseInitializer.Initialize();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"خطأ في تهيئة قاعدة البيانات: {ex.Message}",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            });

            // الانتظار لإكمال الأنيميشن
            await Task.Delay(2500);

            // فتح نافذة تسجيل الدخول
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // إغلاق Splash Screen
            this.Close();
        }
    }
}
