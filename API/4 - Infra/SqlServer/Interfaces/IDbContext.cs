using Microsoft.EntityFrameworkCore;

namespace SqlServer.Interfaces;

public interface IDbContext : IDisposable
{
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
