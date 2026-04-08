using Application.Dto.Finance;
using AutoMapper;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class InstallmentPlanService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;
    private readonly IMapper _mapper;
    private readonly ExpenseService _expenseService;

    public InstallmentPlanService(
        IFinanceStore finance,
        INotificationHandler notification,
        IMapper mapper,
        ExpenseService expenseService)
    {
        _finance = finance;
        _notification = notification;
        _mapper = mapper;
        _expenseService = expenseService;
    }

    public async Task<List<InstallmentPlanDto>> GetAllAsync(string userId)
    {
        var list = await _finance.ListInstallmentPlansAsync(userId);
        return list
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => _mapper.Map<InstallmentPlanDto>(p))
            .ToList();
    }

    public async Task<InstallmentPlanDto?> GetByIdAsync(string userId, Guid id)
    {
        var p = await _finance.GetInstallmentPlanAsync(userId, id, true);
        return p == null ? null : _mapper.Map<InstallmentPlanDto>(p);
    }

    public async Task<InstallmentPlanDto?> CreateAsync(string userId, InstallmentPlanDto dto)
    {
        if (dto.InstallmentCount < 1 || dto.TotalAmount <= 0)
        {
            _notification.DefaultBuilder("Inst_01", "Parcelas e valor total inválidos");
            return null;
        }

        var cat = await _finance.GetCategoryAsync(userId, dto.CategoryId);
        if (cat == null)
        {
            _notification.DefaultBuilder("Inst_02", "Categoria inválida");
            return null;
        }

        if (dto.CreditCardId.HasValue)
        {
            var cc = await _finance.GetCreditCardAsync(userId, dto.CreditCardId.Value);
            if (cc == null)
            {
                _notification.DefaultBuilder("Inst_03", "Cartão inválido");
                return null;
            }
        }

        var plan = new InstallmentPlan
        {
            UserId = userId,
            CreditCardId = dto.CreditCardId,
            CategoryId = dto.CategoryId,
            Description = dto.Description?.Trim() ?? "Parcelamento",
            TotalAmount = dto.TotalAmount,
            InstallmentCount = dto.InstallmentCount,
            StartDate = dto.StartDate.Date
        };

        await _finance.InsertInstallmentPlanAsync(plan);

        var baseAmount = Math.Floor(dto.TotalAmount / dto.InstallmentCount * 100m) / 100m;
        var remainder = dto.TotalAmount - baseAmount * dto.InstallmentCount;

        for (var i = 1; i <= dto.InstallmentCount; i++)
        {
            var amt = i == dto.InstallmentCount ? baseAmount + remainder : baseAmount;
            var due = plan.StartDate.AddMonths(i - 1);
            await _finance.InsertInstallmentAsync(new Installment
            {
                InstallmentPlanId = plan.Id,
                SequenceNumber = i,
                DueDate = due,
                Amount = amt,
                IsPaid = false
            });
        }

        return await GetByIdAsync(userId, plan.Id);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var plan = await _finance.GetInstallmentPlanAsync(userId, id, true);
        if (plan == null)
        {
            _notification.DefaultBuilder("Inst_04", "Plano não encontrado");
            return false;
        }

        if (plan.Installments.Any(i => i.IsPaid))
        {
            _notification.DefaultBuilder("Inst_05", "Não é possível excluir: existem parcelas pagas");
            return false;
        }

        await _finance.DeleteInstallmentPlanAsync(userId, id);
        return true;
    }

    public async Task<ExpenseDto?> PayInstallmentAsync(string userId, Guid installmentId)
    {
        var inst = await _finance.GetInstallmentWithPlanAsync(installmentId);
        if (inst == null || inst.InstallmentPlan.UserId != userId)
        {
            _notification.DefaultBuilder("Inst_06", "Parcela não encontrada");
            return null;
        }

        if (inst.IsPaid && inst.ExpenseId.HasValue)
            return await _expenseService.GetByIdAsync(userId, inst.ExpenseId.Value);

        var plan = inst.InstallmentPlan;
        var expense = new Expense
        {
            UserId = userId,
            Amount = inst.Amount,
            Date = DateTime.UtcNow.Date,
            CategoryId = plan.CategoryId,
            Description = $"{plan.Description} ({inst.SequenceNumber}/{plan.InstallmentCount})",
            PaymentMethod = plan.CreditCardId.HasValue ? PaymentMethod.Credit : PaymentMethod.Other,
            CreditCardId = plan.CreditCardId,
            InstallmentPlanId = plan.Id
        };
        await _finance.InsertExpenseAsync(expense);
        inst.IsPaid = true;
        inst.ExpenseId = expense.Id;
        await _finance.UpdateInstallmentAsync(inst);

        return await _expenseService.GetByIdAsync(userId, expense.Id);
    }

    public async Task<InstallmentDto?> UpdateInstallmentAsync(string userId, Guid installmentId, InstallmentDto dto)
    {
        var inst = await _finance.GetInstallmentWithPlanAsync(installmentId);
        if (inst == null || inst.InstallmentPlan.UserId != userId)
        {
            _notification.DefaultBuilder("Inst_07", "Parcela não encontrada");
            return null;
        }

        if (inst.IsPaid)
        {
            _notification.DefaultBuilder("Inst_08", "Parcela já paga não pode ser alterada");
            return null;
        }

        if (dto.Amount <= 0)
        {
            _notification.DefaultBuilder("Inst_09", "Valor inválido");
            return null;
        }

        inst.Amount = dto.Amount;
        inst.DueDate = dto.DueDate.Date;
        await _finance.UpdateInstallmentAsync(inst);
        return _mapper.Map<InstallmentDto>(inst);
    }

    public async Task<bool> DeleteInstallmentAsync(string userId, Guid installmentId)
    {
        var inst = await _finance.GetInstallmentWithPlanAsync(installmentId);
        if (inst == null || inst.InstallmentPlan.UserId != userId)
        {
            _notification.DefaultBuilder("Inst_10", "Parcela não encontrada");
            return false;
        }

        if (inst.IsPaid)
        {
            _notification.DefaultBuilder("Inst_11", "Parcela paga não pode ser removida");
            return false;
        }

        await _finance.DeleteInstallmentAsync(installmentId);
        return true;
    }
}
