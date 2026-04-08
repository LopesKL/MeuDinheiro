using Application;
using Firebase;
using Notifications.Notifications;
using Repositories.Interfaces;
using Repositories.Repositories;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Microsoft.EntityFrameworkCore;
using SqlServer;
using SqlServer.Context;
using WebAPI.Workers;
using WebAPI;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
var serilogConnectionString = builder.Configuration.GetConnectionString("SqlServer");
var useInMemory = builder.Configuration.GetValue<bool>("Database:UseInMemory", false);
var useSqlite = builder.Configuration.GetValue<bool>("Database:UseSqlite", false);
var connectionStringValid = !string.IsNullOrWhiteSpace(serilogConnectionString)
    && !serilogConnectionString.Contains("your_user", StringComparison.OrdinalIgnoreCase)
    && !serilogConnectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console();

if (!useInMemory && !useSqlite && connectionStringValid)
{
    loggerConfig.WriteTo.MSSqlServer(
        connectionString: serilogConnectionString!,
        sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
        {
            TableName = "LogAPI",
            AutoCreateSqlTable = true
        },
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error);
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

try
{
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddFirebaseFinanceStore(builder.Configuration);
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
            if (db.Database.IsInMemory() || provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                await db.Database.EnsureCreatedAsync();
            else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
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

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
