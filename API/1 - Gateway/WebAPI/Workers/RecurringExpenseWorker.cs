using Application.Finance;

namespace WebAPI.Workers;

/// <summary>Executa geração de gastos recorrentes periodicamente.</summary>
public class RecurringExpenseWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringExpenseWorker> _logger;

    public RecurringExpenseWorker(IServiceScopeFactory scopeFactory, ILogger<RecurringExpenseWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<RecurringExpenseService>();
                var n = await svc.GenerateDueForTodayAsync(stoppingToken);
                if (n > 0)
                    _logger.LogInformation("Gastos recorrentes gerados: {Count}", n);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no worker de gastos recorrentes");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
