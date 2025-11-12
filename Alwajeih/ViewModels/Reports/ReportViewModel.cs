using System;
using System.Data;
using System.Windows.Input;
using Alwajeih.Services;
using Alwajeih.Utilities.Helpers;
using Alwajeih.ViewModels.Base;
using Alwajeih.Data.Repositories;

namespace Alwajeih.ViewModels.Reports
{
    /// <summary>
    /// 📑 ViewModel للتقارير
    /// </summary>
    public class ReportViewModel : BaseViewModel
    {
        private readonly ReportService _reportService;
        private readonly AuthenticationService _authService;
        private readonly MemberRepository _memberRepository;
        private readonly SystemSettingsRepository _settingsRepository;

        private DataTable _reportData;
        private string _reportTitle;
        private DateTime _startDate;
        private DateTime _endDate = DateTime.Now;
        private int _selectedReportType;
        private System.Collections.ObjectModel.ObservableCollection<Models.Member> _members;
        private Models.Member _selectedMember;

        public ReportViewModel()
        {
            _reportService = new ReportService();
            _authService = AuthenticationService.Instance;
            _memberRepository = new MemberRepository();
            _settingsRepository = new SystemSettingsRepository();

            // تحميل تاريخ البداية من الإعدادات
            LoadStartDateFromSettings();

            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport, _ => true);
            ExportToExcelCommand = new RelayCommand(ExecuteExportToExcel, CanExecuteExport);
            ExportToPdfCommand = new RelayCommand(ExecuteExportToPdf, CanExecuteExport);
            
            ReportTitle = "اختر نوع التقرير";
            LoadMembers();
        }

        private void LoadStartDateFromSettings()
        {
            try
            {
                var settings = _settingsRepository.GetCurrentSettings();
                if (settings != null)
                {
                    _startDate = settings.StartDate;
                }
                else
                {
                    // إذا لم يكن محدداً، استخدم تاريخ افتراضي
                    _startDate = new DateTime(DateTime.Now.Year, 1, 1);
                }
            }
            catch
            {
                _startDate = new DateTime(DateTime.Now.Year, 1, 1);
            }
        }

        private void LoadMembers()
        {
            try
            {
                var membersList = _memberRepository.GetAll().Where(m => !m.IsArchived).ToList();
                Members = new System.Collections.ObjectModel.ObservableCollection<Models.Member>(membersList);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل الأعضاء: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #region Properties

        public DataTable ReportData
        {
            get => _reportData;
            set
            {
                SetProperty(ref _reportData, value);
                ((RelayCommand)ExportToExcelCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportToPdfCommand).RaiseCanExecuteChanged();
            }
        }

        public string ReportTitle
        {
            get => _reportTitle;
            set => SetProperty(ref _reportTitle, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public int SelectedReportType
        {
            get => _selectedReportType;
            set => SetProperty(ref _selectedReportType, value);
        }

        public System.Collections.ObjectModel.ObservableCollection<Models.Member> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        public Models.Member SelectedMember
        {
            get => _selectedMember;
            set => SetProperty(ref _selectedMember, value);
        }

        #endregion

        #region Commands

        public ICommand GenerateReportCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToPdfCommand { get; }

        #endregion

        #region Command Implementations

        private void ExecuteGenerateReport(object parameter)
        {
            try
            {
                DataTable data = SelectedReportType switch
                {
                    0 => _reportService.GenerateDailyReport(StartDate),
                    1 => _reportService.GenerateWeeklyReport(StartDate, EndDate),
                    2 => _reportService.GenerateVaultReport(StartDate, EndDate),
                    3 => _reportService.GenerateArrearsReport(),
                    4 => _reportService.GenerateComprehensiveFinancialReport(StartDate, EndDate),
                    5 => SelectedMember != null ? _reportService.GenerateComprehensiveMemberFinancialReport(SelectedMember.MemberID, StartDate, EndDate) : throw new Exception("الرجاء اختيار عضو"),
                    6 => _reportService.GenerateAllMembersReport(),
                    7 => _reportService.GenerateDetailedCollectionsReport(StartDate, EndDate),
                    8 => _reportService.GenerateComprehensiveArrearsReport(),
                    9 => _reportService.GeneratePreviousArrearsReport(),
                    10 => _reportService.GenerateDetailedVaultReport(StartDate, EndDate),
                    11 => _reportService.GenerateExternalPaymentsReport(StartDate, EndDate),
                    12 => SelectedMember != null ? _reportService.GenerateMemberTransactionsReport(SelectedMember.MemberID, StartDate, EndDate) : throw new Exception("الرجاء اختيار عضو"),
                    13 => _reportService.GenerateBehindAssociationReport(),
                    14 => SelectedMember != null ? _reportService.GenerateBehindAssociationMemberReport(SelectedMember.MemberID) : throw new Exception("الرجاء اختيار عضو"),
                    15 => _reportService.GenerateRegularMembersReport(),
                    16 => _reportService.GenerateBehindAssociationMembersOnlyReport(),
                    _ => new DataTable()
                };

                ReportTitle = SelectedReportType switch
                {
                    0 => $"📊 تقرير يومي - {StartDate:yyyy-MM-dd}",
                    1 => $"📅 تقرير أسبوعي - من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}",
                    2 => $"🏦 تقرير الخزنة - من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}",
                    3 => $"⚠️ تقرير المتأخرات",
                    4 => $"💰 تقرير مالي شامل - من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}",
                    5 => $"👤 تقرير مالي شامل للعضو - {SelectedMember?.Name} ({StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd})",
                    6 => $"👥 تقرير جميع الأعضاء",
                    7 => $"💵 تقرير التحصيلات المفصل - من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}",
                    8 => $"⚠️ تقرير المتأخرات الشامل",
                    9 => $"📋 تقرير السوابق",
                    10 => $"🏦 تقرير الخزنة المفصل - من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}",
                    11 => $"💳 تقرير المدفوعات الخارجية - من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}",
                    12 => $"📋 تقرير معاملات العضو - {SelectedMember?.Name} ({StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd})",
                    13 => $"💰 تقرير شامل لأعضاء خلف الجمعية",
                    14 => $"👤 تقرير تفصيلي لعضو خلف الجمعية - {SelectedMember?.Name}",
                    15 => $"👥 تقرير الأعضاء العاديين",
                    16 => $"💰 تقرير أعضاء خلف الجمعية فقط",
                    _ => "تقرير"
                };

                ReportData = data;

                if (data.Rows.Count == 0)
                {
                    System.Windows.MessageBox.Show("⚠️ لا توجد بيانات للعرض", "تنبيه",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    System.Windows.MessageBox.Show($"✅ تم توليد التقرير بنجاح!\n\nعدد السجلات: {data.Rows.Count}", "نجاح",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في توليد التقرير: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteExport(object parameter)
        {
            return ReportData != null && ReportData.Rows.Count > 0;
        }

        private void ExecuteExportToExcel(object parameter)
        {
            try
            {
                // إنشاء مجلد التقارير
                var reportsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "التقارير");
                if (!System.IO.Directory.Exists(reportsFolder))
                {
                    System.IO.Directory.CreateDirectory(reportsFolder);
                }

                var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var fullPath = System.IO.Path.Combine(reportsFolder, fileName);
                
                // استخدام المُصدّر المحسّن
                Utilities.Helpers.EnhancedExcelExporter.ExportToExcel(ReportData, fullPath, ReportTitle);
                
                System.Windows.MessageBox.Show($"✅ تم تصدير التقرير إلى Excel بنجاح!\n\nالمسار: {reportsFolder}\nاسم الملف: {fileName}", "نجاح",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                // فتح مجلد التقارير
                System.Diagnostics.Process.Start("explorer.exe", reportsFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تصدير التقرير: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteExportToPdf(object parameter)
        {
            try
            {
                // إنشاء مجلد التقارير
                var reportsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "التقارير");
                if (!System.IO.Directory.Exists(reportsFolder))
                {
                    System.IO.Directory.CreateDirectory(reportsFolder);
                }

                var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var fullPath = System.IO.Path.Combine(reportsFolder, fileName);
                
                // استخدام المُصدّر المحسّن
                Utilities.Helpers.EnhancedPdfExporter.ExportToPdf(ReportData, fullPath, ReportTitle);
                
                System.Windows.MessageBox.Show($"✅ تم تصدير التقرير إلى PDF بنجاح!\n\nالمسار: {reportsFolder}\nاسم الملف: {fileName}", "نجاح",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                // فتح مجلد التقارير
                System.Diagnostics.Process.Start("explorer.exe", reportsFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تصدير التقرير: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
