using System;
using System.Collections.Generic;
using Alwajeih.Data;
using Alwajeih.Models;
using Dapper;

namespace Alwajeih.Data.Repositories
{
    /// <summary>
    /// مستودع الأعضاء
    /// </summary>
    public class MemberRepository : IRepository<Member>
    {
        public IEnumerable<Member> GetAll()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM Members WHERE IsArchived = 0 ORDER BY Name";
            return connection.Query<Member>(sql);
        }

        public IEnumerable<Member> GetActive()
        {
            return GetAll(); // Same as GetAll - returns non-archived members
        }

        public IEnumerable<Member> GetAllIncludingArchived()
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM Members ORDER BY Name";
            return connection.Query<Member>(sql);
        }

        public Member? GetById(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "SELECT * FROM Members WHERE MemberID = @MemberID";
            return connection.QueryFirstOrDefault<Member>(sql, new { MemberID = id });
        }

        public int Add(Member entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                INSERT INTO Members (Name, Phone, MemberType, CreatedDate, IsArchived, CreatedBy)
                VALUES (@Name, @Phone, @MemberType, @CreatedDate, @IsArchived, @CreatedBy);
                SELECT last_insert_rowid();";

            return connection.ExecuteScalar<int>(
                sql,
                new
                {
                    entity.Name,
                    entity.Phone,
                    MemberType = entity.MemberType.ToString(),
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    entity.IsArchived,
                    entity.CreatedBy,
                }
            );
        }

        public bool Update(Member entity)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                UPDATE Members 
                SET Name = @Name, 
                    Phone = @Phone,
                    MemberType = @MemberType,
                    IsArchived = @IsArchived
                WHERE MemberID = @MemberID";

            return connection.Execute(sql, new
            {
                entity.MemberID,
                entity.Name,
                entity.Phone,
                MemberType = entity.MemberType.ToString(),
                entity.IsArchived
            }) > 0;
        }

        public bool Delete(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "DELETE FROM Members WHERE MemberID = @MemberID";
            return connection.Execute(sql, new { MemberID = id }) > 0;
        }

        public bool Archive(int id)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql = "UPDATE Members SET IsArchived = 1 WHERE MemberID = @MemberID";
            return connection.Execute(sql, new { MemberID = id }) > 0;
        }

        public IEnumerable<Member> Search(string searchTerm)
        {
            using var connection = DatabaseContext.CreateConnection();
            string sql =
                @"
                SELECT * FROM Members 
                WHERE IsArchived = 0 
                AND (Name LIKE @SearchTerm OR Phone LIKE @SearchTerm)
                ORDER BY Name";
            return connection.Query<Member>(sql, new { SearchTerm = $"%{searchTerm}%" });
        }
    }
}
