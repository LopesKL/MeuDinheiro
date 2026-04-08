using Application.Dto.Finance;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class AccountService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;
    private readonly PatrimonySnapshotService _snapshots;

    public AccountService(IFinanceStore finance, INotificationHandler notification, PatrimonySnapshotService snapshots)
    {
        _finance = finance;
        _notification = notification;
        _snapshots = snapshots;
    }

    public async Task<List<AccountDto>> GetAllAsync(string userId)
    {
        var list = await _finance.ListAccountsAsync(userId);
        return list.Select(Map).ToList();
    }

    public async Task<decimal> GetTotalAsync(string userId) =>
        await _finance.SumAccountBalancesAsync(userId);

    public async Task<AccountDto?> GetByIdAsync(string userId, Guid id)
    {
        var a = await _finance.GetAccountAsync(userId, id);
        return a == null ? null : Map(a);
    }

    public async Task<AccountDto?> UpsertAsync(string userId, AccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _notification.DefaultBuilder("Acc_01", "Nome da conta é obrigatório");
            return null;
        }

        if (dto.Id == Guid.Empty)
        {
            var entity = new Account
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                Type = (AccountType)dto.Type,
                Balance = dto.Balance,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.Trim().ToUpperInvariant()
            };
            await _finance.InsertAccountAsync(entity);
            await _snapshots.RefreshCurrentMonthAsync(userId);
            return Map(entity);
        }

        var existing = await _finance.GetAccountAsync(userId, dto.Id);
        if (existing == null)
        {
            _notification.DefaultBuilder("Acc_02", "Conta não encontrada");
            return null;
        }

        existing.Name = dto.Name.Trim();
        existing.Type = (AccountType)dto.Type;
        existing.Balance = dto.Balance;
        existing.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? existing.Currency : dto.Currency.Trim().ToUpperInvariant();
        await _finance.UpdateAccountAsync(existing);
        await _snapshots.RefreshCurrentMonthAsync(userId);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var a = await _finance.GetAccountAsync(userId, id);
        if (a == null)
        {
            _notification.DefaultBuilder("Acc_03", "Conta não encontrada");
            return false;
        }

        await _finance.DeleteAccountAsync(userId, id);
        await _snapshots.RefreshCurrentMonthAsync(userId);
        return true;
    }

    public async Task<List<PatrimonyHistoryPointDto>> GetPatrimonyHistoryAsync(string userId, int months = 12)
    {
        var list = await _finance.ListPatrimonySnapshotsAsync(userId, months);
        return list
            .Select(s => new PatrimonyHistoryPointDto
            {
                Year = s.Year,
                Month = s.Month,
                Label = $"{s.Month:00}/{s.Year}",
                TotalBalance = s.TotalBalance
            })
            .ToList();
    }

    private static AccountDto Map(Account a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Type = (int)a.Type,
        Balance = a.Balance,
        Currency = a.Currency
    };
}
