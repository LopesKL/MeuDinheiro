using Notifications.Notifications;
using Project.Entities;
using Repositories.Interfaces;
using SqlServer.Interfaces;

namespace Repositories.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContext _context;
    private readonly INotificationHandler _notification;
    private IRepository<AppUser>? _appUserRepository;
    private IRepository<AppRole>? _appRoleRepository;
    private IRepository<Crud>? _crudRepository;

    public UnitOfWork(IDbContext context, INotificationHandler notification)
    {
        _context = context;
        _notification = notification;
    }

    public IRepository<AppUser> AppUserRepository =>
        _appUserRepository ??= new Repository<AppUser>(_context);

    public IRepository<AppRole> AppRoleRepository =>
        _appRoleRepository ??= new Repository<AppRole>(_context);

    public IRepository<Crud> CrudRepository =>
        _crudRepository ??= new Repository<Crud>(_context);

    public async Task Save()
    {
        if (_notification.HasNotification())
            return;

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
