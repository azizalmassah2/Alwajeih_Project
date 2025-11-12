using System;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.Data.Repositories;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Receipts
{
    /// <summary>
    /// 🧾 ViewModel للإيصالات
    /// </summary>
    public class ReceiptViewModel : BaseViewModel
    {
        private readonly ReceiptService _receiptService;
        private readonly ReceiptRepository _receiptRepository;
        
        private string _receiptNumber;
        private Receipt _currentReceipt;

        public ReceiptViewModel()
        {
            _receiptService = new ReceiptService();
            _receiptRepository = new ReceiptRepository();
            SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
            PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
        }

        public string ReceiptNumber
        {
            get => _receiptNumber;
            set
            {
                SetProperty(ref _receiptNumber, value);
                ((RelayCommand)SearchCommand).RaiseCanExecuteChanged();
            }
        }

        public Receipt CurrentReceipt
        {
            get => _currentReceipt;
            set
            {
                SetProperty(ref _currentReceipt, value);
                ((RelayCommand)PrintCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand PrintCommand { get; }

        private bool CanExecuteSearch(object parameter) => !string.IsNullOrWhiteSpace(ReceiptNumber);
        private bool CanExecutePrint(object parameter) => CurrentReceipt != null;

        private void ExecuteSearch(object parameter)
        {
            try
            {
                CurrentReceipt = _receiptService.GetReceiptByNumber(ReceiptNumber);
                if (CurrentReceipt == null)
                {
                    System.Windows.MessageBox.Show("⚠️ الإيصال غير موجود", "تنبيه",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecutePrint(object parameter)
        {
            System.Windows.MessageBox.Show("🖨️ وظيفة الطباعة قيد التطوير", "معلومات",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
