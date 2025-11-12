using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Alwajeih.ViewModels.Notifications
{
    public class ToastNotificationViewModel : INotifyPropertyChanged
    {
        private string _icon;
        private string _title;
        private string _message;
        private string _subMessage;
        private string _primaryActionText;
        private string _secondaryActionText;
        private WpfBrush _headerColor;
        private Visibility _subMessageVisibility = Visibility.Collapsed;
        private Visibility _actionsVisibility = Visibility.Collapsed;
        private Visibility _primaryActionVisibility = Visibility.Visible;
        private Visibility _secondaryActionVisibility = Visibility.Visible;

        public string Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public string SubMessage
        {
            get => _subMessage;
            set 
            { 
                _subMessage = value;
                SubMessageVisibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged(); 
            }
        }

        public string PrimaryActionText
        {
            get => _primaryActionText;
            set 
            { 
                _primaryActionText = value;
                PrimaryActionVisibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
                UpdateActionsVisibility();
                OnPropertyChanged(); 
            }
        }

        public string SecondaryActionText
        {
            get => _secondaryActionText;
            set 
            { 
                _secondaryActionText = value;
                SecondaryActionVisibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
                UpdateActionsVisibility();
                OnPropertyChanged(); 
            }
        }

        public WpfBrush HeaderColor
        {
            get => _headerColor;
            set { _headerColor = value; OnPropertyChanged(); }
        }

        public Visibility SubMessageVisibility
        {
            get => _subMessageVisibility;
            set { _subMessageVisibility = value; OnPropertyChanged(); }
        }

        public Visibility ActionsVisibility
        {
            get => _actionsVisibility;
            set { _actionsVisibility = value; OnPropertyChanged(); }
        }

        public Visibility PrimaryActionVisibility
        {
            get => _primaryActionVisibility;
            set { _primaryActionVisibility = value; OnPropertyChanged(); }
        }

        public Visibility SecondaryActionVisibility
        {
            get => _secondaryActionVisibility;
            set { _secondaryActionVisibility = value; OnPropertyChanged(); }
        }

        private void UpdateActionsVisibility()
        {
            ActionsVisibility = (PrimaryActionVisibility == Visibility.Visible || 
                               SecondaryActionVisibility == Visibility.Visible) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
