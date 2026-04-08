using Microsoft.Extensions.Hosting;

namespace SqlServer.Services;

public class TimedHostedService : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(ExecuteStoredProcedure, null, TimeSpan.Zero, TimeSpan.FromMinutes(60));
        return Task.CompletedTask;
    }

    private void ExecuteStoredProcedure(object? state)
    {
        // Implementar stored procedure se necessário
        // Este método não será executado quando usar banco em memória
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
