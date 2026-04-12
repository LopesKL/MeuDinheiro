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
                "Render: ligue o addon PostgreSQL ao Web Service (injeta DATABASE_URL) ou defina ConnectionStrings__PostgreSQL. " +
                "Outros: variável ConnectionStrings__PostgreSQL, ficheiro postgres-connection.txt, ou appsettings.Secrets.json. " +
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
