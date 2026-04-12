using System.Collections;
using Microsoft.Extensions.Configuration;

namespace WebAPI;

/// <summary>
/// Preenche ConnectionStrings:PostgreSQL a partir de variáveis típicas de PaaS (Render, etc.).
/// Lê primeiro o ambiente do processo (Environment) e só depois IConfiguration — evita falhas de ordem/cadeia.
/// </summary>
internal static class DatabaseUrlConfigurationLoader
{
    public static void Apply(WebApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("PostgreSQL")))
            return;

        var resolved = ResolvePostgresConnectionString(builder.Configuration);
        if (string.IsNullOrWhiteSpace(resolved))
            return;

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = resolved.Trim()
        });
    }

    private static string? ResolvePostgresConnectionString(IConfiguration configuration)
    {
        // 1) Processo (Render injeta aqui no arranque do container)
        var uri =
            FirstNonEmpty(
                TrimUriEnv(GetEnvironmentVariableCaseInsensitive("DATABASE_URL")),
                TrimUriEnv(GetEnvironmentVariableCaseInsensitive("POSTGRES_URL")),
                TrimUriEnv(GetEnvironmentVariableCaseInsensitive("DATABASE_URI")));

        // 2) IConfiguration (ficheiros + env já carregados pelo host)
        if (string.IsNullOrWhiteSpace(uri))
        {
            uri = FirstNonEmpty(
                TrimUriEnv(configuration["DATABASE_URL"]),
                TrimUriEnv(configuration["POSTGRES_URL"]));
        }

        if (!string.IsNullOrWhiteSpace(uri))
            return NormalizePostgresUri(uri.Trim());

        return BuildFromLibpqEnv();
    }

    /// <summary>Remove aspas que por vezes ficam ao copiar/colar no painel do Render.</summary>
    private static string? TrimUriEnv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;
        var s = value.Trim();
        if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            s = s[1..^1].Trim();
        return s;
    }

    private static string? GetEnvironmentVariableCaseInsensitive(string name)
    {
        foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
        {
            if (e.Key is string k && string.Equals(k, name, StringComparison.OrdinalIgnoreCase))
                return e.Value?.ToString();
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }

        return null;
    }

    /// <summary>Host, user, password, database obrigatórios (estilo libpq / muitos templates Docker).</summary>
    private static string? BuildFromLibpqEnv()
    {
        var host = GetEnvironmentVariableCaseInsensitive("PGHOST");
        var user = GetEnvironmentVariableCaseInsensitive("PGUSER");
        var password = GetEnvironmentVariableCaseInsensitive("PGPASSWORD");
        var database = GetEnvironmentVariableCaseInsensitive("PGDATABASE");
        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(database))
            return null;

        var port = GetEnvironmentVariableCaseInsensitive("PGPORT");
        if (string.IsNullOrWhiteSpace(port))
            port = "5432";

        return
            $"Host={host};Port={port};Username={user};Password={password};Database={database};SSL Mode=Require;Trust Server Certificate=true";
    }

    internal static string NormalizePostgresUri(string url)
    {
        if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (url.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
            return url;

        return url.Contains('?', StringComparison.Ordinal) ? $"{url}&sslmode=require" : $"{url}?sslmode=require";
    }
}
