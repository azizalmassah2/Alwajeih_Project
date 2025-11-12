using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    /// <summary>
    /// مستودع المستخدمين
    /// </summary>
    public class UserRepository : IRepository<User>
    {
        public IEnumerable<User> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM Users ORDER BY UserID";
            return connection.Query<User>(sql);
        }

        public User? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM Users WHERE UserID = @UserID";
            return connection.QueryFirstOrDefault<User>(sql, new { UserID = id });
        }

        public User? GetByUsername(string username)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM Users WHERE Username = @Username";
            return connection.QueryFirstOrDefault<User>(sql, new { Username = username });
        }

        public int Add(User entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedDate)
                VALUES (@Username, @PasswordHash, @Role, @IsActive, @CreatedDate);
                SELECT last_insert_rowid();";
            
            return connection.ExecuteScalar<int>(sql, new
            {
                entity.Username,
                entity.PasswordHash,
                Role = entity.Role.ToString(),
                entity.IsActive,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public bool Update(User entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = @"
                UPDATE Users 
                SET Username = @Username, 
                    PasswordHash = @PasswordHash, 
                    Role = @Role, 
                    IsActive = @IsActive,
                    LastLogin = @LastLogin
                WHERE UserID = @UserID";
            
            return connection.Execute(sql, new
            {
                entity.UserID,
                entity.Username,
                entity.PasswordHash,
                Role = entity.Role.ToString(),
                entity.IsActive,
                LastLogin = entity.LastLogin?.ToString("yyyy-MM-dd HH:mm:ss")
            }) > 0;
        }

        public bool Delete(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "DELETE FROM Users WHERE UserID = @UserID";
            return connection.Execute(sql, new { UserID = id }) > 0;
        }

        public bool UpdateLastLogin(int userId)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "UPDATE Users SET LastLogin = @LastLogin WHERE UserID = @UserID";
            return connection.Execute(sql, new 
            { 
                UserID = userId, 
                LastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
            }) > 0;
        }
    }
}
