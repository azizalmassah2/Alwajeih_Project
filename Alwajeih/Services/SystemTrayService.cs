using System;
using System.Drawing;
using System.Windows;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة System Tray - تجعل التطبيق يعمل في الخلفية
    /// </summary>
    public class SystemTrayService : IDisposable
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool _disposed = false;

        public SystemTrayService()
        {
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = CreateCustomIcon(), // أيقونة مخصصة
                Text = "الوجيه - نظام الادخار",
                Visible = true
            };

            // إنشاء قائمة السياق
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            
            contextMenu.Items.Add("فتح التطبيق", null, OnOpenApp);
            contextMenu.Items.Add("-"); // فاصل
            contextMenu.Items.Add("عرض المتأخرات", null, OnShowArrears);
            contextMenu.Items.Add("الجرد الأسبوعي", null, OnShowReconciliation);
            contextMenu.Items.Add("-"); // فاصل
            contextMenu.Items.Add("إنشاء متأخرات اليوم", null, OnCreateArrears);
            contextMenu.Items.Add("-"); // فاصل
            
            // خيار بدء التشغيل التلقائي
            var startupItem = new System.Windows.Forms.ToolStripMenuItem("تشغيل مع Windows");
            startupItem.Checked = StartupService.IsStartupEnabled();
            startupItem.CheckOnClick = true;
            startupItem.Click += OnToggleStartup;
            contextMenu.Items.Add(startupItem);
            
            contextMenu.Items.Add("الإعدادات", null, OnShowSettings);
            contextMenu.Items.Add("-"); // فاصل
            contextMenu.Items.Add("خروج", null, OnExit);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => OnOpenApp(s, e);

            // إظهار إشعار بأن التطبيق يعمل في الخلفية
            _notifyIcon.BalloonTipTitle = "الوجيه - نظام الادخار";
            _notifyIcon.BalloonTipText = "التطبيق يعمل في الخلفية. انقر نقراً مزدوجاً للفتح.";
            _notifyIcon.ShowBalloonTip(3000);
        }

        private void OnOpenApp(object sender, EventArgs e)
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Application.Current is null");
                    return;
                }

                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        if (mainWindow != null)
                        {
                            // إظهار في شريط المهام
                            mainWindow.ShowInTaskbar = true;
                            
                            // استعادة الحالة العادية إذا كانت مصغرة
                            if (mainWindow.WindowState == WindowState.Minimized)
                            {
                                mainWindow.WindowState = WindowState.Normal;
                            }
                            
                            // إظهار وتفعيل النافذة
                            mainWindow.Show();
                            mainWindow.Activate();
                            mainWindow.Focus();
                            
                            // جلب النافذة للمقدمة
                            if (mainWindow.WindowState == WindowState.Normal)
                            {
                                mainWindow.Topmost = true;
                                mainWindow.Topmost = false;
                            }
                            
                            System.Diagnostics.Debug.WriteLine("✅ تم فتح النافذة الرئيسية");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("❌ MainWindow is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في فتح النافذة: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في OnOpenApp: {ex.Message}");
            }
        }

        private void OnShowArrears(object sender, EventArgs e)
        {
            try
            {
                if (System.Windows.Application.Current == null) return;

                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.ShowInTaskbar = true;
                            
                            if (mainWindow.WindowState == WindowState.Minimized)
                                mainWindow.WindowState = WindowState.Normal;
                            
                            mainWindow.Show();
                            mainWindow.Activate();
                            mainWindow.Focus();
                            
                            // جلب للمقدمة
                            mainWindow.Topmost = true;
                            mainWindow.Topmost = false;
                            
                            // التنقل إلى صفحة المتأخرات
                            if (mainWindow is MainWindow mw)
                            {
                                mw.NavigateTo("ArrearsManagement");
                            }
                            
                            System.Diagnostics.Debug.WriteLine("✅ تم فتح صفحة المتأخرات");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في فتح المتأخرات: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في OnShowArrears: {ex.Message}");
            }
        }

        private void OnShowReconciliation(object sender, EventArgs e)
        {
            try
            {
                if (System.Windows.Application.Current == null) return;

                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.ShowInTaskbar = true;
                            
                            if (mainWindow.WindowState == WindowState.Minimized)
                                mainWindow.WindowState = WindowState.Normal;
                            
                            mainWindow.Show();
                            mainWindow.Activate();
                            mainWindow.Focus();
                            
                            mainWindow.Topmost = true;
                            mainWindow.Topmost = false;
                            
                            // التنقل إلى صفحة الجرد
                            if (mainWindow is MainWindow mw)
                            {
                                mw.NavigateTo("Reconciliation");
                            }
                            
                            System.Diagnostics.Debug.WriteLine("✅ تم فتح صفحة الجرد");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في فتح الجرد: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في OnShowReconciliation: {ex.Message}");
            }
        }

        private void OnCreateArrears(object sender, EventArgs e)
        {
            try
            {
                if (System.Windows.Application.Current == null) return;

                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var arrearService = new ArrearService();
                        var (success, message, count) = arrearService.CreateMissingDailyArrears(DateTime.Now);
                        
                        if (success)
                        {
                            ToastNotificationService.ShowArrearsCreatedNotification(count, DateTime.Now);
                            System.Diagnostics.Debug.WriteLine($"✅ تم إنشاء {count} متأخرة");
                        }
                        else
                        {
                            ToastNotificationService.ShowWarningNotification("خطأ", message);
                        }
                    }
                    catch (Exception ex)
                    {
                        ToastNotificationService.ShowWarningNotification("خطأ", $"فشل إنشاء المتأخرات: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في إنشاء المتأخرات: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في OnCreateArrears: {ex.Message}");
            }
        }

        private void OnShowSettings(object sender, EventArgs e)
        {
            try
            {
                if (System.Windows.Application.Current == null) return;

                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.ShowInTaskbar = true;
                            
                            if (mainWindow.WindowState == WindowState.Minimized)
                                mainWindow.WindowState = WindowState.Normal;
                            
                            mainWindow.Show();
                            mainWindow.Activate();
                            mainWindow.Focus();
                            
                            mainWindow.Topmost = true;
                            mainWindow.Topmost = false;
                            
                            // التنقل إلى صفحة الإعدادات
                            if (mainWindow is MainWindow mw)
                            {
                                mw.NavigateTo("Settings");
                            }
                            
                            System.Diagnostics.Debug.WriteLine("✅ تم فتح صفحة الإعدادات");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في فتح الإعدادات: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في OnShowSettings: {ex.Message}");
            }
        }

        private void OnToggleStartup(object sender, EventArgs e)
        {
            try
            {
                bool result = StartupService.ToggleStartup();
                string status = StartupService.IsStartupEnabled() ? "مُفعّل" : "مُعطّل";
                System.Diagnostics.Debug.WriteLine($"✅ بدء التشغيل التلقائي: {status}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تبديل بدء التشغيل: {ex.Message}");
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                if (System.Windows.Application.Current == null) return;

                var result = System.Windows.MessageBox.Show(
                    "هل أنت متأكد من الخروج من التطبيق؟",
                    "تأكيد الخروج",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("✅ إغلاق التطبيق");
                        System.Windows.Application.Current.Shutdown();
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في OnExit: {ex.Message}");
            }
        }

        /// <summary>
        /// إنشاء أيقونة مخصصة للتطبيق
        /// </summary>
        private Icon CreateCustomIcon()
        {
            try
            {
                // استخدام مولد الأيقونات
                return Utilities.IconGenerator.CreateAlwajeihIcon(32);
            }
            catch
            {
                // في حالة الفشل، استخدام الأيقونة الافتراضية
                return SystemIcons.Application;
            }
        }


        /// <summary>
        /// إظهار إشعار Balloon
        /// </summary>
        public void ShowBalloonTip(string title, string text, System.Windows.Forms.ToolTipIcon icon = System.Windows.Forms.ToolTipIcon.Info, int timeout = 3000)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = text;
                _notifyIcon.BalloonTipIcon = icon;
                _notifyIcon.ShowBalloonTip(timeout);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _notifyIcon?.Dispose();
                _disposed = true;
            }
        }
    }
}
