using Application.Dto.Finance;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class RecurringExpenseService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;

    public RecurringExpenseService(IFinanceStore finance, INotificationHandler notification)
    {
        _finance = finance;
        _notification = notification;
    }

    public async Task<List<RecurringExpenseDto>> GetAllAsync(string userId)
    {
        var list = await _finance.ListRecurringAsync(userId);
        return list.Select(Map).ToList();
    }

    public async Task<RecurringExpenseDto?> GetByIdAsync(string userId, Guid id)
    {
        var r = await _finance.GetRecurringAsync(userId, id);
        return r == null ? null : Map(r);
    }

    public async Task<RecurringExpenseDto?> UpsertAsync(string userId, RecurringExpenseDto dto)
    {
        if (dto.Amount <= 0 || dto.DayOfMonth is < 1 or > 31)
        {
            _notification.DefaultBuilder("Rec_01", "Valor ou dia do mês inválido");
            return null;
        }

        var cat = await _finance.GetCategoryAsync(userId, dto.CategoryId);
        if (cat == null)
        {
            _notification.DefaultBuilder("Rec_02", "Categoria inválida");
            return null;
        }

        if (dto.CreditCardId.HasValue)
        {
            var cc = await _finance.GetCreditCardAsync(userId, dto.CreditCardId.Value);
            if (cc == null)
            {
                _notification.DefaultBuilder("Rec_03", "Cartão inválido");
                return null;
            }
        }

        if (dto.Id == Guid.Empty)
        {
            var entity = new RecurringExpense
            {
                UserId = userId,
                Type = (RecurringExpenseType)dto.Type,
                Amount = dto.Amount,
                CategoryId = dto.CategoryId,
                Description = dto.Description?.Trim() ?? "Recorrente",
                PaymentMethod = (PaymentMethod)dto.PaymentMethod,
                DayOfMonth = dto.DayOfMonth,
                Active = dto.Active,
                CreditCardId = dto.CreditCardId
            };
            await _finance.InsertRecurringAsync(entity);
            await TryAccrueIfDueInCurrentMonthAsync(entity);
            return Map(entity);
        }

        var existing = await _finance.GetRecurringAsync(userId, dto.Id);
        if (existing == null)
        {
            _notification.DefaultBuilder("Rec_04", "Registro não encontrado");
            return null;
        }

        existing.Type = (RecurringExpenseType)dto.Type;
        existing.Amount = dto.Amount;
        existing.CategoryId = dto.CategoryId;
        existing.Description = dto.Description?.Trim() ?? existing.Description;
        existing.PaymentMethod = (PaymentMethod)dto.PaymentMethod;
        existing.DayOfMonth = dto.DayOfMonth;
        existing.Active = dto.Active;
        existing.CreditCardId = dto.CreditCardId;
        await _finance.UpdateRecurringAsync(existing);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var r = await _finance.GetRecurringAsync(userId, id);
        if (r == null)
        {
            _notification.DefaultBuilder("Rec_05", "Registro não encontrado");
            return false;
        }

        await _finance.DeleteRecurringAsync(userId, id);
        return true;
    }

    public async Task TryAccrueIfDueInCurrentMonthAsync(RecurringExpense r, CancellationToken ct = default)
    {
        if (!r.Active)
            return;

        var today = DateTime.UtcNow.Date;
        var monthKey = new DateTime(today.Year, today.Month, 1);
        if (r.LastGeneratedMonth == monthKey)
            return;

        var lastDay = DateTime.DaysInMonth(today.Year, today.Month);
        var chargeDay = Math.Min(r.DayOfMonth, lastDay);
        if (today.Day < chargeDay)
            return;

        var expenseDate = new DateTime(today.Year, today.Month, chargeDay);
        var expense = new Expense
        {
            UserId = r.UserId,
            Amount = r.Amount,
            Date = expenseDate,
            CategoryId = r.CategoryId,
            Description = r.Description + " (recorrente)",
            PaymentMethod = r.PaymentMethod,
            CreditCardId = r.CreditCardId
        };
        await _finance.InsertExpenseAsync(expense);
        r.LastGeneratedMonth = monthKey;
        await _finance.UpdateRecurringAsync(r);
    }

    public async Task<int> GenerateDueForTodayAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var monthKey = new DateTime(today.Year, today.Month, 1);

        var recs = await _finance.ListActiveRecurringForDayOfMonthAsync(today.Day, ct);

        var count = 0;
        foreach (var r in recs)
        {
            if (r.LastGeneratedMonth == monthKey)
                continue;

            var expense = new Expense
            {
                UserId = r.UserId,
                Amount = r.Amount,
                Date = today.Date,
                CategoryId = r.CategoryId,
                Description = r.Description + " (recorrente)",
                PaymentMethod = r.PaymentMethod,
                CreditCardId = r.CreditCardId
            };
            await _finance.InsertExpenseAsync(expense);
            r.LastGeneratedMonth = monthKey;
            await _finance.UpdateRecurringAsync(r);
            count++;
        }

        return count;
    }

    private static RecurringExpenseDto Map(RecurringExpense r) => new()
    {
        Id = r.Id,
        Type = (int)r.Type,
        Amount = r.Amount,
        CategoryId = r.CategoryId,
        Description = r.Description,
        PaymentMethod = (int)r.PaymentMethod,
        DayOfMonth = r.DayOfMonth,
        Active = r.Active,
        CreditCardId = r.CreditCardId
    };
}
