using Application;
using Notifications.Notifications;
using Repositories.Interfaces;
using Repositories.Repositories;
using Serilog;
using Microsoft.EntityFrameworkCore;
using SqlServer;
using SqlServer.Context;
using WebAPI.Workers;
using WebAPI;
using Repositories;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

// Npgsql 6+: colunas timestamptz rejeitam DateTime Kind=Unspecified; a flag evita 500 em caminhos legados.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false);
DatabaseUrlConfigurationLoader.Apply(builder);
PostgresConnectionFileLoader.Apply(builder);
DatabaseUrlConfigurationLoader.ApplySanitizationToExistingConnectionString(builder);

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "logs"));

// Serilog: apenas console (stdout no Render). Sem sinks configuráveis por JSON para evitar MSSqlServer/outros.
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console();

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

try
{
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("PostgreSQL")))
    {
        var probe = DatabaseUrlConfigurationLoader.ProbeProcessEnvironment();
        Log.Warning(
            "PostgreSQL: ConnectionStrings:PostgreSQL continua vazio após carregar config. " +
            "Ambiente (sem valores): URL-like vars={UrlLike}, ConnectionStrings__PostgreSQL={ConnStrEnv}, PGHOST+PGDATABASE={PgLib}. " +
            "Primeira chave URL-like encontrada: {FirstUrlKey}. " +
            "Render: no Web Service (Docker) → Environment → DATABASE_URL = Internal Database URL do Postgres, ou ConnectionStrings__PostgreSQL (Npgsql).",
            probe.HasDatabaseUrlLikeEnv,
            probe.HasConnectionStringsPostgreSQLEnv,
            probe.HasPgLibEnv,
            probe.FirstUrlEnvKey ?? "(nenhuma)");
    }

    PostgreSqlConfigurationGuard.Validate(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddSqlServerContext(builder.Configuration, builder.Environment.ContentRootPath);
    builder.Services.AddEfFinanceStore();

    builder.Services.AddApplication(builder.Configuration);

    builder.Services.AddScoped<INotificationHandler, NotificationHandler>();

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddHostedService<RecurringExpenseWorker>();

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var wwwroot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(Path.Combine(wwwroot, "uploads"));

    var app = builder.Build();

    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiServerContext>();
        if (await db.Database.CanConnectAsync())
        {
            var provider = db.Database.ProviderName ?? string.Empty;
            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                await db.Database.EnsureCreatedAsync();
            else if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                await db.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Não foi possível aplicar migração ou criar o banco automaticamente.");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // Sem isto, 500 em produção muitas vezes não inclui cabeçalhos CORS e o browser acusa "CORS" em vez do erro real.
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                AppendWildcardCorsHeaders(context.Response.Headers);

                var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (ex != null)
                    Log.Error(ex, "Exceção não tratada na API (ver stack trace abaixo)");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Erro interno no servidor.",
                    detail = "Consulte os logs do serviço (ex.: Render → Logs).",
                });
            });
        });
    }

    app.UseStaticFiles();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await next();
    });

    app.UseRouting();
    app.UseCors();
    app.UseResponseCompression();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Render/browser na raiz: sem isto, GET / devolve 404 (só existem rotas /api/...).
    app.MapGet("/", () => Results.Json(new
    {
        ok = true,
        service = "MeuDinheiro API",
        routes = "Todas sob /api/... (ex.: POST /api/SignIn/signin). Swagger apenas em Development."
    }));

    // O browser pede /favicon.ico automaticamente; sem ficheiro em wwwroot → 404 na consola.
    app.MapMethods("/favicon.ico", new[] { "GET", "HEAD" }, () => Results.NoContent());

    app.Run();
}
catch (Exception ex)
{
    try
    {
        var logsDir = Path.Combine(builder.Environment.ContentRootPath, "logs");
        Directory.CreateDirectory(logsDir);
        File.WriteAllText(
            Path.Combine(logsDir, "startup-failure.txt"),
            $"{DateTime.UtcNow:O} UTC\n{ex}");
    }
    catch
    {
        // ignorar falha ao escrever diagnóstico
    }

    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

static void AppendWildcardCorsHeaders(IHeaderDictionary headers)
{
    if (!headers.ContainsKey("Access-Control-Allow-Origin"))
        headers.Append("Access-Control-Allow-Origin", "*");
    if (!headers.ContainsKey("Access-Control-Allow-Headers"))
        headers.Append("Access-Control-Allow-Headers", "Authorization, Content-Type, Accept, Origin, X-Requested-With");
    if (!headers.ContainsKey("Access-Control-Allow-Methods"))
        headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, PATCH, DELETE, OPTIONS");
}
