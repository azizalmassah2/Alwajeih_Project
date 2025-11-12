using System.Collections.Generic;

namespace Alwajeih.Data.Repositories
{
    /// <summary>
    /// واجهة عامة للعمليات الأساسية على قاعدة البيانات
    /// </summary>
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        T? GetById(int id);
        int Add(T entity);
        bool Update(T entity);
        bool Delete(int id);
    }
}
