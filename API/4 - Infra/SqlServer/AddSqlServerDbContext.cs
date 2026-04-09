using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Project.Entities;
using Serilog;
using SqlServer.Context;
using SqlServer.Services;
using System.Text;

namespace SqlServer;

public static class AddSqlServerDbContext
{
    /// <summary>
    /// Registra o DbContext. Ordem: InMemory → SQLite (arquivo) → SQL Server (connection string válida) → fallback InMemory.
    /// Dados financeiros ficam no Firestore (interface IFinanceStore em Repositories).
    /// </summary>
    public static IServiceCollection AddSqlServerContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("SqlServer");
        var useInMemory = configuration.GetValue<bool>("Database:UseInMemory", false);
        var useSqlite = configuration.GetValue<bool>("Database:UseSqlite", false);
        var sqliteRelativePath = configuration["Database:SqliteDatabasePath"] ?? "Data/finance.db";

        var connectionStringValid = !string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains("your_user", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);

        if (useInMemory)
        {
            services.AddDbContext<ApiServerContext>(options =>
                options.UseInMemoryDatabase("FrameworkDB")
                    .EnableSensitiveDataLogging());
        }
        else if (useSqlite)
        {
            var sqliteFullPath = Path.IsPathRooted(sqliteRelativePath)
                ? sqliteRelativePath
                : Path.GetFullPath(Path.Combine(contentRootPath, sqliteRelativePath));
            var dir = Path.GetDirectoryName(sqliteFullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var sqliteConnection = $"Data Source={sqliteFullPath}";
            services.AddDbContext<ApiServerContext>(options =>
                options.UseSqlite(sqliteConnection)
                    .EnableSensitiveDataLogging());
        }
        else if (connectionStringValid)
        {
            services.AddDbContext<ApiServerContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly("WebAPI"))
                    .EnableSensitiveDataLogging());
        }
        else
        {
            services.AddDbContext<ApiServerContext>(options =>
                options.UseInMemoryDatabase("FrameworkDB")
                    .EnableSensitiveDataLogging());
        }

        services.AddScoped<SqlServer.Interfaces.IDbContext>(provider =>
            provider.GetRequiredService<ApiServerContext>());

        services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
            })
            .AddEntityFrameworkStores<ApiServerContext>()
            .AddDefaultTokenProviders();

        var jwtKey = configuration["Authentication:JWTIssuerSigningKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT Key not found. Please configure 'Authentication:JWTIssuerSigningKey' in appsettings.json or environment variables.");
        }

        var audience = configuration["Authentication:Audience"] ?? "ApiAudience";
        var issuer = configuration["Authentication:Issuer"] ?? "ApiIssuer";

        services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(bearerOptions =>
            {
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    SaveSigninToken = true
                };
            });

        var usePersistentStore = !useInMemory && (useSqlite || connectionStringValid);
        if (usePersistentStore)
            services.AddHostedService<TimedHostedService>();

        return services;
    }
}
