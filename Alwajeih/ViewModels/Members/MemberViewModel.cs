using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Alwajeih.Data.Repositories;
using Alwajeih.Models;
using Alwajeih.Services;
using Alwajeih.ViewModels.Base;

namespace Alwajeih.ViewModels.Members
{
    /// <summary>
    /// 👥 ViewModel لإدارة الأعضاء
    /// </summary>
    public class MemberViewModel : BaseViewModel
    {
        private readonly MemberService _memberService;
        private readonly MemberRepository _memberRepository;
        private readonly SavingPlanService _planService;
        private readonly SavingPlanRepository _planRepository;
        private readonly SystemSettingsRepository _settingsRepository;
        private readonly AuthenticationService _authService;

        private ObservableCollection<Member> _members;
        private Member? _selectedMember;
        private string _searchText;
        private bool _showArchived;

        // خصائص العضو الجديد/المحرر
        private string _name;
        private string _idNumber;
        private string _phone;
        private string _address;
        private decimal _dailyAmount;
        private MemberType _memberType = MemberType.Regular;
        private CollectionFrequency _collectionFrequency = CollectionFrequency.Daily;
        private bool _isEditMode;

        public MemberViewModel()
        {
            _memberService = new MemberService();
            _memberRepository = new MemberRepository();
            _planService = new SavingPlanService();
            _planRepository = new SavingPlanRepository();
            _settingsRepository = new SystemSettingsRepository();
            _authService = AuthenticationService.Instance;

            Members = new ObservableCollection<Member>();

            // الأوامر
            AddMemberCommand = new RelayCommand(ExecuteAddMember, CanExecuteAddMember);
            EditMemberCommand = new RelayCommand(ExecuteEditMember, CanExecuteEdit);
            SaveMemberCommand = new RelayCommand(ExecuteSaveMember, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel, _ => true);
            ArchiveMemberCommand = new RelayCommand(ExecuteArchiveMember, CanExecuteEdit);
            SearchCommand = new RelayCommand(ExecuteSearch, _ => true);
            RefreshCommand = new RelayCommand(ExecuteRefresh, _ => true);
            ViewStatementCommand = new RelayCommand(ExecuteViewStatement, CanExecuteEdit);

            LoadMembers();
        }

        #region Properties

        public ObservableCollection<Member> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        public Member? SelectedMember
        {
            get => _selectedMember;
            set
            {
                SetProperty(ref _selectedMember, value);
                ((RelayCommand)EditMemberCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ArchiveMemberCommand).RaiseCanExecuteChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                // بحث فوري عند الكتابة
                ExecuteSearch(null);
            }
        }

        public bool ShowArchived
        {
            get => _showArchived;
            set
            {
                SetProperty(ref _showArchived, value);
                LoadMembers();
            }
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string IdNumber
        {
            get => _idNumber;
            set => SetProperty(ref _idNumber, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public decimal DailyAmount
        {
            get => _dailyAmount;
            set
            {
                SetProperty(ref _dailyAmount, value);
                ((RelayCommand)SaveMemberCommand).RaiseCanExecuteChanged();
            }
        }

        public MemberType MemberType
        {
            get => _memberType;
            set
            {
                if (SetProperty(ref _memberType, value))
                {
                    OnPropertyChanged(nameof(MemberTypeIndex));
                }
            }
        }

        public int MemberTypeIndex
        {
            get => (int)_memberType;
            set
            {
                _memberType = (MemberType)value;
                OnPropertyChanged(nameof(MemberType));
                OnPropertyChanged(nameof(MemberTypeIndex));
                OnPropertyChanged(nameof(IsBehindAssociationMember));
                OnPropertyChanged(nameof(ShowDailyAmountField));
            }
        }
        
        /// <summary>
        /// للتحكم في إظهار/إخفاء حقول المبلغ اليومي ونوع التحصيل
        /// </summary>
        public bool IsBehindAssociationMember => _memberType == MemberType.BehindAssociation;
        
        /// <summary>
        /// للتحكم في إظهار حقل المبلغ اليومي
        /// يظهر فقط عند الإضافة (ليس في وضع التعديل) وللأعضاء الأساسيين فقط
        /// </summary>
        public bool ShowDailyAmountField => !_isEditMode && _memberType != MemberType.BehindAssociation;

        public CollectionFrequency CollectionFrequency
        {
            get => _collectionFrequency;
            set
            {
                if (SetProperty(ref _collectionFrequency, value))
                {
                    OnPropertyChanged(nameof(CollectionFrequencyIndex));
                }
            }
        }

        public int CollectionFrequencyIndex
        {
            get => (int)_collectionFrequency;
            set
            {
                _collectionFrequency = (CollectionFrequency)value;
                OnPropertyChanged(nameof(CollectionFrequency));
                OnPropertyChanged(nameof(CollectionFrequencyIndex));
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(ShowDailyAmountField));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand AddMemberCommand { get; }
        public ICommand EditMemberCommand { get; }
        public ICommand SaveMemberCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ArchiveMemberCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewStatementCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanExecuteAddMember(object parameter)
        {
            return _authService.HasPermission("AddMember");
        }

        private void ExecuteAddMember(object parameter)
        {
            ClearForm();
            IsEditMode = false;
        }

        private bool CanExecuteEdit(object parameter)
        {
            return SelectedMember != null && _authService.HasPermission("EditMember");
        }

        private void ExecuteEditMember(object parameter)
        {
            if (SelectedMember == null)
                return;

            Name = SelectedMember.Name;
            Phone = SelectedMember.Phone;
            MemberType = SelectedMember.MemberType;
            
            // تحميل نوع التحصيل من السهم النشط
            var activePlan = _planService.GetActivePlansForMember(SelectedMember.MemberID).FirstOrDefault();
            if (activePlan != null)
            {
                CollectionFrequency = activePlan.CollectionFrequency;
            }
            
            IsEditMode = true;
        }

        private bool CanExecuteSave(object parameter)
        {
            // عند الإضافة: يجب إدخال الاسم والمبلغ اليومي
            // عند التعديل: يجب إدخال الاسم فقط
            if (IsEditMode)
            {
                return !string.IsNullOrWhiteSpace(Name);
            }
            else
            {
                return !string.IsNullOrWhiteSpace(Name) && DailyAmount > 0;
            }
        }

        private void ExecuteSaveMember(object parameter)
        {
            try
            {
                var userId = _authService.CurrentUser?.UserID ?? 0;

                if (IsEditMode && SelectedMember != null)
                {
                    // تحديث
                    SelectedMember.Name = Name;
                    SelectedMember.Phone = Phone;
                    SelectedMember.MemberType = MemberType;

                    var result = _memberService.UpdateMember(SelectedMember, userId);
                    if (result.Success)
                    {
                        // تحديث نوع التحصيل في السهم النشط
                        var activePlan = _planService.GetActivePlansForMember(SelectedMember.MemberID).FirstOrDefault();
                        if (activePlan != null && activePlan.CollectionFrequency != CollectionFrequency)
                        {
                            activePlan.CollectionFrequency = CollectionFrequency;
                            _planRepository.Update(activePlan);
                        }
                        
                        System.Windows.MessageBox.Show(
                            "✅ تم تحديث العضو بنجاح",
                            "نجاح",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information
                        );
                        LoadMembers();
                        ClearForm();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            $"❌ {result.Message}",
                            "خطأ",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error
                        );
                    }
                }
                else
                {
                    // إضافة جديد
                    var result = _memberService.AddMember(Name, Phone, MemberType, userId);
                    if (result.Success)
                    {
                        string memberTypeText = MemberType == MemberType.Regular ? "عضو أساسي" : "خلف الجمعية";
                        
                        // إنشاء سهم تلقائياً فقط للأعضاء الأساسيين
                        if (MemberType == MemberType.Regular)
                        {
                            var settings = _settingsRepository.GetCurrentSettings();
                            DateTime startDate = settings?.StartDate ?? DateTime.Now;
                            
                            var planResult = _planService.CreatePlan(
                                result.MemberID,
                                1, // رقم السهم الأول
                                DailyAmount,
                                startDate,
                                userId,
                                CollectionFrequency
                            );
                        
                        if (planResult.Success)
                        {
                            var endDate = settings?.EndDate ?? startDate.AddDays(182);
                            var totalAmount = DailyAmount * 182;
                            
                                string frequencyText = CollectionFrequency == CollectionFrequency.Daily ? "تحصيل يومي" : "تحصيل أسبوعي";
                                System.Windows.MessageBox.Show(
                                    $"✅ تم إضافة العضو والسهم بنجاح!\n\n" +
                                    $"👤 اسم العضو: {Name}\n" +
                                    $"📂 نوع العضو: {memberTypeText}\n" +
                                    $"🔄 نوع التحصيل: {frequencyText}\n" +
                                    $"📋 رقم السهم: 1\n" +
                                    $"💰 المبلغ اليومي: {DailyAmount:N2} ريال\n" +
                                    $"📅 تاريخ البداية: {startDate:yyyy-MM-dd}\n" +
                                    $"📅 تاريخ النهاية: {endDate:yyyy-MM-dd}\n" +
                                    $"💵 الإجمالي: {totalAmount:N2} ريال",
                                    "نجاح ✅",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Information
                                );
                        }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    $"⚠️ تم إضافة العضو لكن فشل إنشاء السهم:\n{planResult.Message}",
                                    "تحذير",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Warning
                                );
                            }
                        }
                        else
                        {
                            // خلف الجمعية - لا يحتاج سهم
                            System.Windows.MessageBox.Show(
                                $"✅ تم إضافة العضو بنجاح!\n\n" +
                                $"👤 اسم العضو: {Name}\n" +
                                $"📂 نوع العضو: {memberTypeText}\n" +
                                $"💰 المبلغ اليومي: {DailyAmount:N2} ريال\n\n" +
                                $"ℹ️ هذا العضو من نوع (خلف الجمعية)\n" +
                                $"يمكنه إيداع وسحب أمواله متى شاء",
                                "نجاح ✅",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information
                            );
                        }
                        
                        LoadMembers();
                        ClearForm();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            $"❌ {result.Message}",
                            "خطأ",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void ExecuteCancel(object parameter)
        {
            ClearForm();
        }

        private void ExecuteArchiveMember(object parameter)
        {
            if (SelectedMember == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"هل أنت متأكد من أرشفة العضو: {SelectedMember.Name}؟",
                "تأكيد الأرشفة",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var userId = _authService.CurrentUser?.UserID ?? 0;
                var archiveResult = _memberService.ArchiveMember(SelectedMember.MemberID, userId);

                if (archiveResult.Success)
                {
                    System.Windows.MessageBox.Show(
                        "✅ تم أرشفة العضو بنجاح",
                        "نجاح",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );
                    LoadMembers();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"❌ {archiveResult.Message}",
                        "خطأ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                }
            }
        }

        private void ExecuteSearch(object parameter)
        {
            LoadMembers();
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadMembers();
        }

        private void ExecuteViewStatement(object parameter)
        {
            if (SelectedMember == null)
                return;

            // فتح نافذة كشف الحساب
            var statementWindow = new System.Windows.Window
            {
                Title = $"كشف حساب - {SelectedMember.Name}",
                Width = 1000,
                Height = 700,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                Content = new Views.Members.MemberStatementView
                {
                    DataContext = new MemberStatementViewModel(SelectedMember.MemberID)
                }
            };
            statementWindow.ShowDialog();
        }

        #endregion

        #region Helper Methods

        private void LoadMembers()
        {
            try
            {
                var members = ShowArchived
                    ? _memberRepository.GetAll()
                    : _memberRepository.GetActive();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    members = members.Where(m =>
                        m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                        || (m.Phone != null && m.Phone.Contains(SearchText))
                    );
                }

                // ترتيب حسب MemberID
                members = members.OrderBy(m => m.MemberID);

                Members.Clear();
                foreach (var member in members)
                {
                    // تحميل نوع التحصيل من السهم النشط للعضو
                    var activePlan = _planService.GetActivePlansForMember(member.MemberID).FirstOrDefault();
                    if (activePlan != null)
                    {
                        member.CollectionFrequency = activePlan.CollectionFrequency;
                    }
                    
                    Members.Add(member);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"❌ خطأ في تحميل الأعضاء: {ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void ClearForm()
        {
            Name = string.Empty;
            Phone = string.Empty;
            DailyAmount = 0;
            MemberType = MemberType.Regular;
            CollectionFrequency = CollectionFrequency.Daily;
            IsEditMode = false;
            SelectedMember = null;
        }

        #endregion
    }
}
