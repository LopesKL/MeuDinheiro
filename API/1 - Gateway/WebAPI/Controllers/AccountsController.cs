using Application.Finance;
using Application.Users;
using Application.Dto.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class AccountsController : BaseController
{
    private readonly AccountService _service;

    public AccountsController(INotificationHandler notification, UserHandler userHandler, AccountService service)
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

    [HttpGet("total")]
    public async Task<IActionResult> Total()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetTotalAsync(userId));
    }

    [HttpGet("patrimony-history")]
    public async Task<IActionResult> PatrimonyHistory([FromQuery] int months = 12)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetPatrimonyHistoryAsync(userId, months));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetByIdAsync(userId, id));
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] AccountDto dto)
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
        return ok ? HandleSuccess("Removido") : HandleResponse<AccountDto?>(null);
    }
}
