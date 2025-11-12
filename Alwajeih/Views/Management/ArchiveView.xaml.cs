using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Alwajeih.Data;

namespace Alwajeih.Views.Management
{
    public partial class ArchiveView : UserControl
    {
        private ObservableCollection<ArchiveInfo> Archives { get; set; }

        public ArchiveView()
        {
            InitializeComponent();
            Archives = new ObservableCollection<ArchiveInfo>();
            ArchivesDataGrid.ItemsSource = Archives;
            LoadCurrentInfo();
            LoadArchives();
        }

        private void LoadCurrentInfo()
        {
            try
            {
                using (var connection = DatabaseContext.CreateConnection())
                {
                    connection.Open();
                    string query = "SELECT StartDate, EndDate FROM SystemSettings ORDER BY SettingID DESC LIMIT 1";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            DateTime startDate = DateTime.Parse(reader["StartDate"].ToString());
                            DateTime endDate = DateTime.Parse(reader["EndDate"].ToString());
                            
                            CurrentStartDateText.Text = startDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("ar"));
                            CurrentEndDateText.Text = endDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("ar"));
                            
                            if (DateTime.Now > endDate)
                            {
                                CurrentStatusText.Text = "🔴 منتهية";
                                CurrentStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DC2626"));
                            }
                            else
                            {
                                CurrentStatusText.Text = "🟢 نشطة";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المعلومات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadArchives()
        {
            try
            {
                Archives.Clear();
                
                string archivesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archives");
                
                if (Directory.Exists(archivesPath))
                {
                    var files = Directory.GetFiles(archivesPath, "*.db");
                    
                    foreach (var file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        
                        Archives.Add(new ArchiveInfo
                        {
                            FileName = System.IO.Path.GetFileNameWithoutExtension(file),
                            FilePath = file,
                            StartDate = DateTime.Now.AddMonths(-6), // يمكن قراءتها من الملف
                            EndDate = DateTime.Now,
                            ArchiveDate = fileInfo.CreationTime,
                            FileSize = FormatFileSize(fileInfo.Length)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الأرشيفات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void ArchiveAndStartNew_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ تحذير: هذه العملية ستقوم بـ:\n\n" +
                "1. أرشفة جميع البيانات الحالية\n" +
                "2. إنشاء قاعدة بيانات جديدة فارغة\n" +
                "3. حذف جميع البيانات من القاعدة الحالية\n\n" +
                "هل أنت متأكد من المتابعة؟\n\n" +
                "تأكد من أخذ نسخة احتياطية أولاً!",
                "تأكيد الأرشفة",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // إنشاء مجلد الأرشيف
                    string archivesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archives");
                    if (!Directory.Exists(archivesPath))
                    {
                        Directory.CreateDirectory(archivesPath);
                    }

                    // اسم ملف الأرشيف
                    string archiveFileName = $"Archive_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                    string archiveFilePath = System.IO.Path.Combine(archivesPath, archiveFileName);

                    // نسخ قاعدة البيانات الحالية
                    string currentDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alwajeih.db");
                    File.Copy(currentDbPath, archiveFilePath, true);

                    // حذف البيانات من القاعدة الحالية (الاحتفاظ بالهيكل فقط)
                    using (var connection = DatabaseContext.CreateConnection())
                    {
                        connection.Open();
                        
                        string[] tables = { 
                            "DailyCollections", "Arrears", "ExternalPayments", 
                            "WeeklyReconciliations", "VaultTransactions", 
                            "SavingPlans", "Members", "SystemSettings"
                        };

                        foreach (var table in tables)
                        {
                            string deleteQuery = $"DELETE FROM {table}";
                            using (var command = new SQLiteCommand(deleteQuery, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    MessageBox.Show(
                        "✅ تمت الأرشفة بنجاح!\n\n" +
                        $"تم حفظ الأرشيف في:\n{archiveFilePath}\n\n" +
                        "يمكنك الآن بدء جمعية جديدة من الإعدادات.",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    LoadArchives();
                    LoadCurrentInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الأرشفة: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshArchives_Click(object sender, RoutedEventArgs e)
        {
            LoadArchives();
            MessageBox.Show("تم تحديث قائمة الأرشيفات", "معلومات", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenArchive_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var archive = button?.DataContext as ArchiveInfo;
            
            if (archive != null)
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{archive.FilePath}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في فتح الملف: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteArchive_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var archive = button?.DataContext as ArchiveInfo;
            
            if (archive != null)
            {
                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف الأرشيف:\n{archive.FileName}؟\n\nهذه العملية لا يمكن التراجع عنها!",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Delete(archive.FilePath);
                        LoadArchives();
                        MessageBox.Show("تم حذف الأرشيف بنجاح", "نجاح", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في حذف الأرشيف: {ex.Message}", "خطأ", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    public class ArchiveInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime ArchiveDate { get; set; }
        public string FileSize { get; set; }
    }
}
