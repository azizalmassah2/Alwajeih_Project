using System;
using System.Data.SQLite;
using Alwajeih.Models;

namespace Alwajeih.Data.Repositories
{
    /// <summary>
    /// Repository لإدارة إعدادات النظام
    /// </summary>
    public class SystemSettingsRepository
    {
        /// <summary>
        /// الحصول على الإعدادات الحالية
        /// </summary>
        public SystemSettings? GetCurrentSettings()
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            string query = @"
                SELECT * FROM SystemSettings 
                ORDER BY SettingID DESC 
                LIMIT 1";
            
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();
            
            if (reader.Read())
            {
                return new SystemSettings
                {
                    SettingID = reader.GetInt32(0),
                    StartDate = DateTime.Parse(reader.GetString(1)),
                    EndDate = DateTime.Parse(reader.GetString(2)),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    CreatedBy = reader.GetInt32(4)
                };
            }
            
            return null;
        }
        
        /// <summary>
        /// حفظ إعدادات جديدة
        /// </summary>
        public bool SaveSettings(DateTime startDate, DateTime endDate, int userId)
        {
            using var connection = DatabaseContext.CreateConnection();
            connection.Open();
            
            string query = @"
                INSERT INTO SystemSettings (StartDate, EndDate, CreatedBy)
                VALUES (@StartDate, @EndDate, @CreatedBy)";
            
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@CreatedBy", userId);
            
            return command.ExecuteNonQuery() > 0;
        }
    }
}
