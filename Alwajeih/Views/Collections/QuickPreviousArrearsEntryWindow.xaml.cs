using System.Windows;
using System.Windows.Input;
using Alwajeih.ViewModels.Collections;

namespace Alwajeih.Views.Collections
{
    public partial class QuickPreviousArrearsEntryWindow : Window
    {
        private readonly QuickPreviousArrearsEntryViewModel _viewModel;

        public QuickPreviousArrearsEntryWindow()
        {
            InitializeComponent();
            _viewModel = new QuickPreviousArrearsEntryViewModel();
            DataContext = _viewModel;
            
            // ✅ لا نغلق النافذة عند الحفظ - تبقى مفتوحة للإدخالات المتعددة
            // (يمكن للمستخدم إغلاقها يدوياً بزر "إغلاق")
            
            // التركيز على شريط البحث عند فتح النافذة
            Loaded += (s, e) => SearchTextBox.Focus();
            
            // عند الكتابة في شريط البحث، فتح ComboBox تلقائياً
            SearchTextBox.TextChanged += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text) && _viewModel.FilteredMembers?.Count > 0)
                {
                    MemberComboBox.IsDropDownOpen = true;
                }
            };
            
            // عند الكتابة في شريط البحث وضغط Enter، الانتقال للحقل التالي
            SearchTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && _viewModel.SelectedMember != null)
                {
                    MemberComboBox.IsDropDownOpen = false;
                    RemainingAmountTextBox.Focus();
                    RemainingAmountTextBox.SelectAll();
                }
            };
            
            // اختصار Enter للحفظ السريع
            RemainingAmountTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && _viewModel.SaveQuickEntryCommand.CanExecute(null))
                {
                    _viewModel.SaveQuickEntryCommand.Execute(null);
                    SearchTextBox.Focus();
                }
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
