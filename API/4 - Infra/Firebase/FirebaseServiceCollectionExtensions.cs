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
    /// <param name="contentRootPath">Raiz da aplicação (ex.: <c>IWebHostEnvironment.ContentRootPath</c>). Caminhos relativos em <c>Firebase:CredentialPath</c> são resolvidos a partir daqui.</param>
    public static IServiceCollection AddFirebaseFinanceStore(
        this IServiceCollection services,
        IConfiguration configuration,
        string? contentRootPath = null)
    {
        var projectId = configuration["Firebase:ProjectId"];
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException(
                "Defina 'Firebase:ProjectId' no appsettings ou variável de ambiente Firebase__ProjectId (ID do projeto no Firebase / Google Cloud).");
        }

        var credPath = configuration["Firebase:CredentialPath"];

        services.AddSingleton(_ => CreateFirestoreDb(projectId.Trim(), credPath, contentRootPath));
        services.AddScoped<IFinanceStore, FirestoreFinanceStore>();
        return services;
    }

    private static FirestoreDb CreateFirestoreDb(string projectId, string? credentialPathFromConfig, string? contentRootPath)
    {
        ApplyCredentialPathFromConfig(credentialPathFromConfig, contentRootPath);

        var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        var credentialOk = !string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath);
        if (!credentialOk)
        {
            throw new InvalidOperationException(
                "Firebase / Firestore: credenciais não encontradas. Coloque o JSON da conta de serviço (mesmo ficheiro que no Node com admin.credential.cert) e: " +
                "(1) defina Firebase:CredentialPath com caminho absoluto ou relativo à pasta do projeto WebAPI (ex.: secrets/service-account.json), ou " +
                "(2) defina a variável de ambiente GOOGLE_APPLICATION_CREDENTIALS com o caminho completo do JSON. " +
                "O SDK usado aqui é Google.Cloud.Firestore (equivalente funcional ao acesso ao Firestore; não é obrigatório o pacote FirebaseAdmin salvo se precisar de Firebase Auth no servidor).");
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

    private static void ApplyCredentialPathFromConfig(string? credentialPathFromConfig, string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(credentialPathFromConfig))
            return;

        var resolved = ResolveCredentialPath(credentialPathFromConfig.Trim(), contentRootPath);
        if (!File.Exists(resolved))
        {
            throw new InvalidOperationException(
                $"Firebase:CredentialPath aponta para um ficheiro que não existe: {resolved}");
        }

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", resolved);
    }

    private static string ResolveCredentialPath(string path, string? contentRootPath)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);
        if (!string.IsNullOrWhiteSpace(contentRootPath))
            return Path.GetFullPath(Path.Combine(contentRootPath, path));
        return Path.GetFullPath(path);
    }
}
