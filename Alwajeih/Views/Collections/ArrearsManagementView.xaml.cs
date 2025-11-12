using System.Windows.Controls;

namespace Alwajeih.Views.Collections
{
    public partial class ArrearsManagementView : UserControl
    {
        public ArrearsManagementView()
        {
            InitializeComponent();
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // ترقيم الصفوف تلقائياً
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
