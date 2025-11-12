using System.Windows.Controls;

namespace Alwajeih.Views.SavingPlans
{
    public partial class SavingPlanView : UserControl
    {
        public SavingPlanView()
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
