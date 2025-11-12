using System.Windows;
using System.Windows.Controls;
using Alwajeih.ViewModels.Collections;

namespace Alwajeih.Views.Collections
{
    public partial class DailyCollectionView : UserControl
    {
        public DailyCollectionView()
        {
            InitializeComponent();
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // ترقيم الصفوف تلقائياً
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void QuickEntryButton_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة الإدخال السريع
            var quickEntryWindow = new QuickPreviousArrearsEntryWindow();
            bool? result = quickEntryWindow.ShowDialog();
            
            // إذا تم الحفظ بنجاح، تحديث البيانات
            if (result == true && DataContext is DailyCollectionViewModel viewModel)
            {
                // تحديث قائمة السابقات
                viewModel.RefreshCommand.Execute(null);
            }
        }
    }
}
