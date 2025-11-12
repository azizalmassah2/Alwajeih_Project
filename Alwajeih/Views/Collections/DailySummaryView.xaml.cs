using System.Windows.Controls;
using Alwajeih.ViewModels.Collections;

namespace Alwajeih.Views.Collections
{
    public partial class DailySummaryView : UserControl
    {
        public DailySummaryView()
        {
            InitializeComponent();
            DataContext = new DailySummaryViewModel();
        }
        
        public DailySummaryView(int weekNumber, int dayNumber)
        {
            InitializeComponent();
            var viewModel = new DailySummaryViewModel();
            viewModel.LoadDailySummary(weekNumber, dayNumber);
            DataContext = viewModel;
        }
    }
}
