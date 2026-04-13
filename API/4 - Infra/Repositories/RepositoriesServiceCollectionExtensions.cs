using Microsoft.Extensions.DependencyInjection;
using Repositories.Interfaces;
using Repositories.Repositories;
using SqlServer.Context;

namespace Repositories;

public static class RepositoriesServiceCollectionExtensions
{
    /// <summary>Regista <see cref="IFinanceStore"/> em memória (dados por instância do processo; reinício apaga).</summary>
    public static IServiceCollection AddMemoryFinanceStore(this IServiceCollection services)
    {
        services.AddSingleton<FinanceStoreState>();
        services.AddScoped<IFinanceStore, MemoryFinanceStore>();
        return services;
    }

    /// <summary>Regista <see cref="IFinanceStore"/> sobre PostgreSQL/SQLite via <see cref="ApiServerContext"/>.</summary>
    public static IServiceCollection AddEfFinanceStore(this IServiceCollection services)
    {
        services.AddScoped<IFinanceStore, EfFinanceStore>();
        return services;
    }
}
