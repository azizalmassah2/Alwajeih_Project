using System.Threading;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة جدولة إنشاء المتأخرات اليومية تلقائياً
    /// تعمل في الخلفية وتُنشئ المتأخرات قبل نهاية كل يوم بـ 10 دقائق
    /// </summary>
    public class DailyArrearSchedulerService : IDisposable
    {
        private ThreadingTimer _timer;
        private readonly ArrearService _arrearService;
        private readonly AuditRepository _auditRepository;
        private bool _disposed = false;
        private DateTime _lastExecutionDate = DateTime.MinValue;

        // وقت التنفيذ: قبل منتصف الليل بـ 10 دقائق (11:50 مساءً)
        private readonly TimeSpan _executionTime = new TimeSpan(23, 50, 0);

        public DailyArrearSchedulerService()
        {
            _arrearService = new ArrearService();
            _auditRepository = new AuditRepository();
        }

        /// <summary>
        /// بدء خدمة الجدولة
        /// </summary>
        public void Start()
        {
            // حساب الوقت المتبقي حتى أول تنفيذ
            TimeSpan initialDelay = CalculateNextExecutionDelay();
            
            System.Diagnostics.Debug.WriteLine($"🕐 خدمة جدولة المتأخرات: بدء العمل");
            System.Diagnostics.Debug.WriteLine($"⏰ أول تنفيذ بعد: {initialDelay.TotalMinutes:F0} دقيقة");

            // إنشاء Timer يعمل كل دقيقة للفحص
            _timer = new ThreadingTimer(
                callback: CheckAndExecute,
                state: null,
                dueTime: TimeSpan.FromMinutes(1), // البدء بعد دقيقة
                period: TimeSpan.FromMinutes(1)   // التكرار كل دقيقة
            );
        }

        /// <summary>
        /// إيقاف خدمة الجدولة
        /// </summary>
        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            System.Diagnostics.Debug.WriteLine($"🛑 خدمة جدولة المتأخرات: توقف العمل");
        }

        /// <summary>
        /// حساب الوقت المتبقي حتى التنفيذ التالي
        /// </summary>
        private TimeSpan CalculateNextExecutionDelay()
        {
            DateTime now = DateTime.Now;
            DateTime nextExecution = now.Date.Add(_executionTime);

            // إذا مر وقت التنفيذ اليوم، جدول للغد
            if (now > nextExecution)
            {
                nextExecution = nextExecution.AddDays(1);
            }

            return nextExecution - now;
        }

        /// <summary>
        /// فحص الوقت وتنفيذ إنشاء المتأخرات إذا حان الوقت
        /// </summary>
        private async void CheckAndExecute(object state)
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime todayExecutionTime = now.Date.Add(_executionTime);

                // التحقق من أننا في نافذة التنفيذ (بين 11:50 و 11:59)
                bool isExecutionTime = now >= todayExecutionTime && 
                                      now < todayExecutionTime.AddMinutes(10);

                // التحقق من أن التنفيذ لم يحدث اليوم
                bool alreadyExecutedToday = _lastExecutionDate.Date == now.Date;

                if (isExecutionTime && !alreadyExecutedToday)
                {
                    System.Diagnostics.Debug.WriteLine($"⏰ {now:HH:mm:ss} - بدء إنشاء متأخرات اليوم تلقائياً...");
                    
                    await Task.Run(() => ExecuteCreateArrears(now.Date));
                    
                    _lastExecutionDate = now.Date;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في خدمة جدولة المتأخرات: {ex.Message}");
                
                // تسجيل الخطأ
                try
                {
                    _auditRepository.Add(new Models.AuditLog
                    {
                        UserID = 1, // System
                        Action = Models.AuditAction.Create,
                        EntityType = Models.EntityType.DailyArrear,
                        Details = $"خطأ في جدولة المتأخرات التلقائية: {ex.Message}",
                        Reason = "Auto-Scheduler Error"
                    });
                }
                catch { }
            }
        }

        /// <summary>
        /// تنفيذ إنشاء المتأخرات
        /// </summary>
        private void ExecuteCreateArrears(DateTime date)
        {
            try
            {
                var (success, message, arrearsCreated) = _arrearService.CreateMissingDailyArrears(date);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ تم إنشاء {arrearsCreated} متأخرة تلقائياً لتاريخ {date:dd/MM/yyyy}");
                    
                    // إرسال إشعار Toast
                    ToastNotificationService.ShowArrearsCreatedNotification(arrearsCreated, date);
                    
                    // تسجيل في Audit
                    _auditRepository.Add(new Models.AuditLog
                    {
                        UserID = 1, // System
                        Action = Models.AuditAction.Create,
                        EntityType = Models.EntityType.DailyArrear,
                        Details = $"إنشاء {arrearsCreated} متأخرة تلقائياً لتاريخ {date:dd/MM/yyyy}",
                        Reason = "Auto-Scheduler"
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ فشل إنشاء المتأخرات التلقائي: {message}");
                    
                    // إرسال إشعار تحذير
                    if (arrearsCreated == 0)
                    {
                        ToastNotificationService.ShowWarningNotification("تنبيه", message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إنشاء المتأخرات: {ex.Message}");
            }
        }

        /// <summary>
        /// إنشاء متأخرات يدوياً (للاختبار)
        /// </summary>
        public (bool Success, string Message, int ArrearsCreated) ExecuteManually()
        {
            try
            {
                DateTime today = DateTime.Now.Date;
                return _arrearService.CreateMissingDailyArrears(today);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}", 0);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _timer?.Dispose();
                _disposed = true;
            }
        }
    }
}
