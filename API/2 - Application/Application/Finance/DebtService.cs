using Application.Dto.Finance;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class DebtService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;

    public DebtService(IFinanceStore finance, INotificationHandler notification)
    {
        _finance = finance;
        _notification = notification;
    }

    public async Task<List<DebtDto>> GetAllAsync(string userId)
    {
        var list = await _finance.ListDebtsAsync(userId);
        return list.Select(Map).ToList();
    }

    public async Task<DebtDto?> GetByIdAsync(string userId, Guid id)
    {
        var d = await _finance.GetDebtAsync(userId, id);
        return d == null ? null : Map(d);
    }

    public async Task<DebtDto?> UpsertAsync(string userId, DebtDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _notification.DefaultBuilder("Debt_01", "Nome da dívida é obrigatório");
            return null;
        }

        if (dto.TotalAmount < 0 || dto.PaidAmount < 0 || dto.PaidAmount > dto.TotalAmount)
        {
            _notification.DefaultBuilder("Debt_02", "Valores inválidos");
            return null;
        }

        if (dto.Id == Guid.Empty)
        {
            var entity = new Debt
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                TotalAmount = dto.TotalAmount,
                PaidAmount = dto.PaidAmount,
                DueDate = dto.DueDate,
                MonthlyPayment = dto.MonthlyPayment
            };
            await _finance.InsertDebtAsync(entity);
            return Map(entity);
        }

        var existing = await _finance.GetDebtAsync(userId, dto.Id);
        if (existing == null)
        {
            _notification.DefaultBuilder("Debt_03", "Dívida não encontrada");
            return null;
        }

        existing.Name = dto.Name.Trim();
        existing.TotalAmount = dto.TotalAmount;
        existing.PaidAmount = dto.PaidAmount;
        existing.DueDate = dto.DueDate;
        existing.MonthlyPayment = dto.MonthlyPayment;
        await _finance.UpdateDebtAsync(existing);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var d = await _finance.GetDebtAsync(userId, id);
        if (d == null)
        {
            _notification.DefaultBuilder("Debt_04", "Dívida não encontrada");
            return false;
        }

        await _finance.DeleteDebtAsync(userId, id);
        return true;
    }

    private static DebtDto Map(Debt d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        TotalAmount = d.TotalAmount,
        PaidAmount = d.PaidAmount,
        Balance = d.TotalAmount - d.PaidAmount,
        DueDate = d.DueDate,
        MonthlyPayment = d.MonthlyPayment
    };
}
