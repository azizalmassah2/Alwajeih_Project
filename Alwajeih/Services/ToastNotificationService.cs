using System;
using System.Windows;
using System.Windows.Media;
using Alwajeih.ViewModels.Notifications;
using Alwajeih.Views.Notifications;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة إشعارات Toast مخصصة
    /// </summary>
    public class ToastNotificationService
    {
        /// <summary>
        /// إرسال إشعار بسيط
        /// </summary>
        public static void ShowSimpleNotification(string title, string message)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "ℹ️",
                        Title = title,
                        Message = message,
                        HeaderColor = new SolidColorBrush(Color.FromRgb(59, 130, 246)) // Blue
                    };

                    var window = new ToastNotificationWindow
                    {
                        DataContext = viewModel
                    };
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال الإشعار: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار إنشاء المتأخرات
        /// </summary>
        public static void ShowArrearsCreatedNotification(int count, DateTime date)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "⚠️",
                        Title = "تم إنشاء متأخرات جديدة",
                        Message = $"تم إنشاء {count} متأخرة تلقائياً",
                        SubMessage = $"التاريخ: {date:dd/MM/yyyy}",
                        HeaderColor = new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Orange
                        PrimaryActionText = "عرض المتأخرات",
                        SecondaryActionText = "إغلاق"
                    };

                    var window = new ToastNotificationWindow
                    {
                        DataContext = viewModel
                    };
                    
                    window.SetPrimaryAction(() =>
                    {
                        // فتح صفحة المتأخرات
                        ShowMainWindow();
                    });
                    
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال إشعار المتأخرات: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار تذكير نهاية اليوم
        /// </summary>
        public static void ShowEndOfDayReminder(int pendingMembers, decimal totalDue)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "⏰",
                        Title = "تذكير نهاية اليوم",
                        Message = $"عدد الأعضاء الذين لم يدفعوا: {pendingMembers}",
                        SubMessage = $"المبلغ المتبقي: {totalDue:N2} ريال",
                        HeaderColor = new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red
                        PrimaryActionText = "فتح التطبيق",
                        SecondaryActionText = "تذكير لاحقاً"
                    };

                    var window = new ToastNotificationWindow { DataContext = viewModel };
                    window.SetPrimaryAction(() => ShowMainWindow());
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال تذكير نهاية اليوم: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار تذكير نهاية الأسبوع
        /// </summary>
        public static void ShowEndOfWeekReminder(int weekNumber)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "📊",
                        Title = "تذكير نهاية الأسبوع",
                        Message = $"حان وقت إجراء الجرد الأسبوعي للأسبوع {weekNumber}",
                        SubMessage = "يجب إنهاء الأسبوع وترحيل المتأخرات إلى سابقات",
                        HeaderColor = new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Purple
                        PrimaryActionText = "فتح الجرد",
                        SecondaryActionText = "تذكير غداً"
                    };

                    var window = new ToastNotificationWindow { DataContext = viewModel };
                    window.SetPrimaryAction(() => ShowMainWindow());
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال تذكير نهاية الأسبوع: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار دفع كبير
        /// </summary>
        public static void ShowLargePaymentNotification(string memberName, decimal amount)
        {
            try
            {
                ShowSuccessNotification("💰 دفعة كبيرة", $"{memberName} قام بدفع {amount:N2} ريال");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال إشعار الدفع: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار خطأ أو تحذير
        /// </summary>
        public static void ShowWarningNotification(string title, string message)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "⚠️",
                        Title = title,
                        Message = message,
                        HeaderColor = new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Orange
                        PrimaryActionText = "حسناً"
                    };

                    var window = new ToastNotificationWindow { DataContext = viewModel };
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال تحذير: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار تذكير دفع
        /// </summary>
        public static void ShowPaymentReminderNotification(int overdueMembers)
        {
            try
            {
                ShowWarningNotification("📢 تذكير بالمتأخرات", $"لديك {overdueMembers} عضو متأخر في الدفع");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال تذكير: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار نجاح العملية
        /// </summary>
        public static void ShowSuccessNotification(string title, string message)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "✅",
                        Title = title,
                        Message = message,
                        HeaderColor = new SolidColorBrush(Color.FromRgb(16, 185, 129)) // Green
                    };

                    var window = new ToastNotificationWindow { DataContext = viewModel };
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال إشعار النجاح: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار نجاح العملية (بعنوان افتراضي)
        /// </summary>
        public static void ShowSuccessNotification(string message)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "✅",
                        Title = "نجاح",
                        Message = message,
                        HeaderColor = new SolidColorBrush(Color.FromRgb(16, 185, 129)) // Green
                    };

                    var window = new ToastNotificationWindow { DataContext = viewModel };
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال إشعار النجاح: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار مخصص مع صورة
        /// </summary>
        public static void ShowCustomNotification(string title, string message, string heroImagePath = null)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "💡",
                        Title = title,
                        Message = message,
                        HeaderColor = new SolidColorBrush(Color.FromRgb(59, 130, 246)) // Blue
                    };

                    var window = new ToastNotificationWindow { DataContext = viewModel };
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال الإشعار المخصص: {ex.Message}");
            }
        }

        /// <summary>
        /// إشعار تذكير للأعضاء الأسبوعيين
        /// </summary>
        public static void ShowWeeklyMembersReminder(int weeklyMembersCount, string dayName)
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new ToastNotificationViewModel
                    {
                        Icon = "📅",
                        Title = "تذكير الأعضاء الأسبوعيين",
                        Message = $"اليوم {dayName} - موعد دفع الأعضاء الأسبوعيين",
                        SubMessage = $"عدد الأعضاء: {weeklyMembersCount}",
                        HeaderColor = new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Purple
                        PrimaryActionText = "فتح التطبيق",
                        SecondaryActionText = "حسناً"
                    };

                    var window = new ToastNotificationWindow { DataContext = viewModel };
                    window.SetPrimaryAction(() => ShowMainWindow());
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إرسال تذكير الأعضاء الأسبوعيين: {ex.Message}");
            }
        }

        /// <summary>
        /// إظهار النافذة الرئيسية
        /// </summary>
        private static void ShowMainWindow()
        {
            try
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = WpfApplication.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Show();
                        mainWindow.WindowState = WindowState.Normal;
                        mainWindow.Activate();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إظهار النافذة: {ex.Message}");
            }
        }
    }
}
