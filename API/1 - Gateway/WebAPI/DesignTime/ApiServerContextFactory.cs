using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SqlServer.Context;

namespace WebAPI.DesignTime;

/// <summary>Fábrica usada pelo CLI do EF Core (migrations) — Identity + Crud; finanças ficam fora deste contexto.</summary>
public class ApiServerContextFactory : IDesignTimeDbContextFactory<ApiServerContext>
{
    public ApiServerContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                "Host=127.0.0.1;Port=5432;Database=FinanceFrameworkIdentity;Username=postgres;Password=postgres;SSL Mode=Disable";
        }

        var options = new DbContextOptionsBuilder<ApiServerContext>();
        options.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly("WebAPI"));
        return new ApiServerContext(options.Options);
    }
}
