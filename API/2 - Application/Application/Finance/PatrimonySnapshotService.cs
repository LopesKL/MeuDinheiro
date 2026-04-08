using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class PatrimonySnapshotService
{
    private readonly IFinanceStore _finance;

    public PatrimonySnapshotService(IFinanceStore finance)
    {
        _finance = finance;
    }

    public async Task RefreshCurrentMonthAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var total = await _finance.SumAccountBalancesAsync(userId);

        var existing = await _finance.FindPatrimonySnapshotAsync(userId, now.Year, now.Month, ct);

        if (existing == null)
        {
            await _finance.InsertPatrimonySnapshotAsync(new PatrimonyMonthlySnapshot
            {
                UserId = userId,
                Year = now.Year,
                Month = now.Month,
                TotalBalance = total,
                UpdatedAt = DateTimeOffset.UtcNow
            }, ct);
        }
        else
        {
            existing.TotalBalance = total;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await _finance.UpdatePatrimonySnapshotAsync(existing, ct);
        }
    }
}
