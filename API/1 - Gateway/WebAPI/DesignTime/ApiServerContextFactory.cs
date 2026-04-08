using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SqlServer.Context;

namespace WebAPI.DesignTime;

/// <summary>Fábrica usada pelo CLI do EF Core (migrations) — modelo só Identity + Crud; finanças no Firestore.</summary>
public class ApiServerContextFactory : IDesignTimeDbContextFactory<ApiServerContext>
{
    public ApiServerContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApiServerContext>();
        options.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=FinanceFrameworkIdentity;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
            b => b.MigrationsAssembly("WebAPI"));
        return new ApiServerContext(options.Options);
    }
}
