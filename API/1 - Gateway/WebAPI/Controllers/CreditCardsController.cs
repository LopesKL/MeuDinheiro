using Application.Finance;
using Application.Users;
using Application.Dto.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class CreditCardsController : BaseController
{
    private readonly CreditCardService _service;

    public CreditCardsController(INotificationHandler notification, UserHandler userHandler, CreditCardService service)
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

    [HttpGet("{id:guid}/invoice")]
    public async Task<IActionResult> Invoice(Guid id, [FromQuery] int year, [FromQuery] int month)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetInvoiceAsync(userId, id, year, month));
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] CreditCardDto dto)
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
        return ok ? HandleSuccess("Removido") : HandleResponse<CreditCardDto?>(null);
    }
}
