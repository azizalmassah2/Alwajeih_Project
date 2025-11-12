using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Management
{
    /// <summary>
    /// 📝 ViewModel لسجل التدقيق
    /// </summary>
    public class AuditLogViewModel : BaseViewModel
    {
        private readonly AuditRepository _auditRepository;
        
        private ObservableCollection<AuditLog> _auditLogs;
        private DateTime _startDate = DateTime.Now.AddDays(-7);
        private DateTime _endDate = DateTime.Now;
        private string _searchText;

        public AuditLogViewModel()
        {
            _auditRepository = new AuditRepository();
            AuditLogs = new ObservableCollection<AuditLog>();
            FilterCommand = new RelayCommand(ExecuteFilter, _ => true);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            LoadAuditLogs();
        }

        public ObservableCollection<AuditLog> AuditLogs
        {
            get => _auditLogs;
            set => SetProperty(ref _auditLogs, value);
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

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public ICommand FilterCommand { get; }
        public ICommand RefreshCommand { get; }

        private void ExecuteFilter(object parameter) => LoadAuditLogs();
        private void ExecuteRefresh(object parameter) => LoadAuditLogs();

        private void LoadAuditLogs()
        {
            try
            {
                var logs = _auditRepository.GetByDateRange(StartDate, EndDate);
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    logs = logs.Where(l => 
                        l.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        (l.Details != null && l.Details.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
                }
                AuditLogs.Clear();
                foreach (var log in logs) AuditLogs.Add(log);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
