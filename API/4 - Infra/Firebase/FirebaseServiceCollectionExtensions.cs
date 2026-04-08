using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repositories.Interfaces;

namespace Firebase;

public static class FirebaseServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="FirestoreDb"/> e <see cref="IFinanceStore"/>.
    /// A conexão com o Firestore só é criada na primeira utilização (evita 500.30 no IIS se a ordem de falha for só credencial).
    /// </summary>
    public static IServiceCollection AddFirebaseFinanceStore(this IServiceCollection services, IConfiguration configuration)
    {
        var projectId = configuration["Firebase:ProjectId"];
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException(
                "Defina 'Firebase:ProjectId' no appsettings ou variável de ambiente Firebase__ProjectId (ID do projeto no Firebase / Google Cloud).");
        }

        var credPath = configuration["Firebase:CredentialPath"];

        services.AddSingleton(_ => CreateFirestoreDb(projectId.Trim(), credPath));
        services.AddScoped<IFinanceStore, FirestoreFinanceStore>();
        return services;
    }

    private static FirestoreDb CreateFirestoreDb(string projectId, string? credentialPathFromConfig)
    {
        ApplyCredentialPathFromConfig(credentialPathFromConfig);

        var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        var credentialOk = !string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath);
        if (!credentialOk)
        {
            throw new InvalidOperationException(
                "Firebase: credenciais não encontradas. No servidor/IIS/Azure: (1) coloque o JSON da conta de serviço em uma pasta acessível e defina " +
                "Firebase:CredentialPath com caminho absoluto, ou (2) defina a variável de ambiente GOOGLE_APPLICATION_CREDENTIALS com o caminho completo do JSON. " +
                "No Azure App Service use Application settings → New application setting (nome GOOGLE_APPLICATION_CREDENTIALS ou carregue o ficheiro e use um caminho sob D:\\home\\).");
        }

        try
        {
            return FirestoreDb.Create(projectId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Falha ao iniciar Firestore para o projeto '{projectId}'. Verifique ProjectId e se o JSON da conta de serviço tem permissão no projeto. Detalhe: {ex.Message}",
                ex);
        }
    }

    private static void ApplyCredentialPathFromConfig(string? credentialPathFromConfig)
    {
        if (string.IsNullOrWhiteSpace(credentialPathFromConfig))
            return;

        var full = Path.GetFullPath(credentialPathFromConfig);
        if (File.Exists(full))
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", full);
    }
}
