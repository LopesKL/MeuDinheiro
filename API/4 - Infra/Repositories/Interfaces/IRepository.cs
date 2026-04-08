using System.Linq.Expressions;

namespace Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    IQueryable<T> GetAll();
    IQueryable<T> Find(Expression<Func<T, bool>> predicate);
    T Insert(T entity);
    Task<T> InsertAsync(T entity);
    T Update(T entity);
    void Remove(T entity);
    Task DeleteAsync(int id);
    Task DeleteGuidAsync(Guid id);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
}
