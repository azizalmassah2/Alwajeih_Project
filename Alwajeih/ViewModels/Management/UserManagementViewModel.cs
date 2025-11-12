using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;
using Alwajeih.Utilities.Helpers;
using Alwajeih.ViewModels.Base;
using Alwajeih.Services;

namespace Alwajeih.ViewModels.Management
{
    /// <summary>
    /// 👤 ViewModel لإدارة المستخدمين
    /// </summary>
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly UserRepository _userRepository;
        private readonly AuditRepository _auditRepository;
        private readonly AuthenticationService _authService;

        private ObservableCollection<User> _users;
        private User? _selectedUser;
        private string _username;
        private string _password;
        private UserRole _selectedRole = UserRole.Viewer;
        private bool _isActive = true;

        public UserManagementViewModel()
        {
            _userRepository = new UserRepository();
            _auditRepository = new AuditRepository();
            _authService = AuthenticationService.Instance;

            Users = new ObservableCollection<User>();

            AddUserCommand = new RelayCommand(ExecuteAddUser, CanExecuteAdd);
            DeleteUserCommand = new RelayCommand(ExecuteDeleteUser, CanExecuteDelete);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);

            LoadUsers();
        }

        #region Properties

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                SetProperty(ref _selectedUser, value);
                ((RelayCommand)DeleteUserCommand).RaiseCanExecuteChanged();
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged();
            }
        }

        public UserRole SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        #endregion

        #region Commands

        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanExecuteAdd(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) && 
                   !string.IsNullOrWhiteSpace(Password) &&
                   _authService.CurrentUser?.Role == UserRole.Manager;
        }

        private void ExecuteAddUser(object parameter)
        {
            try
            {
                var passwordHash = PasswordHelper.HashPassword(Password);
                var user = new User
                {
                    Username = Username,
                    PasswordHash = passwordHash,
                    Role = SelectedRole,
                    IsActive = IsActive
                };

                int userId = _userRepository.Add(user);

                _auditRepository.Add(new AuditLog
                {
                    UserID = _authService.CurrentUser?.UserID ?? 0,
                    Action = AuditAction.Create,
                    EntityType = EntityType.User,
                    EntityID = userId,
                    Details = $"إضافة مستخدم جديد: {Username}"
                });

                System.Windows.MessageBox.Show(
                    $"✅ تم إضافة المستخدم بنجاح!\n\n" +
                    $"👤 اسم المستخدم: {Username}\n" +
                    $"🔐 الصلاحية: {GetRoleText(SelectedRole)}",
                    "نجاح",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                LoadUsers();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteDelete(object parameter)
        {
            return SelectedUser != null && 
                   SelectedUser.UserID != _authService.CurrentUser?.UserID &&
                   _authService.CurrentUser?.Role == UserRole.Manager;
        }

        private void ExecuteDeleteUser(object parameter)
        {
            if (SelectedUser == null) return;

            var result = System.Windows.MessageBox.Show(
                $"هل أنت متأكد من حذف المستخدم: {SelectedUser.Username}؟",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _userRepository.Delete(SelectedUser.UserID);

                    _auditRepository.Add(new AuditLog
                    {
                        UserID = _authService.CurrentUser?.UserID ?? 0,
                        Action = AuditAction.Delete,
                        EntityType = EntityType.User,
                        EntityID = SelectedUser.UserID,
                        Details = $"حذف مستخدم: {SelectedUser.Username}"
                    });

                    System.Windows.MessageBox.Show("✅ تم حذف المستخدم بنجاح", "نجاح",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    LoadUsers();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadUsers();
        }

        #endregion

        #region Helper Methods

        private void LoadUsers()
        {
            try
            {
                var users = _userRepository.GetAll();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ خطأ في تحميل المستخدمين: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            Username = string.Empty;
            Password = string.Empty;
            SelectedRole = UserRole.Viewer;
            IsActive = true;
        }

        private string GetRoleText(UserRole role)
        {
            return role switch
            {
                UserRole.Manager => "مدير",
                UserRole.Cashier => "صراف",
                UserRole.Viewer => "عارض",
                _ => role.ToString()
            };
        }

        #endregion
    }
}
