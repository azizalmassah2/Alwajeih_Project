using System;
using System.Linq;
using System.Threading;
using Alwajeih.Data.Repositories;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة جدولة التذكيرات اليومية والأسبوعية
    /// </summary>
    public class ReminderSchedulerService : IDisposable
    {
        private System.Threading.Timer _timer;
        private readonly DailyCollectionRepository _collectionRepository;
        private readonly SavingPlanRepository _planRepository;
        private readonly ReconciliationRepository _reconciliationRepository;
        private readonly SystemSettingsRepository _settingsRepository;
        private bool _disposed = false;
        
        // أوقات التذكيرات
        private readonly TimeSpan _morningReminderTime = new TimeSpan(9, 0, 0);  // 9:00 صباحاً
        private readonly TimeSpan _afternoonReminderTime = new TimeSpan(15, 0, 0); // 3:00 عصراً
        private readonly TimeSpan _endOfDayReminderTime = new TimeSpan(20, 0, 0); // 8:00 مساءً
        private readonly TimeSpan _endOfWeekReminderTime = new TimeSpan(18, 0, 0); // 6:00 مساءً (الخميس)

        private DateTime _lastMorningReminder = DateTime.MinValue;
        private DateTime _lastAfternoonReminder = DateTime.MinValue;
        private DateTime _lastEndOfDayReminder = DateTime.MinValue;
        private DateTime _lastEndOfWeekReminder = DateTime.MinValue;

        public ReminderSchedulerService()
        {
            _collectionRepository = new DailyCollectionRepository();
            _planRepository = new SavingPlanRepository();
            _reconciliationRepository = new ReconciliationRepository();
            _settingsRepository = new SystemSettingsRepository();
        }

        /// <summary>
        /// بدء خدمة التذكيرات
        /// </summary>
        public void Start()
        {
            System.Diagnostics.Debug.WriteLine($"📢 خدمة التذكيرات: بدء العمل");

            // فحص كل 5 دقائق
            _timer = new System.Threading.Timer(
                callback: CheckAndSendReminders,
                state: null,
                dueTime: TimeSpan.FromMinutes(1),
                period: TimeSpan.FromMinutes(5)
            );
        }

        /// <summary>
        /// إيقاف خدمة التذكيرات
        /// </summary>
        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            System.Diagnostics.Debug.WriteLine($"🛑 خدمة التذكيرات: توقف العمل");
        }

        /// <summary>
        /// فحص وإرسال التذكيرات
        /// </summary>
        private void CheckAndSendReminders(object state)
        {
            try
            {
                DateTime now = DateTime.Now;
                
                // تحميل الإعدادات
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    WeekHelper.StartDate = settings.StartDate;
                }

                // تذكير الصباح (9:00 ص)
                if (ShouldSendReminder(now, _morningReminderTime, ref _lastMorningReminder))
                {
                    SendMorningReminder();
                    
                    // تذكير الأعضاء الأسبوعيين
                    SendWeeklyMembersReminder(now);
                }

                // تذكير بعد الظهر (3:00 م)
                if (ShouldSendReminder(now, _afternoonReminderTime, ref _lastAfternoonReminder))
                {
                    SendAfternoonReminder();
                }

                // تذكير نهاية اليوم (8:00 م)
                if (ShouldSendReminder(now, _endOfDayReminderTime, ref _lastEndOfDayReminder))
                {
                    SendEndOfDayReminder();
                }

                // تذكير نهاية الأسبوع (الخميس 6:00 م)
                if (now.DayOfWeek == DayOfWeek.Thursday && 
                    ShouldSendReminder(now, _endOfWeekReminderTime, ref _lastEndOfWeekReminder))
                {
                    SendEndOfWeekReminder();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في خدمة التذكيرات: {ex.Message}");
            }
        }

        /// <summary>
        /// التحقق من وقت إرسال التذكير
        /// </summary>
        private bool ShouldSendReminder(DateTime now, TimeSpan targetTime, ref DateTime lastReminderDate)
        {
            DateTime todayTarget = now.Date.Add(targetTime);
            
            // في نافذة التنفيذ (10 دقائق)
            bool isTimeWindow = now >= todayTarget && now < todayTarget.AddMinutes(10);
            
            // لم يتم الإرسال اليوم
            bool notSentToday = lastReminderDate.Date != now.Date;
            
            return isTimeWindow && notSentToday;
        }

        /// <summary>
        /// تذكير الصباح - بداية اليوم
        /// </summary>
        private void SendMorningReminder()
        {
            try
            {
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                int currentDay = WeekHelper.GetCurrentDayNumber();

                // عدد الأعضاء النشطين
                var activePlans = _planRepository.GetActive().Count();
                
                ToastNotificationService.ShowCustomNotification(
                    "☀️ صباح الخير - بداية يوم جديد",
                    $"📅 الأسبوع {currentWeek} - اليوم {currentDay}\n" +
                    $"👥 عدد الأعضاء النشطين: {activePlans}\n" +
                    $"💼 جاهز لبدء التحصيل اليومي"
                );

                _lastMorningReminder = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"📢 تم إرسال تذكير الصباح");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تذكير الصباح: {ex.Message}");
            }
        }

        /// <summary>
        /// تذكير بعد الظهر - منتصف اليوم
        /// </summary>
        private void SendAfternoonReminder()
        {
            try
            {
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                int currentDay = WeekHelper.GetCurrentDayNumber();

                // حساب من لم يدفع بعد
                var activePlans = _planRepository.GetActive().ToList();
                int paidCount = 0;
                decimal totalCollected = 0;

                foreach (var plan in activePlans)
                {
                    var payment = _collectionRepository.GetByPlanWeekDay(plan.PlanID, currentWeek, currentDay);
                    if (payment != null && payment.AmountPaid >= plan.DailyAmount)
                    {
                        paidCount++;
                        totalCollected += payment.AmountPaid;
                    }
                }

                int pendingCount = activePlans.Count - paidCount;

                if (pendingCount > 0)
                {
                    ToastNotificationService.ShowCustomNotification(
                        "⏰ تذكير منتصف اليوم",
                        $"✅ تم التحصيل من {paidCount} عضو ({totalCollected:N2} ريال)\n" +
                        $"⏳ المتبقي: {pendingCount} عضو\n" +
                        $"💡 تذكير: يُفضل إنهاء التحصيل قبل المساء"
                    );
                }

                _lastAfternoonReminder = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"📢 تم إرسال تذكير بعد الظهر");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تذكير بعد الظهر: {ex.Message}");
            }
        }

        /// <summary>
        /// تذكير نهاية اليوم
        /// </summary>
        private void SendEndOfDayReminder()
        {
            try
            {
                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                int currentDay = WeekHelper.GetCurrentDayNumber();

                // حساب من لم يدفع
                var activePlans = _planRepository.GetActive().ToList();
                int pendingCount = 0;
                decimal totalDue = 0;

                foreach (var plan in activePlans)
                {
                    var payment = _collectionRepository.GetByPlanWeekDay(plan.PlanID, currentWeek, currentDay);
                    if (payment == null || payment.AmountPaid < plan.DailyAmount)
                    {
                        pendingCount++;
                        totalDue += plan.DailyAmount - (payment?.AmountPaid ?? 0);
                    }
                }

                if (pendingCount > 0)
                {
                    ToastNotificationService.ShowEndOfDayReminder(pendingCount, totalDue);
                }
                else
                {
                    ToastNotificationService.ShowSuccessNotification(
                        "✅ تم إنهاء تحصيل اليوم بنجاح!\nجميع الأعضاء قاموا بالدفع 🎉"
                    );
                }

                _lastEndOfDayReminder = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"📢 تم إرسال تذكير نهاية اليوم");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تذكير نهاية اليوم: {ex.Message}");
            }
        }

        /// <summary>
        /// تذكير نهاية الأسبوع (الخميس)
        /// </summary>
        private void SendEndOfWeekReminder()
        {
            try
            {
                int currentWeek = WeekHelper.GetCurrentWeekNumber();

                // التحقق من وجود جرد للأسبوع
                var reconciliation = _reconciliationRepository.GetByWeekNumber(currentWeek);

                if (reconciliation == null)
                {
                    ToastNotificationService.ShowEndOfWeekReminder(currentWeek);
                }
                else
                {
                    ToastNotificationService.ShowSuccessNotification(
                        $"✅ تم إجراء الجرد للأسبوع {currentWeek} مسبقاً"
                    );
                }

                _lastEndOfWeekReminder = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"📢 تم إرسال تذكير نهاية الأسبوع");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تذكير نهاية الأسبوع: {ex.Message}");
            }
        }

        /// <summary>
        /// تذكير الأعضاء الأسبوعيين
        /// </summary>
        private void SendWeeklyMembersReminder(DateTime now)
        {
            try
            {
                int currentDayOfWeek = (int)now.DayOfWeek;
                if (currentDayOfWeek == 0) currentDayOfWeek = 7; // تحويل الأحد من 0 إلى 7

                // الحصول على الأعضاء الأسبوعيين الذين موعدهم اليوم
                var weeklyPlans = _planRepository.GetActive()
                    .Where(p => p.CollectionFrequency == Models.CollectionFrequency.Weekly && 
                               p.PreferredPaymentDay == currentDayOfWeek)
                    .ToList();

                if (weeklyPlans.Any())
                {
                    string dayName = now.ToString("dddd", new System.Globalization.CultureInfo("ar-SA"));
                    ToastNotificationService.ShowWeeklyMembersReminder(weeklyPlans.Count, dayName);
                    System.Diagnostics.Debug.WriteLine($"📅 تذكير: {weeklyPlans.Count} عضو أسبوعي في يوم {dayName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تذكير الأعضاء الأسبوعيين: {ex.Message}");
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
