using System.Collections;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace WebAPI;

/// <summary>
/// Preenche e normaliza <c>ConnectionStrings:PostgreSQL</c> a partir de variáveis de ambiente (prioridade),
/// <see cref="IConfiguration"/> e variáveis estilo libpq. Adequado a Render, Docker e IIS.
/// </summary>
internal static class DatabaseUrlConfigurationLoader
{
    private static readonly string[] EnvUriKeys =
    [
        "DATABASE_URL",
        "POSTGRES_URL",
        "POSTGRESQL_URL",
        "DATABASE_URI",
    ];

    public static void Apply(WebApplicationBuilder builder)
    {
        if (!IsConnectionStringUnset(builder.Configuration))
            return;

        var resolved = ResolvePostgresConnectionString(builder.Configuration);
        if (string.IsNullOrWhiteSpace(resolved))
            return;

        RegisterConnectionString(builder, resolved);
    }

    /// <summary>
    /// Remove espaços, aspas e BOM acidental em valores vindos de painéis (Render, Azure) ou ficheiros.
    /// </summary>
    internal static string? TrimConfigurationValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;
        var s = value.Trim();
        if (s.Length > 0 && s[0] == '\uFEFF')
            s = s.TrimStart('\uFEFF').Trim();
        while (s.Length >= 2
               && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            s = s[1..^1].Trim();
        return s;
    }

    /// <summary>
    /// Garante <c>sslmode=require</c> em URIs <c>postgres(ql)://</c> quando o host o exige (ex.: Render externo).
    /// </summary>
    internal static string NormalizePostgresUri(string url)
    {
        if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (url.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
            return url;

        return url.Contains('?', StringComparison.Ordinal) ? $"{url}&sslmode=require" : $"{url}?sslmode=require";
    }

    /// <summary>
    /// Última passagem: normaliza aspas/URI já presente em <c>ConnectionStrings:PostgreSQL</c> (ex.: só via <c>ConnectionStrings__PostgreSQL</c> no env).
    /// </summary>
    public static void ApplySanitizationToExistingConnectionString(WebApplicationBuilder builder)
    {
        var raw = builder.Configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var trimmed = TrimConfigurationValue(raw);
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        var final = IsPostgresUri(trimmed)
            ? NormalizePostgresUri(trimmed)
            : trimmed;

        if (string.Equals(raw, final, StringComparison.Ordinal))
            return;

        RegisterConnectionString(builder, final);
    }

    internal static PostgresEnvProbe ProbeProcessEnvironment()
    {
        string? firstUrlKey = null;
        foreach (var name in EnvUriKeys)
        {
            var v = TrimConfigurationValue(GetEnvironmentVariableCaseInsensitive(name));
            if (!string.IsNullOrWhiteSpace(v))
            {
                firstUrlKey = name;
                break;
            }
        }

        var hasDatabaseUrl = firstUrlKey != null;
        var hasConnStrEnv = !string.IsNullOrWhiteSpace(
            GetEnvironmentVariableCaseInsensitive("ConnectionStrings__PostgreSQL"));
        var hasPgLib = !string.IsNullOrWhiteSpace(GetEnvironmentVariableCaseInsensitive("PGHOST"))
                      && !string.IsNullOrWhiteSpace(GetEnvironmentVariableCaseInsensitive("PGDATABASE"));

        return new PostgresEnvProbe(hasDatabaseUrl, hasConnStrEnv, hasPgLib, firstUrlKey);
    }

    private static bool IsConnectionStringUnset(IConfiguration configuration) =>
        string.IsNullOrWhiteSpace(configuration.GetConnectionString("PostgreSQL"));

    private static void RegisterConnectionString(WebApplicationBuilder builder, string value) =>
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = value.Trim()
        });

    private static string? ResolvePostgresConnectionString(IConfiguration configuration)
    {
        var fromEnv = FirstNonEmpty(EnvUriKeys.Select(k => TrimConfigurationValue(GetEnvironmentVariableCaseInsensitive(k))));
        var uri = FirstNonEmpty(fromEnv);

        if (string.IsNullOrWhiteSpace(uri))
        {
            uri = FirstNonEmpty(
                TrimConfigurationValue(configuration["DATABASE_URL"]),
                TrimConfigurationValue(configuration["POSTGRES_URL"]),
                TrimConfigurationValue(configuration["POSTGRESQL_URL"]));
        }

        if (!string.IsNullOrWhiteSpace(uri))
            return NormalizePostgresUri(uri.Trim());

        return BuildFromLibpqEnv();
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

    private static string? FirstNonEmpty(IEnumerable<string?> values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }

        return null;
    }

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

    private static bool IsPostgresUri(string s) =>
        s.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || s.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Dados não sensíveis para log de diagnóstico no arranque (ex.: Render). FirstUrlEnvKey = nome da variável, nunca o URL.</summary>
internal readonly record struct PostgresEnvProbe(
    bool HasDatabaseUrlLikeEnv,
    bool HasConnectionStringsPostgreSQLEnv,
    bool HasPgLibEnv,
    string? FirstUrlEnvKey);
