using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Alwajeih.Services;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Management
{
    /// <summary>
    /// 💾 ViewModel للنسخ الاحتياطي
    /// </summary>
    public class BackupViewModel : BaseViewModel
    {
        private readonly BackupService _backupService;
        private readonly AuthenticationService _authService;

        private ObservableCollection<string> _availableBackups;
        private string _selectedBackup;

        public BackupViewModel()
        {
            _backupService = new BackupService();
            _authService = AuthenticationService.Instance;

            AvailableBackups = new ObservableCollection<string>();

            CreateBackupCommand = new RelayCommand(ExecuteCreateBackup, CanExecuteBackup);
            RestoreBackupCommand = new RelayCommand(ExecuteRestoreBackup, CanExecuteRestore);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);

            LoadAvailableBackups();
        }

        #region Properties

        public ObservableCollection<string> AvailableBackups
        {
            get => _availableBackups;
            set => SetProperty(ref _availableBackups, value);
        }

        public string SelectedBackup
        {
            get => _selectedBackup;
            set
            {
                SetProperty(ref _selectedBackup, value);
                ((RelayCommand)RestoreBackupCommand).RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanExecuteBackup(object parameter)
        {
            return _authService.HasPermission("ManageBackup");
        }

        private void ExecuteCreateBackup(object parameter)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "هل تريد إنشاء نسخة احتياطية من قاعدة البيانات؟",
                    "تأكيد النسخ الاحتياطي 💾",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var userId = _authService.CurrentUser?.UserID ?? 0;
                    var backupResult = _backupService.CreateBackup(userId);

                    if (backupResult.Success)
                    {
                        System.Windows.MessageBox.Show(
                            $"✅ تم إنشاء النسخة الاحتياطية بنجاح!\n\n" +
                            $"📁 {System.IO.Path.GetFileName(backupResult.BackupPath)}",
                            "نجاح",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        LoadAvailableBackups();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"❌ {backupResult.Message}", "خطأ",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteRestore(object parameter)
        {
            return !string.IsNullOrEmpty(SelectedBackup) && _authService.HasPermission("ManageBackup");
        }

        private void ExecuteRestoreBackup(object parameter)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "⚠️ تحذير: سيتم استبدال جميع البيانات الحالية!\n\n" +
                    "هل أنت متأكد من الاسترجاع؟",
                    "تأكيد الاسترجاع",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var userId = _authService.CurrentUser?.UserID ?? 0;
                    var restoreResult = _backupService.RestoreBackup(SelectedBackup, userId);

                    if (restoreResult.Success)
                    {
                        System.Windows.MessageBox.Show(
                            "✅ تم استرجاع النسخة الاحتياطية بنجاح!\n\n" +
                            "يرجى إعادة تشغيل البرنامج.",
                            "نجاح",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        // إغلاق التطبيق
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"❌ {restoreResult.Message}", "خطأ",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadAvailableBackups();
        }

        #endregion

        #region Helper Methods

        private void LoadAvailableBackups()
        {
            try
            {
                var backups = _backupService.GetAvailableBackups();
                AvailableBackups.Clear();
                foreach (var backup in backups)
                {
                    AvailableBackups.Add(backup);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل النسخ الاحتياطية: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
