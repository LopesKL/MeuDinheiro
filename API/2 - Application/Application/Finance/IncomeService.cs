using Application.Dto.Finance;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class IncomeService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;

    public IncomeService(IFinanceStore finance, INotificationHandler notification)
    {
        _finance = finance;
        _notification = notification;
    }

    public async Task<List<IncomeDto>> GetAllAsync(string userId)
    {
        var list = await _finance.ListIncomesAsync(userId);
        return list.Select(Map).ToList();
    }

    public async Task<IncomeDto?> GetByIdAsync(string userId, Guid id)
    {
        var e = await _finance.GetIncomeAsync(userId, id);
        return e == null ? null : Map(e);
    }

    public async Task<List<IncomeMonthlyHistoryDto>> GetMonthlyHistoryAsync(string userId, int months = 24)
    {
        var from = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-months + 1);
        var incomes = await _finance.ListIncomesFromMonthAsync(userId, from);

        var groups = incomes
            .GroupBy(i => new { i.ReferenceMonth.Year, i.ReferenceMonth.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new IncomeMonthlyHistoryDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Label = $"{g.Key.Month:00}/{g.Key.Year}",
                Total = g.Sum(x => x.Amount),
                Items = g.Select(Map).ToList()
            })
            .ToList();

        return groups;
    }

    public async Task<IncomeDto?> UpsertAsync(string userId, IncomeDto dto)
    {
        if (dto.Amount <= 0)
        {
            _notification.DefaultBuilder("Inc_01", "Valor deve ser maior que zero");
            return null;
        }

        var refMonth = new DateTime(dto.ReferenceMonth.Year, dto.ReferenceMonth.Month, 1);

        if (dto.CreditCardId.HasValue)
        {
            var cc = await _finance.GetCreditCardAsync(userId, dto.CreditCardId.Value);
            if (cc == null)
            {
                _notification.DefaultBuilder("Inc_04", "Cartão não encontrado");
                return null;
            }
        }

        if (dto.Id == Guid.Empty)
        {
            var batchId = dto.BatchId is { } b && b != Guid.Empty ? b : (Guid?)null;
            var entity = new Income
            {
                UserId = userId,
                Amount = dto.Amount,
                Source = dto.Source?.Trim() ?? "Renda",
                Description = dto.Description,
                ReferenceMonth = refMonth,
                BatchId = batchId,
                CreditCardId = dto.CreditCardId
            };
            await _finance.InsertIncomeAsync(entity);
            return Map(entity);
        }

        var existing = await _finance.GetIncomeAsync(userId, dto.Id);
        if (existing == null)
        {
            _notification.DefaultBuilder("Inc_02", "Renda não encontrada");
            return null;
        }

        existing.Amount = dto.Amount;
        existing.Source = dto.Source?.Trim() ?? existing.Source;
        existing.Description = dto.Description;
        existing.ReferenceMonth = refMonth;
        existing.CreditCardId = dto.CreditCardId;
        if (dto.BatchId is { } nb && nb != Guid.Empty)
            existing.BatchId = nb;
        await _finance.UpdateIncomeAsync(existing);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var e = await _finance.GetIncomeAsync(userId, id);
        if (e == null)
        {
            _notification.DefaultBuilder("Inc_03", "Renda não encontrada");
            return false;
        }

        await _finance.DeleteIncomeAsync(userId, id);
        return true;
    }

    private static IncomeDto Map(Income i) => new()
    {
        Id = i.Id,
        Amount = i.Amount,
        Source = i.Source,
        Description = i.Description,
        ReferenceMonth = i.ReferenceMonth,
        BatchId = i.BatchId,
        CreditCardId = i.CreditCardId
    };
}
