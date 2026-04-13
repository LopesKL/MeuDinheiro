using System.Text.RegularExpressions;
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

        int closingDay;
        int dueDay;
        decimal? mealDaily = null;
        int? mealCreditDay = null;

        if (dto.IsMealVoucher)
        {
            if (dto.MealVoucherDailyAmount is null or <= 0)
            {
                _notification.DefaultBuilder("Cc_08", "Informe o valor fixo por dia útil (maior que zero)");
                return null;
            }

            if (dto.MealVoucherCreditDay is < 1 or > 31)
            {
                _notification.DefaultBuilder("Cc_09", "Dia de crédito do vale deve ser entre 1 e 31");
                return null;
            }

            closingDay = 1;
            dueDay = 1;
            mealDaily = dto.MealVoucherDailyAmount!.Value;
            mealCreditDay = dto.MealVoucherCreditDay!.Value;
        }
        else
        {
            if (dto.ClosingDay is < 1 or > 31 || dto.DueDay is < 1 or > 31)
            {
                _notification.DefaultBuilder("Cc_02", "Dias de fechamento/vencimento entre 1 e 31");
                return null;
            }

            closingDay = dto.ClosingDay;
            dueDay = dto.DueDay;
        }

        if (!TryNormalizeThemeColor(dto.ThemeColor, out var themeColor))
        {
            _notification.DefaultBuilder("Cc_07", "Cor inválida. Use #RGB ou #RRGGBB.");
            return null;
        }

        if (dto.Id == Guid.Empty)
        {
            var entity = new CreditCard
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                ClosingDay = closingDay,
                DueDay = dueDay,
                IsMealVoucher = dto.IsMealVoucher,
                MealVoucherDailyAmount = mealDaily,
                MealVoucherCreditDay = mealCreditDay,
                ThemeColor = themeColor
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
        existing.ClosingDay = closingDay;
        existing.DueDay = dueDay;
        existing.IsMealVoucher = dto.IsMealVoucher;
        existing.MealVoucherDailyAmount = mealDaily;
        existing.MealVoucherCreditDay = mealCreditDay;
        existing.ThemeColor = themeColor;
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

        var weekdays = CountWeekdaysInMonth(year, month);
        var daily = card.MealVoucherDailyAmount ?? 0m;
        var expectedCredit = card.IsMealVoucher ? decimal.Round(daily * weekdays, 2, MidpointRounding.AwayFromZero) : 0m;

        return new CreditCardInvoiceDto
        {
            CreditCardId = card.Id,
            CreditCardName = card.Name,
            Year = year,
            Month = month,
            Total = expenses.Sum(e => e.Amount),
            Expenses = expenses.Select(e => _mapper.Map<ExpenseDto>(e)).ToList(),
            IsMealVoucher = card.IsMealVoucher,
            MealVoucherDailyAmount = card.MealVoucherDailyAmount,
            MealVoucherCreditDay = card.MealVoucherCreditDay,
            BusinessDaysInMonth = weekdays,
            ExpectedMonthlyCredit = expectedCredit
        };
    }

    private static int CountWeekdaysInMonth(int year, int month)
    {
        var last = DateTime.DaysInMonth(year, month);
        var n = 0;
        for (var d = 1; d <= last; d++)
        {
            var dt = new DateTime(year, month, d);
            if (dt.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                n++;
        }

        return n;
    }

    private static CreditCardDto Map(CreditCard c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        ClosingDay = c.ClosingDay,
        DueDay = c.DueDay,
        IsMealVoucher = c.IsMealVoucher,
        MealVoucherDailyAmount = c.MealVoucherDailyAmount,
        MealVoucherCreditDay = c.MealVoucherCreditDay,
        ThemeColor = c.ThemeColor
    };

    private static bool TryNormalizeThemeColor(string? raw, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        var s = raw.Trim();
        if (s.Length > 16)
            return false;
        if (!Regex.IsMatch(s, "^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$"))
            return false;
        normalized = s;
        return true;
    }
}
