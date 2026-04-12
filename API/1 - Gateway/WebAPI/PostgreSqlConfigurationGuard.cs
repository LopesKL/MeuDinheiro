using Microsoft.Extensions.Configuration;

namespace WebAPI;

internal static class PostgreSqlConfigurationGuard
{
    public static void Validate(IConfiguration configuration)
    {
        var usePublishedDatabase = configuration.GetValue<bool>("Database:UsePublishedDatabase", true);
        if (!usePublishedDatabase)
            return;

        var cs = configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                "Database:UsePublishedDatabase=true mas ConnectionStrings:PostgreSQL está vazio. " +
                "IIS: (1) Variável ConnectionStrings__PostgreSQL no site, ou (2) ficheiro postgres-connection.txt na pasta da app (uma linha Npgsql), ou (3) appsettings.Secrets.json. " +
                "Modelo: postgres-connection.example.txt");
        }

        string[] placeholders =
        [
            "COLE_AQUI", "COLOQUE_", "COPIE_", "SUA_SENHA", "PREENCHA", "REPLACE_", "YOUR_PASSWORD", "TODO_PASSWORD", "PASTE_"
        ];

        foreach (var p in placeholders)
        {
            if (cs.Contains(p, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"ConnectionStrings:PostgreSQL ainda contém \"{p}\" — use a senha real do painel (Render/Azure).");
            }
        }
    }
}
