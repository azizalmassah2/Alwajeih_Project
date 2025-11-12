using System.Windows;
using Alwajeih.Views;
using Alwajeih.Views.Authentication;

namespace Alwajeih
{
    /// <summary>
    /// تطبيق الوجيه - نظام إدارة الادخار
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // عرض شاشة Splash Screen
            var splashScreen = new SplashScreenWindow();
            splashScreen.Show();
            // var mainWindow = new MainWindow();
            // mainWindow.Show();
        }
    }
}
