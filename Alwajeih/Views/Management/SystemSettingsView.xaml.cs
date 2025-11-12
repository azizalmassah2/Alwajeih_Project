using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using Alwajeih.Data;

namespace Alwajeih.Views.Management
{
    public partial class SystemSettingsView : UserControl
    {
        public SystemSettingsView()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
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
                            
                            StartDatePicker.SelectedDate = startDate;
                            EndDatePicker.SelectedDate = endDate;
                            
                            UpdateStatistics(startDate, endDate);
                        }
                        else
                        {
                            // إعدادات افتراضية
                            DateTime defaultStart = new DateTime(2025, 8, 30);
                            DateTime defaultEnd = new DateTime(2026, 2, 28);
                            
                            StartDatePicker.SelectedDate = defaultStart;
                            EndDatePicker.SelectedDate = defaultEnd;
                            
                            UpdateStatistics(defaultStart, defaultEnd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الإعدادات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics(DateTime startDate, DateTime endDate)
        {
            // حساب الإحصائيات
            TimeSpan totalDuration = endDate - startDate;
            int totalWeeks = (int)Math.Ceiling(totalDuration.TotalDays / 7);
            
            TimeSpan elapsed = DateTime.Now - startDate;
            int elapsedWeeks = Math.Max(0, (int)Math.Floor(elapsed.TotalDays / 7));
            
            int remainingWeeks = Math.Max(0, totalWeeks - elapsedWeeks);
            
            TotalWeeksText.Text = $"{totalWeeks} أسبوع";
            ElapsedWeeksText.Text = $"{elapsedWeeks} أسبوع";
            RemainingWeeksText.Text = $"{remainingWeeks} أسبوع";
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("يرجى تحديد تاريخ البداية والنهاية", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = StartDatePicker.SelectedDate.Value;
                DateTime endDate = EndDatePicker.SelectedDate.Value;

                if (endDate <= startDate)
                {
                    MessageBox.Show("تاريخ النهاية يجب أن يكون بعد تاريخ البداية", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    "هل أنت متأكد من حفظ هذه التواريخ؟\n\n" +
                    $"البداية: {startDate:yyyy-MM-dd}\n" +
                    $"النهاية: {endDate:yyyy-MM-dd}\n\n" +
                    "تغيير التواريخ قد يؤثر على الحسابات والتقارير.",
                    "تأكيد الحفظ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = DatabaseContext.CreateConnection())
                    {
                        connection.Open();
                        
                        // تحديث أو إدخال الإعدادات
                        string query = @"
                            INSERT OR REPLACE INTO SystemSettings (SettingID, StartDate, EndDate, CreatedAt, CreatedBy)
                            VALUES (1, @StartDate, @EndDate, datetime('now'), 1)";
                        
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                            command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
                            command.ExecuteNonQuery();
                        }
                    }

                    UpdateStatistics(startDate, endDate);
                    
                    MessageBox.Show("✅ تم حفظ الإعدادات بنجاح!", "نجاح", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadSettings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            MessageBox.Show("تم إعادة تحميل الإعدادات", "معلومات", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
