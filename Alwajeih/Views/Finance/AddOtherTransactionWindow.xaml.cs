using System.Windows;
using Alwajeih.ViewModels.Finance;

namespace Alwajeih.Views.Finance
{
    /// <summary>
    /// Interaction logic for AddOtherTransactionWindow.xaml
    /// </summary>
    public partial class AddOtherTransactionWindow : Window
    {
        public AddOtherTransactionWindow()
        {
            InitializeComponent();
            
            // الاشتراك في حدث النجاح لإغلاق النافذة
            if (DataContext is AddOtherTransactionViewModel viewModel)
            {
                viewModel.OnSaveSuccess += () =>
                {
                    DialogResult = true;
                    Close();
                };
            }
        }
    }
}
