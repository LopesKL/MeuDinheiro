using Application.Dto.Finance;
using AutoMapper;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class ExpenseService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;
    private readonly IMapper _mapper;

    public ExpenseService(IFinanceStore finance, INotificationHandler notification, IMapper mapper)
    {
        _finance = finance;
        _notification = notification;
        _mapper = mapper;
    }

    public async Task<List<ExpenseDto>> GetAllAsync(string userId, int? year = null, int? month = null)
    {
        DateTime? from = null;
        DateTime? toEx = null;
        if (year.HasValue && month.HasValue)
        {
            from = new DateTime(year.Value, month.Value, 1);
            toEx = from.Value.AddMonths(1);
        }

        var list = await _finance.ListExpensesAsync(userId, from, toEx);
        await _finance.HydrateExpenseCategoriesAsync(list, userId);
        return list.OrderByDescending(e => e.Date).Select(MapExpense).ToList();
    }

    public async Task<ExpenseDto?> GetByIdAsync(string userId, Guid id)
    {
        var e = await _finance.GetExpenseAsync(userId, id);
        return e == null ? null : MapExpense(e);
    }

    public async Task<ExpenseDto?> UpsertAsync(string userId, ExpenseDto dto)
    {
        if (dto.Amount <= 0)
        {
            _notification.DefaultBuilder("Exp_01", "Valor deve ser maior que zero");
            return null;
        }

        var cat = await _finance.GetCategoryAsync(userId, dto.CategoryId);
        if (cat == null)
        {
            _notification.DefaultBuilder("Exp_02", "Categoria inválida");
            return null;
        }

        if (dto.CreditCardId.HasValue)
        {
            var card = await _finance.GetCreditCardAsync(userId, dto.CreditCardId.Value);
            if (card == null)
            {
                _notification.DefaultBuilder("Exp_03", "Cartão inválido");
                return null;
            }
        }

        if (dto.Id == Guid.Empty)
        {
            var entity = new Expense
            {
                UserId = userId,
                Amount = dto.Amount,
                Date = dto.Date,
                CategoryId = dto.CategoryId,
                Description = dto.Description?.Trim() ?? string.Empty,
                PaymentMethod = (PaymentMethod)dto.PaymentMethod,
                StoreLocation = dto.StoreLocation,
                CreditCardId = dto.CreditCardId,
                InstallmentPlanId = dto.InstallmentPlanId,
                ImagePath = dto.ImagePath,
                CreationSource = Enum.IsDefined(typeof(ExpenseCreationSource), dto.CreationSource)
                    ? (ExpenseCreationSource)dto.CreationSource
                    : ExpenseCreationSource.Unspecified
            };
            await _finance.InsertExpenseAsync(entity);
            return await GetByIdAsync(userId, entity.Id);
        }

        var existing = await _finance.GetExpenseAsync(userId, dto.Id);
        if (existing == null)
        {
            _notification.DefaultBuilder("Exp_04", "Gasto não encontrado");
            return null;
        }

        existing.Amount = dto.Amount;
        existing.Date = dto.Date;
        existing.CategoryId = dto.CategoryId;
        existing.Description = dto.Description?.Trim() ?? string.Empty;
        existing.PaymentMethod = (PaymentMethod)dto.PaymentMethod;
        existing.StoreLocation = dto.StoreLocation;
        existing.CreditCardId = dto.CreditCardId;
        existing.InstallmentPlanId = dto.InstallmentPlanId;
        if (dto.ImagePath != null)
            existing.ImagePath = dto.ImagePath;
        await _finance.UpdateExpenseAsync(existing);
        return await GetByIdAsync(userId, existing.Id);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var e = await _finance.GetExpenseAsync(userId, id);
        if (e == null)
        {
            _notification.DefaultBuilder("Exp_05", "Gasto não encontrado");
            return false;
        }

        var inst = await _finance.FindInstallmentByExpenseIdAsync(id);
        if (inst != null)
        {
            inst.ExpenseId = null;
            inst.IsPaid = false;
            await _finance.UpdateInstallmentAsync(inst);
        }

        await _finance.DeleteExpenseAsync(userId, id);
        return true;
    }

    public async Task<ParseExpenseResultDto?> ParseQuickInputAsync(string userId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _notification.DefaultBuilder("Exp_06", "Texto vazio");
            return null;
        }

        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        decimal? amount = null;
        var rest = new List<string>();
        foreach (var p in parts)
        {
            if (amount == null && decimal.TryParse(p.Replace(",", "."), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v))
            {
                amount = v;
                continue;
            }

            rest.Add(p);
        }

        int? pm = null;
        var descParts = new List<string>();
        foreach (var w in rest)
        {
            var lw = w.ToLowerInvariant();
            if (lw is "crédito" or "credito" or "cartão" or "cartao")
                pm = (int)PaymentMethod.Credit;
            else if (lw is "débito" or "debito")
                pm = (int)PaymentMethod.Debit;
            else if (lw is "pix")
                pm = (int)PaymentMethod.Pix;
            else if (lw is "dinheiro")
                pm = (int)PaymentMethod.Cash;
            else
                descParts.Add(w);
        }

        var description = string.Join(" ", descParts);
        var categories = await _finance.ListExpenseCategoriesAsync(userId);
        Guid? catId = null;
        string? catName = null;
        foreach (var c in categories)
        {
            if (description.Contains(c.Name, StringComparison.OrdinalIgnoreCase))
            {
                catId = c.Id;
                catName = c.Name;
                break;
            }
        }

        if (catId == null && description.Length > 0)
        {
            var hit = categories.FirstOrDefault(c =>
                c.Name.Contains(description, StringComparison.OrdinalIgnoreCase) ||
                description.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
            if (hit != null)
            {
                catId = hit.Id;
                catName = hit.Name;
            }
        }

        return new ParseExpenseResultDto
        {
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(description) ? "Gasto rápido" : description,
            PaymentMethod = pm,
            SuggestedCategoryId = catId,
            SuggestedCategoryName = catName
        };
    }

    private ExpenseDto MapExpense(Expense e) => _mapper.Map<ExpenseDto>(e);
}
