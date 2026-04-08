using Application.Dto.Finance;
using AutoMapper;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class CreditCardService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;
    private readonly IMapper _mapper;

    public CreditCardService(IFinanceStore finance, INotificationHandler notification, IMapper mapper)
    {
        _finance = finance;
        _notification = notification;
        _mapper = mapper;
    }

    public async Task<List<CreditCardDto>> GetAllAsync(string userId)
    {
        var list = await _finance.ListCreditCardsAsync(userId);
        return list.Select(Map).ToList();
    }

    public async Task<CreditCardDto?> GetByIdAsync(string userId, Guid id)
    {
        var c = await _finance.GetCreditCardAsync(userId, id);
        return c == null ? null : Map(c);
    }

    public async Task<CreditCardDto?> UpsertAsync(string userId, CreditCardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _notification.DefaultBuilder("Cc_01", "Nome do cartão é obrigatório");
            return null;
        }

        if (dto.ClosingDay is < 1 or > 31 || dto.DueDay is < 1 or > 31)
        {
            _notification.DefaultBuilder("Cc_02", "Dias de fechamento/vencimento entre 1 e 31");
            return null;
        }

        if (dto.Id == Guid.Empty)
        {
            var entity = new CreditCard
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                ClosingDay = dto.ClosingDay,
                DueDay = dto.DueDay
            };
            await _finance.InsertCreditCardAsync(entity);
            return Map(entity);
        }

        var existing = await _finance.GetCreditCardAsync(userId, dto.Id);
        if (existing == null)
        {
            _notification.DefaultBuilder("Cc_03", "Cartão não encontrado");
            return null;
        }

        existing.Name = dto.Name.Trim();
        existing.ClosingDay = dto.ClosingDay;
        existing.DueDay = dto.DueDay;
        await _finance.UpdateCreditCardAsync(existing);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var c = await _finance.GetCreditCardAsync(userId, id);
        if (c == null)
        {
            _notification.DefaultBuilder("Cc_04", "Cartão não encontrado");
            return false;
        }

        if (await _finance.CreditCardInUseAsync(id))
        {
            _notification.DefaultBuilder("Cc_05", "Cartão em uso");
            return false;
        }

        await _finance.DeleteCreditCardAsync(userId, id);
        return true;
    }

    public async Task<CreditCardInvoiceDto?> GetInvoiceAsync(string userId, Guid cardId, int year, int month)
    {
        var card = await _finance.GetCreditCardAsync(userId, cardId);
        if (card == null)
        {
            _notification.DefaultBuilder("Cc_06", "Cartão não encontrado");
            return null;
        }

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var expenses = await _finance.ListExpensesForCardAsync(userId, cardId, start, end);
        await _finance.HydrateExpenseCategoriesAsync(expenses, userId);

        return new CreditCardInvoiceDto
        {
            CreditCardId = card.Id,
            CreditCardName = card.Name,
            Year = year,
            Month = month,
            Total = expenses.Sum(e => e.Amount),
            Expenses = expenses.Select(e => _mapper.Map<ExpenseDto>(e)).ToList()
        };
    }

    private static CreditCardDto Map(CreditCard c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        ClosingDay = c.ClosingDay,
        DueDay = c.DueDay
    };
}
