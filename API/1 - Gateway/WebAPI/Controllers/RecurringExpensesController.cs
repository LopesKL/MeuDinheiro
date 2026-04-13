using Application.Finance;
using Application.Users;
using Application.Dto.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class RecurringExpensesController : BaseController
{
    private readonly RecurringExpenseService _service;

    public RecurringExpensesController(INotificationHandler notification, UserHandler userHandler, RecurringExpenseService service)
        : base(notification, userHandler)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetAllAsync(userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetByIdAsync(userId, id));
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] RecurringExpenseDto dto)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.UpsertAsync(userId, dto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        var ok = await _service.DeleteAsync(userId, id);
        return ok ? HandleSuccess("Removido") : HandleResponse<RecurringExpenseDto?>(null);
    }

    /// <summary>Define valor vigente a partir do mês informado (e meses futuros), sem alterar o valor base.</summary>
    [HttpPost("{recurringId:guid}/amount-schedule")]
    public async Task<IActionResult> SetAmountSchedule(Guid recurringId, [FromBody] SetRecurringAmountFromMonthDto dto)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.SetAmountFromMonthAsync(userId, recurringId, dto));
    }

    [HttpDelete("{recurringId:guid}/amount-schedule/{scheduleId:guid}")]
    public async Task<IActionResult> DeleteAmountSchedule(Guid recurringId, Guid scheduleId)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.DeleteAmountScheduleAsync(userId, recurringId, scheduleId));
    }
}
