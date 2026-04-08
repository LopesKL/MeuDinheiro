using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using SqlServer.Interfaces;

namespace Repositories.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly IDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(IDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual IQueryable<T> GetAll()
    {
        return _dbSet;
    }

    public virtual IQueryable<T> Find(Expression<Func<T, bool>> predicate)
    {
        return _dbSet.Where(predicate);
    }

    public virtual T Insert(T entity)
    {
        return _dbSet.Add(entity).Entity;
    }

    public virtual async Task<T> InsertAsync(T entity)
    {
        var entry = await _dbSet.AddAsync(entity);
        return entry.Entity;
    }

    public virtual T Update(T entity)
    {
        _dbSet.Update(entity);
        return entity;
    }

    public virtual void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
            _dbSet.Remove(entity);
    }

    public virtual async Task DeleteGuidAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
            _dbSet.Remove(entity);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
