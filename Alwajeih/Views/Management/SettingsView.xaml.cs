using System.Windows.Controls;

namespace Alwajeih.Views.Management
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            LoadViews();
        }

        private void LoadViews()
        {
            SystemSettingsFrame.Navigate(new SystemSettingsView());
            BackupFrame.Navigate(new BackupView());
            UserManagementFrame.Navigate(new UserManagementView());
            AuditLogFrame.Navigate(new AuditLogView());
        }
    }
}
