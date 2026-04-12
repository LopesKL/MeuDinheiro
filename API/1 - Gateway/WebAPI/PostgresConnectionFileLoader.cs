using Microsoft.Extensions.Configuration;

namespace WebAPI;

/// <summary>
/// IIS: se <c>ConnectionStrings__PostgreSQL</c> não estiver definida, lê uma única linha do ficheiro
/// <c>postgres-connection.txt</c> na pasta da aplicação (ao lado de WebAPI.dll / web.config).
/// </summary>
internal static class PostgresConnectionFileLoader
{
    public const string FileName = "postgres-connection.txt";

    public static void Apply(WebApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("PostgreSQL")))
            return;

        var searchRoots = new[]
        {
            builder.Environment.ContentRootPath,
            AppContext.BaseDirectory
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        string? path = null;
        foreach (var root in searchRoots)
        {
            if (string.IsNullOrEmpty(root))
                continue;
            var candidate = Path.Combine(root, FileName);
            if (File.Exists(candidate))
            {
                path = candidate;
                break;
            }
        }

        if (path == null)
            return;

        var line = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .FirstOrDefault(l =>
                l.Length > 0
                && !l.StartsWith("#", StringComparison.Ordinal)
                && l.Contains("Host=", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(line))
            return;

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = line
        });
    }
}
