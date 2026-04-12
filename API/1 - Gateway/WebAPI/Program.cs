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

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false);
DatabaseUrlConfigurationLoader.Apply(builder);
PostgresConnectionFileLoader.Apply(builder);

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
    PostgreSqlConfigurationGuard.Validate(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddMemoryFinanceStore();
    builder.Services.AddSqlServerContext(builder.Configuration, builder.Environment.ContentRootPath);

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

    app.UseStaticFiles();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await next();
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseResponseCompression();
    app.UseRouting();
    app.UseCors();
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
