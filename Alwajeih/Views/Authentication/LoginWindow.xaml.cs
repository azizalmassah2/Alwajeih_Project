using System;
using System.Windows;
using System.Windows.Controls;
using Alwajeih.ViewModels.Authentication;

namespace Alwajeih.Views.Authentication
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            SetWindowIcon();
            UsernameTextBox.Focus();
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
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تعيين أيقونة النافذة: {ex.Message}");
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
