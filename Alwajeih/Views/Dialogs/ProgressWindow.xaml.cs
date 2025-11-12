using System.Windows;

namespace Alwajeih.Views.Dialogs
{
    /// <summary>
    /// نافذة عرض التقدم
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// تحديث التقدم
        /// </summary>
        public void UpdateProgress(int percentage, string message)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = percentage;
                MessageTextBlock.Text = message;
                PercentageTextBlock.Text = $"{percentage}%";
            });
        }
    }
}
