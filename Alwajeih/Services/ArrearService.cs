using System;
using System.Linq;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Services
{
    /// <summary>
    /// خدمة المتأخرات والسوابق
    /// المتأخرات: تخص الأسبوع الحالي فقط (يومية)
    /// السابقات: تخص الأسابيع السابقة (أسبوعية)
    /// </summary>
    public class ArrearService
    {
        private readonly ArrearRepository _arrearRepository;
        private readonly AccumulatedArrearsRepository _accumulatedArrearsRepository;
        private readonly AccumulatedArrearPaymentRepository _accumulatedPaymentRepository;
        private readonly WeeklyArrearPaymentHistoryRepository _paymentHistoryRepository;
        private readonly AuditRepository _auditRepository;
        private readonly SystemSettingsRepository _settingsRepository;

        public ArrearService()
        {
            _arrearRepository = new ArrearRepository();
            _accumulatedArrearsRepository = new AccumulatedArrearsRepository();
            _accumulatedPaymentRepository = new AccumulatedArrearPaymentRepository();
            _paymentHistoryRepository = new WeeklyArrearPaymentHistoryRepository();
            _auditRepository = new AuditRepository();
            _settingsRepository = new SystemSettingsRepository();
        }

        /// <summary>
        /// إنشاء متأخرة يومية - للأسبوع الحالي فقط
        /// </summary>
        public int CreateDailyArrear(int planId, DateTime arrearDate, decimal amountDue)
        {
            // تحميل تاريخ البداية من الإعدادات
            var settings = _settingsRepository.GetCurrentSettings();
            if (settings != null)
            {
                WeekHelper.StartDate = settings.StartDate;
            }

            // حساب رقم الأسبوع واليوم
            int weekNumber = WeekHelper.GetWeekNumber(arrearDate);
            int dayNumber = WeekHelper.GetDayNumber(arrearDate);

            var arrear = new DailyArrear
            {
                PlanID = planId,
                WeekNumber = weekNumber,
                DayNumber = dayNumber,
                ArrearDate = arrearDate,
                AmountDue = amountDue,
                RemainingAmount = amountDue
            };

            int arrearId = _arrearRepository.Add(arrear);

            return arrearId;
        }

        /// <summary>
        /// حساب مجموع السوابق
        /// </summary>
        public decimal CalculateTotalArrears(int planId)
        {
            var dailyArrears = _arrearRepository.GetUnpaidArrears(planId);
            return dailyArrears.Sum(a => a.RemainingAmount);
        }

        /// <summary>
        /// إنشاء متأخرات تلقائية للأعضاء الذين لم يدفعوا في يوم معين
        /// يتم استدعاؤها في نهاية كل يوم
        /// </summary>
        public (bool Success, string Message, int ArrearsCreated) CreateMissingDailyArrears(DateTime date)
        {
            try
            {
                // تحميل تاريخ البداية من الإعدادات
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    WeekHelper.StartDate = settings.StartDate;
                }

                // حساب رقم الأسبوع واليوم
                int weekNumber = WeekHelper.GetWeekNumber(date);
                int dayNumber = WeekHelper.GetDayNumber(date);

                // الحصول على جميع الأسهم النشطة
                var planRepository = new SavingPlanRepository();
                var memberRepository = new MemberRepository();
                var activePlans = planRepository.GetActive().ToList();

                // الحصول على التحصيلات لهذا اليوم
                var collectionRepository = new DailyCollectionRepository();
                
                int arrearsCreated = 0;

                foreach (var plan in activePlans)
                {
                    // ✅ تجاهل أعضاء خلف الجمعية - لا متأخرات لهم
                    var member = memberRepository.GetById(plan.MemberID);
                    if (member != null && member.MemberType == MemberType.BehindAssociation)
                        continue;
                    
                    // التحقق من وجود سداد لهذا اليوم
                    bool hasPaidToday = collectionRepository.HasExistingPayment(
                        plan.PlanID, 
                        weekNumber, 
                        dayNumber);

                    if (!hasPaidToday)
                    {
                        // لم يدفع - إنشاء متأخرة بكامل المبلغ اليومي
                        var arrear = new DailyArrear
                        {
                            PlanID = plan.PlanID,
                            WeekNumber = weekNumber,
                            DayNumber = dayNumber,
                            ArrearDate = date,
                            AmountDue = plan.DailyAmount,
                            RemainingAmount = plan.DailyAmount
                        };

                        _arrearRepository.Add(arrear);
                        arrearsCreated++;
                    }
                    else
                    {
                        // تحقق من الدفع الجزئي
                        var payment = collectionRepository.GetByPlanWeekDay(
                            plan.PlanID, 
                            weekNumber, 
                            dayNumber);

                        if (payment != null && payment.AmountPaid < plan.DailyAmount)
                        {
                            // دفع جزئي - التحقق من عدم وجود متأخرة مسبقة
                            var existingArrear = _arrearRepository.GetArrearsByPlanAndWeek(
                                plan.PlanID, 
                                weekNumber)
                                .FirstOrDefault(a => a.DayNumber == dayNumber);

                            if (existingArrear == null)
                            {
                                decimal arrearAmount = plan.DailyAmount - payment.AmountPaid;
                                var arrear = new DailyArrear
                                {
                                    PlanID = plan.PlanID,
                                    WeekNumber = weekNumber,
                                    DayNumber = dayNumber,
                                    ArrearDate = date,
                                    AmountDue = arrearAmount,
                                    RemainingAmount = arrearAmount
                                };

                                _arrearRepository.Add(arrear);
                                arrearsCreated++;
                            }
                        }
                    }
                }

                return (true, $"تم إنشاء {arrearsCreated} متأخرة تلقائياً", arrearsCreated);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// التحقق من أن متأخرات الأسبوع المحدد تم تحويلها إلى سابقات مسبقاً
        /// </summary>
        public bool CheckIfArrearsAlreadyConverted(int weekNumber)
        {
            try
            {
                // التحقق من AccumulatedArrears - إذا كان LastWeekNumber > weekNumber فهذا يعني أن الأسبوع تمت معالجته
                var accumulatedArrears = _accumulatedArrearsRepository.GetAll()
                    .Where(a => a.LastWeekNumber > weekNumber)
                    .ToList();

                if (accumulatedArrears.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ الأسبوع {weekNumber}: تم العثور على {accumulatedArrears.Count} سجل متراكم تم تجاوز هذا الأسبوع");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق من تحويل المتأخرات: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// تحويل متأخرات الأسبوع الحالي إلى سابقات عند نهاية الأسبوع
        /// يتم استدعاؤها من الجرد الأسبوعي
        /// ويحدث أيضاً جدول AccumulatedArrears
        /// </summary>
        public (bool Success, string Message) ConvertCurrentWeekArrearsToPrevious(int weekNumber)
        {
            try
            {
                // الحصول على جميع المتأخرات غير المسددة للأسبوع المحدد
                var currentWeekArrears = _arrearRepository.GetArrearsByWeek(weekNumber)
                    .Where(a => !a.IsPaid)
                    .GroupBy(a => a.PlanID);

                int convertedCount = 0;
                int accumulatedUpdated = 0;

                foreach (var planArrears in currentWeekArrears)
                {
                    int planId = planArrears.Key;
                    decimal totalArrears = planArrears.Sum(a => a.RemainingAmount);

                    if (totalArrears > 0)
                    {
                        // ✅ التحقق: هل تم تحويل متأخرات هذا الأسبوع مسبقاً؟
                        var existingPrevious = _arrearRepository.GetPreviousArrearsByPlanAndWeek(planId, weekNumber);
                        
                        if (existingPrevious == null || existingPrevious.Count == 0)
                        {
                            // ✅ لم يتم التحويل مسبقاً → نحول الآن
                            // 1️⃣ إنشاء سابقة للأسبوع في PreviousArrears (للتاريخ والتفاصيل)
                            var previousArrear = new PreviousArrears
                            {
                                PlanID = planId,
                                WeekNumber = weekNumber,
                                TotalArrears = totalArrears,
                                RemainingAmount = totalArrears,
                                IsPaid = false
                            };

                            _arrearRepository.AddPreviousArrears(previousArrear);
                            convertedCount++;
                        }
                        else
                        {
                            // ⚠️ تم التحويل مسبقاً → نتخطى إنشاء PreviousArrears فقط
                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ الأسبوع {weekNumber} - السهم {planId}: تم تخطي إنشاء PreviousArrears (محول مسبقاً)");
                        }

                        // 2️⃣ تحديث AccumulatedArrears (الإجمالي المتراكم) - يتم دائماً
                        var accumulated = _accumulatedArrearsRepository.GetByPlanId(planId);
                        
                        if (accumulated != null)
                        {
                            // ✅ 📝 تسجيل المدفوعات في سجل التاريخ قبل التصفير (لكشف حساب العضو)
                            if (accumulated.PaidAmount > 0)
                            {
                                var paymentHistory = new WeeklyArrearPaymentHistory
                                {
                                    PlanID = planId,
                                    WeekNumber = weekNumber,
                                    PaymentDate = DateTime.Now,
                                    AmountPaid = accumulated.PaidAmount,
                                    RemainingBeforePayment = accumulated.TotalArrears,
                                    RemainingAfterPayment = accumulated.RemainingAmount,
                                    Notes = $"سداد سابقات الأسبوع {weekNumber}",
                                    RecordedAt = DateTime.Now
                                };
                                
                                _paymentHistoryRepository.Add(paymentHistory);
                                
                                System.Diagnostics.Debug.WriteLine(
                                    $"📝 تسجيل مدفوعات - السهم {planId}: دفع {accumulated.PaidAmount:N2} ريال في الأسبوع {weekNumber}");
                            }
                            
                            // ✅ إعادة ضبط السابقات للأسبوع الجديد:
                            // المتبقي من الأسبوع السابق يصبح هو الإجمالي الجديد
                            // نصفر المدفوع لنبدأ من جديد في الأسبوع التالي
                            
                            accumulated.TotalArrears = accumulated.RemainingAmount;  // المتبقي يصبح الإجمالي
                            accumulated.PaidAmount = 0;                               // تصفير المدفوع
                            accumulated.RemainingAmount = accumulated.TotalArrears;   // المتبقي = الإجمالي
                            accumulated.LastWeekNumber = weekNumber + 1;              // الأسبوع التالي
                            accumulated.LastUpdated = DateTime.Now;
                            
                            _accumulatedArrearsRepository.Update(accumulated);
                        }
                        else
                        {
                            // إنشاء سجل جديد
                            var newAccumulated = new AccumulatedArrears
                            {
                                PlanID = planId,
                                LastWeekNumber = weekNumber,
                                TotalArrears = totalArrears,
                                PaidAmount = 0,
                                RemainingAmount = totalArrears,
                                IsPaid = false,
                                CreatedDate = DateTime.Now,
                                LastUpdated = DateTime.Now
                            };
                            
                            _accumulatedArrearsRepository.Add(newAccumulated);
                        }
                        
                        accumulatedUpdated++;

                        System.Diagnostics.Debug.WriteLine(
                            $"✅ الأسبوع {weekNumber} - السهم {planId}: تحويل {totalArrears:N2} ريال → سابقات + تحديث الإجمالي");
                    }
                }

                // ✅ تحديث باقي السجلات التي LastWeekNumber == weekNumber (حتى لو لم يكن لها متأخرات هذا الأسبوع)
                var allAccumulated = _accumulatedArrearsRepository.GetAll()
                    .Where(a => a.LastWeekNumber == weekNumber && !a.IsPaid)
                    .ToList();
                
                foreach (var accumulated in allAccumulated)
                {
                    // تحقق: هل تم تحديثه بالفعل في الحلقة السابقة؟
                    bool alreadyUpdated = currentWeekArrears.Any(g => g.Key == accumulated.PlanID);
                    
                    if (!alreadyUpdated)
                    {
                        // ✅ 📝 تسجيل المدفوعات قبل التصفير
                        if (accumulated.PaidAmount > 0)
                        {
                            var paymentHistory = new WeeklyArrearPaymentHistory
                            {
                                PlanID = accumulated.PlanID,
                                WeekNumber = weekNumber,
                                PaymentDate = DateTime.Now,
                                AmountPaid = accumulated.PaidAmount,
                                RemainingBeforePayment = accumulated.TotalArrears,
                                RemainingAfterPayment = accumulated.RemainingAmount,
                                Notes = $"سداد سابقات الأسبوع {weekNumber}",
                                RecordedAt = DateTime.Now
                            };
                            
                            _paymentHistoryRepository.Add(paymentHistory);
                        }
                        
                        // ✅ تحديث البيانات
                        accumulated.TotalArrears = accumulated.RemainingAmount;
                        accumulated.PaidAmount = 0;
                        accumulated.RemainingAmount = accumulated.TotalArrears;
                        accumulated.LastWeekNumber = weekNumber + 1;
                        accumulated.LastUpdated = DateTime.Now;
                        
                        _accumulatedArrearsRepository.Update(accumulated);
                        accumulatedUpdated++;
                        
                        System.Diagnostics.Debug.WriteLine(
                            $"✅ الأسبوع {weekNumber} - السهم {accumulated.PlanID}: تحديث AccumulatedArrears (بدون متأخرات جديدة)");
                    }
                }

                return (true, $"تم تحويل متأخرات {convertedCount} سهم إلى سابقات وتحديث {accumulatedUpdated} سجل متراكم");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في تحويل المتأخرات: {ex.Message}");
            }
        }

        /// <summary>
        /// تحديث السابقات المتراكمة عند الجرد الأسبوعي
        /// - حساب إجمالي المدفوعات من جدول AccumulatedArrearPayments
        /// - تحديث LastWeekNumber للأسبوع التالي
        /// - تصفير PaidAmount للأسبوع الجديد
        /// </summary>
        public (bool Success, string Message) UpdateAccumulatedArrearsOnReconciliation(int weekNumber)
        {
            try
            {
                // جلب جميع السابقات المتراكمة التي LastWeekNumber == weekNumber
                // ✅ تغيير: نستخدم == بدلاً من <= للتأكد من أننا نعالج الأسبوع الحالي فقط
                var allAccumulated = _accumulatedArrearsRepository.GetAll()
                    .Where(a => a.LastWeekNumber == weekNumber && !a.IsPaid)
                    .ToList();
                
                int updatedCount = 0;
                
                foreach (var accumulated in allAccumulated)
                {
                    // حساب إجمالي المدفوعات من جدول AccumulatedArrearPayments لهذا الأسبوع
                    var weekPayments = _accumulatedPaymentRepository.GetByWeek(weekNumber)
                        .Where(p => p.PlanID == accumulated.PlanID)
                        .Sum(p => p.AmountPaid);
                    
                    if (weekPayments > 0)
                    {
                        // ✅ تحديث السابقات المتراكمة بناءً على المدفوعات المسجلة
                        accumulated.PaidAmount += weekPayments;
                        accumulated.RemainingAmount -= weekPayments;
                        accumulated.IsPaid = accumulated.RemainingAmount <= 0;
                        accumulated.LastUpdated = DateTime.Now;
                        
                        _accumulatedArrearsRepository.Update(accumulated);
                        updatedCount++;
                        
                        System.Diagnostics.Debug.WriteLine(
                            $"✅ الأسبوع {weekNumber} - السهم {accumulated.PlanID}: دفع {weekPayments:N2} ريال");
                    }
                }
                
                return (true, $"تم تحديث {updatedCount} سجل سابقات متراكمة");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في تحديث السابقات: {ex.Message}");
            }
        }
        
        /// <summary>
        /// تسجيل دفعة سابقات عند التحصيل اليومي
        /// يتم استدعاؤها عند دفع سابقات لتسجيل الدفعة في جدول AccumulatedArrearPayments
        /// </summary>
        public (bool Success, string Message) RecordPreviousArrearPayment(int planId, int weekNumber, int dayNumber, decimal amount, int recordedBy)
        {
            try
            {
                if (amount <= 0)
                    return (true, "لا يوجد مبلغ لتسجيله");
                
                // تسجيل الدفعة في جدول AccumulatedArrearPayments (حتى لو لم يكن هناك سجل في AccumulatedArrears)
                // هذا مهم للبيانات القديمة التي تم إدخالها قبل إنشاء الجدول
                var payment = new AccumulatedArrearPayment
                {
                    PlanID = planId,
                    WeekNumber = weekNumber,
                    DayNumber = dayNumber,
                    AmountPaid = amount,
                    PaymentDate = DateTime.Now,
                    RecordedBy = recordedBy,
                    Notes = $"دفعة سابقات - الأسبوع {weekNumber} اليوم {dayNumber}"
                };
                
                _accumulatedPaymentRepository.Add(payment);
                
                System.Diagnostics.Debug.WriteLine(
                    $"✅ تسجيل دفعة سابقات: السهم {planId} - الأسبوع {weekNumber} - المبلغ {amount:N2} ريال");
                
                return (true, $"تم تسجيل دفعة سابقات بمبلغ {amount:N2} ريال");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في تسجيل دفعة السابقات: {ex.Message}");
            }
        }

        /// <summary>
        /// التحقق مما إذا تمت معالجة البيانات القديمة مسبقاً
        /// </summary>
        public bool IsHistoricalDataProcessed()
        {
            try
            {
                // التحقق من وجود متأخرات وسابقات في قاعدة البيانات
                var anyArrears = _arrearRepository.GetAllUnpaid().Any();
                var anyPreviousArrears = _arrearRepository.GetUnpaidPreviousArrears().Any();
                
                return anyArrears || anyPreviousArrears;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// معالجة البيانات القديمة: إنشاء متأخرات وسابقات للأسابيع الماضية
        /// </summary>
        public (bool Success, string Message, int ArrearsCreated, int PreviousCreated) ProcessHistoricalData(
            Action<int, string> progressCallback = null)
        {
            try
            {
                // تحميل تاريخ البداية من الإعدادات
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    WeekHelper.StartDate = settings.StartDate;
                }

                int currentWeek = WeekHelper.GetCurrentWeekNumber();
                
                // التأكد من أن الأسبوع في النطاق الصحيح (1-26)
                if (currentWeek < 1)
                    currentWeek = 1;
                else if (currentWeek > WeekHelper.TotalWeeks)
                    currentWeek = WeekHelper.TotalWeeks;
                
                int arrearsCreated = 0;
                int previousCreated = 0;

                var planRepository = new SavingPlanRepository();
                var memberRepository = new MemberRepository();
                var collectionRepository = new DailyCollectionRepository();
                
                // معالجة الأسابيع الماضية + الأيام الماضية من الأسبوع الحالي
                int currentDay = WeekHelper.GetCurrentDayNumber();
                int weeksToProcess = currentWeek; // شمل الأسبوع الحالي
                int totalSteps = ((currentWeek - 1) * 7) + (currentDay - 1); // الأسابيع الكاملة + الأيام الماضية من الأسبوع الحالي
                int currentStep = 0;

                // معالجة كل أسبوع من 1 إلى الأسبوع الحالي
                for (int week = 1; week <= currentWeek; week++)
                {
                    progressCallback?.Invoke(
                        (currentStep * 100) / totalSteps,
                        $"معالجة الأسبوع {week} من {weeksToProcess}...");
                    
                    // معالجة كل يوم في الأسبوع
                    // إذا كان الأسبوع الحالي، فقط نعالج الأيام السابقة (قبل اليوم الحالي)
                    int lastDayToProcess = (week == currentWeek) ? currentDay - 1 : 7;
                    
                    for (int day = 1; day <= lastDayToProcess; day++)
                    {
                        currentStep++;
                        DateTime date = WeekHelper.GetDateFromWeekAndDay(week, day);
                        
                        // تخطي التواريخ المستقبلية
                        if (date > DateTime.Now.Date)
                            continue;

                        var activePlans = planRepository.GetActive().ToList();

                        foreach (var plan in activePlans)
                        {
                            try
                            {
                                // ✅ تجاهل أعضاء خلف الجمعية - لا متأخرات لهم
                                var member = memberRepository.GetById(plan.MemberID);
                                if (member != null && member.MemberType == MemberType.BehindAssociation)
                                    continue;
                                
                                // التحقق من وجود متأخرة مسبقة باستخدام PlanID و ArrearDate (UNIQUE constraint)
                                var existingArrears = _arrearRepository.GetArrearsByPlanAndWeek(plan.PlanID, week);
                                bool arrearExists = existingArrears.Any(a => 
                                    a.DayNumber == day && 
                                    a.ArrearDate.Date == date.Date);

                                if (arrearExists)
                                    continue; // تخطي إذا كانت المتأخرة موجودة مسبقاً

                                // التحقق من وجود سداد
                                bool hasPaid = collectionRepository.HasExistingPayment(plan.PlanID, week, day);

                                if (!hasPaid)
                                {
                                    // إنشاء متأخرة - لم يدفع
                                    var arrear = new DailyArrear
                                    {
                                        PlanID = plan.PlanID,
                                        WeekNumber = week,
                                        DayNumber = day,
                                        ArrearDate = date,
                                        AmountDue = plan.DailyAmount,
                                        RemainingAmount = plan.DailyAmount,
                                        IsPaid = false
                                    };

                                    _arrearRepository.Add(arrear);
                                    arrearsCreated++;
                                }
                                else
                                {
                                    // تحقق من الدفع الجزئي
                                    var payment = collectionRepository.GetByPlanWeekDay(plan.PlanID, week, day);
                                    if (payment != null && payment.AmountPaid < plan.DailyAmount)
                                    {
                                        decimal arrearAmount = plan.DailyAmount - payment.AmountPaid;
                                        var arrear = new DailyArrear
                                        {
                                            PlanID = plan.PlanID,
                                            WeekNumber = week,
                                            DayNumber = day,
                                            ArrearDate = date,
                                            AmountDue = arrearAmount,
                                            RemainingAmount = arrearAmount,
                                            IsPaid = false
                                        };

                                        _arrearRepository.Add(arrear);
                                        arrearsCreated++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // تخطي المتأخرات المكررة أو الأخطاء الأخرى
                                System.Diagnostics.Debug.WriteLine($"خطأ في معالجة الخطة {plan.PlanID} للأسبوع {week} اليوم {day}: {ex.Message}");
                                continue;
                            }
                        }
                    }

                    // تحويل متأخرات الأسابيع السابقة إلى سابقات
                    // ملاحظة: كل أسبوع له سابقة منفصلة، لكن عند العرض يتم جمعها (تراكمها)
                    if (week < currentWeek)
                    {
                        var currentWeekArrears = _arrearRepository.GetArrearsByWeek(week)
                            .Where(a => !a.IsPaid)
                            .GroupBy(a => a.PlanID);

                        foreach (var planArrears in currentWeekArrears)
                        {
                            int planId = planArrears.Key;
                            decimal totalArrears = planArrears.Sum(a => a.RemainingAmount);

                            if (totalArrears > 0)
                            {
                                // التحقق من عدم وجود سابقة مسبقة لنفس الأسبوع (تجنب التكرار)
                                var existingPrevious = _arrearRepository.GetPreviousArrearsByPlanId(planId)
                                    .FirstOrDefault(p => p.WeekNumber == week);

                                if (existingPrevious == null)
                                {
                                    // إنشاء سابقة لهذا الأسبوع
                                    // عند الاستعلام، يتم جمع جميع السابقات للحصول على الإجمالي المُراكم
                                    var previousArrear = new PreviousArrears
                                    {
                                        PlanID = planId,
                                        WeekNumber = week,
                                        TotalArrears = totalArrears,
                                        RemainingAmount = totalArrears,
                                        IsPaid = false
                                    };

                                    _arrearRepository.AddPreviousArrears(previousArrear);
                                    previousCreated++;
                                }
                            }
                        }
                    }
                }
                
                // 🔄 الخطوة الجديدة: حساب وتعبئة جدول AccumulatedArrears
                progressCallback?.Invoke(95, "تحديث جدول السابقات المتراكمة...");
                int accumulatedUpdated = UpdateAccumulatedArrearsFromHistory(currentWeek);
                
                // إرسال تحديث نهائي
                progressCallback?.Invoke(100, "اكتملت المعالجة بنجاح!");

                return (true, 
                    $"تمت معالجة البيانات القديمة بنجاح\n" +
                    $"• تم تحديث {accumulatedUpdated} سجل في جدول السابقات المتراكمة", 
                    arrearsCreated, 
                    previousCreated);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}", 0, 0);
            }
        }
        
        /// <summary>
        /// تحديث جدول السابقات المتراكمة من البيانات التاريخية
        /// يحسب مجموع سابقات كل عضو من PreviousArrears ويسجلها في AccumulatedArrears
        /// ✅ يحافظ على المدفوعات الموجودة - لا يعيد حسابها
        /// </summary>
        private int UpdateAccumulatedArrearsFromHistory(int currentWeek)
        {
            int updated = 0;
            
            try
            {
                var planRepository = new SavingPlanRepository();
                var memberRepository = new MemberRepository();
                var activePlans = planRepository.GetActive().ToList();
                
                foreach (var plan in activePlans)
                {
                    // ✅ تجاهل أعضاء خلف الجمعية - لا سابقات لهم
                    var member = memberRepository.GetById(plan.MemberID);
                    if (member != null && member.MemberType == MemberType.BehindAssociation)
                        continue;
                    
                    // جلب جميع سابقات الأسابيع السابقة للعضو
                    var previousArrears = _arrearRepository.GetPreviousArrearsByPlanId(plan.PlanID)
                        .Where(p => p.WeekNumber < currentWeek)
                        .ToList();
                    
                    if (!previousArrears.Any())
                        continue;
                    
                    // حساب الإجمالي من PreviousArrears
                    decimal totalArrearsFromHistory = previousArrears.Sum(p => p.TotalArrears);
                    int lastWeek = previousArrears.Max(p => p.WeekNumber);
                    
                    // التحقق من وجود سجل متراكم
                    var existing = _accumulatedArrearsRepository.GetByPlanId(plan.PlanID);
                    
                    if (existing != null)
                    {
                        // ✅ السجل موجود - نحافظ على المدفوعات الموجودة
                        // فقط نحدث LastWeekNumber و TotalArrears إذا تغيرت
                        if (existing.LastWeekNumber < lastWeek || existing.TotalArrears != totalArrearsFromHistory)
                        {
                            existing.LastWeekNumber = lastWeek;
                            existing.TotalArrears = totalArrearsFromHistory;
                            
                            // ✅ نحافظ على PaidAmount (لا نمسه!)
                            // نحسب RemainingAmount بناءً على الموجود
                            existing.RemainingAmount = existing.TotalArrears - existing.PaidAmount;
                            existing.IsPaid = (existing.RemainingAmount <= 0);
                            existing.LastUpdated = DateTime.Now;
                            
                            _accumulatedArrearsRepository.Update(existing);
                            updated++;
                        }
                    }
                    else
                    {
                        // ✅ سجل جديد - نأخذ البيانات من PreviousArrears
                        decimal paidAmountFromHistory = previousArrears.Sum(p => p.PaidAmount);
                        decimal remainingAmountFromHistory = previousArrears.Sum(p => p.RemainingAmount);
                        
                        var accumulated = new AccumulatedArrears
                        {
                            PlanID = plan.PlanID,
                            LastWeekNumber = lastWeek,
                            TotalArrears = totalArrearsFromHistory,
                            PaidAmount = paidAmountFromHistory,
                            RemainingAmount = remainingAmountFromHistory,
                            IsPaid = (remainingAmountFromHistory <= 0),
                            CreatedDate = DateTime.Now,
                            LastUpdated = DateTime.Now
                        };
                        
                        _accumulatedArrearsRepository.Add(accumulated);
                        updated++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحديث السابقات المتراكمة: {ex.Message}");
            }
            
            return updated;
        }

        /// <summary>
        /// حساب إجمالي متأخرات الأسبوع الحالي لعضو معين
        /// </summary>
        public decimal GetCurrentWeekArrearsTotal(int planId, int weekNumber)
        {
            var arrears = _arrearRepository.GetArrearsByPlanAndWeek(planId, weekNumber)
                .Where(a => !a.IsPaid);
            return arrears.Sum(a => a.RemainingAmount);
        }

        /// <summary>
        /// حساب إجمالي السابقات لعضو معين
        /// </summary>
        public decimal GetPreviousArrearsTotal(int planId)
        {
            var previousArrears = _arrearRepository.GetPreviousArrearsByPlanId(planId);
            return previousArrears?.Where(p => !p.IsPaid).Sum(p => p.RemainingAmount) ?? 0;
        }

        /// <summary>
        /// توزيع دفعة على المتأخرات (FIFO)
        /// </summary>
        public (bool Success, string Message) DistributePayment(int planId, decimal amount, int userId)
        {
            try
            {
                var arrears = _arrearRepository.GetUnpaidArrears(planId)
                                              .OrderBy(a => a.ArrearDate)
                                              .ToList();

                if (!arrears.Any())
                    return (false, "لا توجد متأخرات لسدادها");

                decimal remainingAmount = amount;

                foreach (var arrear in arrears)
                {
                    if (remainingAmount <= 0) break;

                    decimal toPay = Math.Min(remainingAmount, arrear.RemainingAmount);

                    arrear.PaidAmount += toPay;
                    arrear.RemainingAmount -= toPay;

                    if (arrear.RemainingAmount == 0)
                    {
                        arrear.IsPaid = true;
                        arrear.PaidDate = DateTime.Now;
                    }

                    _arrearRepository.Update(arrear);
                    remainingAmount -= toPay;
                }

                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.DailyArrear,
                    Details = $"سداد متأخرات بمبلغ {amount} ريال"
                });

                return (true, $"تم سداد {amount - remainingAmount} ريال من المتأخرات");
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// سداد متأخرة (alias for DistributePayment)
        /// </summary>
        public (bool Success, string Message) PayArrear(int arrearId, decimal amount, int userId)
        {
            var arrear = _arrearRepository.GetById(arrearId);
            if (arrear == null)
                return (false, "المتأخرة غير موجودة");

            return DistributePayment(arrear.PlanID, amount, userId);
        }
        
        /// <summary>
        /// سداد متأخرة مع تفاصيل الدفع
        /// </summary>
        public (bool Success, string Message) PayArrear(int arrearId, decimal amount, PaymentSource paymentSource, string? notes, int userId)
        {
            try
            {
                var arrear = _arrearRepository.GetById(arrearId);
                if (arrear == null)
                    return (false, "المتأخرة غير موجودة");

                if (amount <= 0)
                    return (false, "المبلغ يجب أن يكون أكبر من صفر");

                if (amount > arrear.RemainingAmount)
                    return (false, "المبلغ المدفوع أكبر من المتبقي");

                // تحديث المتأخرة
                arrear.PaidAmount += amount; // ✅ تحديث المبلغ المسدد
                arrear.RemainingAmount -= amount;
                arrear.IsPaid = arrear.RemainingAmount == 0;
                if (arrear.IsPaid)
                    arrear.PaidDate = DateTime.Now;

                _arrearRepository.Update(arrear);

                // تسجيل في Audit
                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.DailyArrear,
                    EntityID = arrearId,
                    Details = $"سداد متأخرة بمبلغ {amount:N2} - {paymentSource}",
                    Reason = notes
                });

                return (true, $"تم سداد {amount:N2} ريال من المتأخرة");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }
        
        /// <summary>
        /// سداد سابقة من التحصيل اليومي
        /// يسجل في DailyCollection ويحدث AccumulatedArrears فقط
        /// </summary>
        public (bool Success, string Message) PayPreviousArrear(int planId, decimal amount, PaymentSource paymentSource, string? notes, int userId)
        {
            try
            {
                if (amount <= 0)
                    return (false, "المبلغ يجب أن يكون أكبر من صفر");

                // التحقق من وجود السجل المتراكم
                var accumulated = _accumulatedArrearsRepository.GetByPlanId(planId);
                if (accumulated == null || accumulated.RemainingAmount == 0)
                    return (false, "لا توجد سابقات متبقية لهذا العضو");

                if (amount > accumulated.RemainingAmount)
                    return (false, $"المبلغ المدفوع ({amount:N2}) أكبر من المتبقي ({accumulated.RemainingAmount:N2})");

                // الحصول على معلومات الخطة
                var planRepo = new SavingPlanRepository();
                var plan = planRepo.GetById(planId);
                if (plan == null)
                    return (false, "الخطة غير موجودة");

                // الحصول على رقم الأسبوع واليوم الحاليين
                var (currentWeek, currentDay) = WeekHelper.GetWeekAndDayFromDate(DateTime.Now);

                // 1️⃣ تسجيل الدفعة في جدول AccumulatedArrearPayments (لكشف الحساب)
                var paymentResult = RecordPreviousArrearPayment(planId, currentWeek, currentDay, amount, userId);
                if (!paymentResult.Success)
                    return (false, $"فشل تسجيل الدفعة: {paymentResult.Message}");
                
                // ملاحظة: لا نحدث AccumulatedArrears هنا - سيتم تحديثه عند الجرد الأسبوعي
                // هذا يضمن أن LastWeekNumber يبقى صحيحاً ويتم تحديثه فقط عند الجرد
                
                // 2️⃣ توزيع المبلغ على PreviousArrears (للسجل التاريخي)
                DistributePaymentToPreviousArrears(planId, amount);

                // 3️⃣ تسجيل في Audit
                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.PreviousArrears,
                    EntityID = accumulated.AccumulatedArrearID,
                    Details = $"سداد سابقات للعضو {plan.MemberName} بمبلغ {amount:N2} - {paymentSource} - سيتم تحديث الرصيد عند الجرد",
                    Reason = notes
                });

                return (true, $"تم تسجيل دفعة سابقات بمبلغ {amount:N2} ريال (سيتم تحديث الرصيد عند الجرد)");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }
        
        /// <summary>
        /// تحديث السابقات المتراكمة (سجل واحد للإجمالي)
        /// يحدث جدول AccumulatedArrears الذي يحتوي على سجل واحد لكل عضو
        /// </summary>
        public (bool Success, string Message) AddDirectPreviousArrears(
            int planId, 
            int weekNumberFrom, 
            int weekNumberTo, 
            decimal totalOriginalAmount,
            decimal remainingAmount,
            string? notes,
            int userId)
        {
            try
            {
                if (remainingAmount < 0)
                    return (false, "المبلغ المتبقي لا يمكن أن يكون سالباً");

                var planRepo = new SavingPlanRepository();
                var plan = planRepo.GetById(planId);
                
                if (plan == null)
                    return (false, "الخطة غير موجودة");

                // البحث عن السجل المتراكم لهذا العضو
                var accumulated = _accumulatedArrearsRepository.GetByPlanId(planId);

                int arrearId;
                decimal paidAmount;
                
                if (accumulated != null)
                {
                    // تحديث السجل المتراكم الموجود
                    // الإجمالي يبقى ثابت، فقط نحدث المتبقي والمدفوع
                    decimal oldRemaining = accumulated.RemainingAmount;
                    accumulated.RemainingAmount = remainingAmount;
                    accumulated.PaidAmount = accumulated.TotalArrears - remainingAmount;
                    accumulated.IsPaid = (remainingAmount == 0);
                    accumulated.LastWeekNumber = weekNumberTo;
                    accumulated.LastUpdated = DateTime.Now;
                    
                    bool updated = _accumulatedArrearsRepository.Update(accumulated);
                    if (!updated)
                        return (false, "فشل تحديث السابقات في قاعدة البيانات");
                    
                    // ✅ توزيع المبلغ المدفوع على PreviousArrears (للسجل التاريخي)
                    decimal paidInThisUpdate = oldRemaining - remainingAmount;
                    if (paidInThisUpdate > 0)
                    {
                        DistributePaymentToPreviousArrears(planId, paidInThisUpdate);
                    }
                    
                    arrearId = accumulated.AccumulatedArrearID;
                    paidAmount = accumulated.PaidAmount;
                }
                else
                {
                    // إنشاء أول سجل متراكم لهذا العضو
                    decimal actualTotalArrears = totalOriginalAmount > 0 ? totalOriginalAmount : remainingAmount;
                    paidAmount = actualTotalArrears - remainingAmount;
                    
                    var newAccumulated = new AccumulatedArrears
                    {
                        PlanID = planId,
                        LastWeekNumber = weekNumberTo,
                        TotalArrears = actualTotalArrears,
                        PaidAmount = paidAmount,
                        RemainingAmount = remainingAmount,
                        IsPaid = (remainingAmount == 0),
                        CreatedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };

                    arrearId = _accumulatedArrearsRepository.Add(newAccumulated);
                    
                    if (arrearId <= 0)
                        return (false, "فشل حفظ السابقات في قاعدة البيانات");
                }
                
                // تسجيل في Audit
                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = accumulated != null ? AuditAction.Update : AuditAction.Create,
                    EntityType = EntityType.PreviousArrears,
                    EntityID = arrearId,
                    Details = $"تحديث السابقات المتراكمة للعضو {plan.MemberName} - آخر أسبوع: {weekNumberTo} - الإجمالي: {totalOriginalAmount:N2} - المدفوع: {paidAmount:N2} - المتبقي: {remainingAmount:N2}",
                    Reason = notes
                });

                return (true, $"✅ تم تحديث السابقات بنجاح\nالمتبقي: {remainingAmount:N2} ريال");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }
        
        /// <summary>
        /// توزيع المبلغ المدفوع على PreviousArrears من الأقدم للأحدث
        /// للحفاظ على السجل التاريخي للمدفوعات
        /// </summary>
        private void DistributePaymentToPreviousArrears(int planId, decimal paymentAmount)
        {
            try
            {
                // الحصول على جميع السابقات غير المسددة بالكامل (من الأقدم للأحدث)
                var previousArrears = _arrearRepository.GetPreviousArrearsByPlanId(planId)
                    .Where(pa => pa.RemainingAmount > 0)
                    .OrderBy(pa => pa.WeekNumber)
                    .ToList();
                
                if (previousArrears.Count == 0)
                    return;
                
                decimal remainingPayment = paymentAmount;
                
                // توزيع المبلغ من الأقدم للأحدث
                foreach (var arrear in previousArrears)
                {
                    if (remainingPayment <= 0)
                        break;
                    
                    decimal paymentForThisArrear = Math.Min(remainingPayment, arrear.RemainingAmount);
                    
                    // تحديث السابقة
                    arrear.PaidAmount += paymentForThisArrear;
                    arrear.RemainingAmount -= paymentForThisArrear;
                    arrear.IsPaid = (arrear.RemainingAmount <= 0);
                    arrear.PaidDate = arrear.IsPaid ? DateTime.Now : arrear.PaidDate;
                    arrear.LastUpdated = DateTime.Now;
                    
                    _arrearRepository.UpdatePreviousArrears(arrear);
                    
                    remainingPayment -= paymentForThisArrear;
                    
                    System.Diagnostics.Debug.WriteLine(
                        $"💰 توزيع سداد: الأسبوع {arrear.WeekNumber} - دفع {paymentForThisArrear:N2} - متبقي {arrear.RemainingAmount:N2}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في توزيع السداد: {ex.Message}");
            }
        }
    }
}
