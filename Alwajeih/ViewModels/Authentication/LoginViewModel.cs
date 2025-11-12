using System.Windows;
using System.Windows.Input;
using Alwajeih.Services;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Authentication
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authService;
        private string _username = "وجيه"; 
        private string _password = "123"; 
        private string _errorMessage = string.Empty;
        private bool _hasError;

        public LoginViewModel()
        {
            _authService = AuthenticationService.Instance;
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public ICommand LoginCommand { get; }

        private bool CanExecuteLogin(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteLogin(object? parameter)
        {
            HasError = false;
            ErrorMessage = string.Empty;

            bool success = _authService.Login(Username, Password);

            if (success)
            {
                // فتح النافذة الرئيسية
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // إغلاق نافذة تسجيل الدخول
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.Authentication.LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
            }
        }
    }
}
