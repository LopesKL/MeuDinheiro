using Microsoft.Extensions.DependencyInjection;
using Repositories.Interfaces;
using Repositories.Repositories;

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
}
