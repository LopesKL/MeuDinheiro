using Project.Entities;

namespace Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<AppUser> AppUserRepository { get; }
    IRepository<AppRole> AppRoleRepository { get; }
    IRepository<Crud> CrudRepository { get; }
    Task Save();
}
