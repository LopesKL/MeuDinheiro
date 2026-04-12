using Microsoft.Extensions.Configuration;

namespace WebAPI;

/// <summary>
/// Render (e outros PaaS) expõem <c>DATABASE_URL</c> em formato URI PostgreSQL.
/// Npgsql 6+ aceita esse URI como connection string; garantimos <c>sslmode=require</c> se faltar.
/// </summary>
internal static class DatabaseUrlConfigurationLoader
{
    public static void Apply(WebApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("PostgreSQL")))
            return;

        var databaseUrl =
            builder.Configuration["DATABASE_URL"]
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        if (string.IsNullOrWhiteSpace(databaseUrl))
            return;

        var cs = NormalizePostgresUri(databaseUrl.Trim());
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = cs
        });
    }

    private static string NormalizePostgresUri(string url)
    {
        if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (url.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
            return url;

        return url.Contains('?', StringComparison.Ordinal) ? $"{url}&sslmode=require" : $"{url}?sslmode=require";
    }
}
